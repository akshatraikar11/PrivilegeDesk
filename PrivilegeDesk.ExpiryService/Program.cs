using Microsoft.EntityFrameworkCore;
using PrivilegeDesk.ExpiryService;
using PrivilegeDesk.ExpiryService.Models;

// CreateDefaultBuilder + UseWindowsService() is the classic Worker Service pattern.
// UseWindowsService() tells the .NET host: "when Windows Service Control Manager
// starts this exe, listen for start/stop instead of expecting a console."
IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService(options =>
    {
        options.ServiceName = "PrivilegeDesk Expiry Service";
    })
    .ConfigureServices((context, services) =>
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(context.Configuration.GetConnectionString("DefaultConnection")));

        services.AddSingleton<ExpiryQueue>();
        services.AddHostedService<Worker>();
    })
    .Build();

host.Run();
