using Microsoft.EntityFrameworkCore;
using PrivilegeDesk.ExpiryService.Models;

namespace PrivilegeDesk.ExpiryService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceScopeFactory _scopes;
    private readonly ExpiryQueue _queue;

    public Worker(ILogger<Worker> logger, IServiceScopeFactory scopes, ExpiryQueue queue)
    {
        _logger = logger;
        _scopes = scopes;
        _queue = queue;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // MULTITHREADING:
        // We start TWO consumer tasks so expired requests can be processed in parallel.
        // They share one Channel (a thread-safe queue). We do NOT share a DbContext —
        // EF Core contexts are not thread-safe, so each consumer opens its own scope.
        //
        // async/await is different from extra threads: await pauses this method while
        // SQL or Delay is in flight, then continues later on a thread-pool thread.
        // Task.Run below is what actually gives us two overlapping workers.
        var consumers = new[]
        {
            Task.Run(() => ConsumeAsync(1, stoppingToken), stoppingToken),
            Task.Run(() => ConsumeAsync(2, stoppingToken), stoppingToken)
        };

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await ProduceAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown via Ctrl+C or Windows Service stop.
        }
        finally
        {
            _queue.Writer.TryComplete();
            await Task.WhenAll(consumers);
        }
    }

    private async Task ProduceAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = DateTime.UtcNow;
        var expiredIds = await db.AccessRequests
            .Where(r => r.Status == RequestStatus.Approved && r.ExpiresAt != null && r.ExpiresAt <= now)
            .Select(r => r.Id)
            .ToListAsync(stoppingToken);

        foreach (var id in expiredIds)
        {
            await _queue.Writer.WriteAsync(id, stoppingToken);
            _logger.LogInformation("Queued request {Id} for expiry.", id);
        }
    }

    private async Task ConsumeAsync(int workerNumber, CancellationToken stoppingToken)
    {
        _logger.LogInformation("Expiry consumer {Worker} started.", workerNumber);

        try
        {
            await foreach (var id in _queue.Reader.ReadAllAsync(stoppingToken))
            {
                await MarkExpiredAsync(id, workerNumber, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Service is stopping.
        }
    }

    private async Task MarkExpiredAsync(int id, int workerNumber, CancellationToken stoppingToken)
    {
        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var request = await db.AccessRequests.FirstOrDefaultAsync(r => r.Id == id, stoppingToken);
        if (request is null || request.Status != RequestStatus.Approved)
        {
            // Another consumer may have already expired this row.
            return;
        }

        request.Status = RequestStatus.Expired;
        db.AuditLogs.Add(new AuditLog
        {
            UserId = request.UserId,
            Action = "RequestExpired",
            Details = $"Request {request.Id} expired (worker {workerNumber}).",
            Timestamp = DateTime.UtcNow
        });

        await db.SaveChangesAsync(stoppingToken);
        _logger.LogInformation("Request {Id} marked expired by worker {Worker}.", id, workerNumber);
    }
}
