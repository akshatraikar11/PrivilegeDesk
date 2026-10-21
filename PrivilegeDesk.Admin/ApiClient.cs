using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace PrivilegeDesk.Admin;

// Tiny wrapper around HttpClient. The JWT lives in memory only (this field),
// so closing the app forgets the session — that is what we want for a PAM tool.
public class ApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;

    public string? Token { get; private set; }
    public string? Username { get; private set; }
    public string? Role { get; private set; }

    public ApiClient(string baseUrl)
    {
        _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
        _http.Timeout = TimeSpan.FromSeconds(15);
    }

    public async Task LoginAsync(string username, string password)
    {
        var response = await _http.PostAsJsonAsync("api/auth/login", new { username, password });
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(body)
                ? $"Login failed ({(int)response.StatusCode})."
                : body.Trim('"'));
        }

        var login = JsonSerializer.Deserialize<LoginResponse>(body, JsonOptions)
            ?? throw new InvalidOperationException("The API returned an empty login response.");

        Token = login.Token;
        Username = login.Username;
        Role = login.Role;
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
    }

    public async Task<List<AccessRequestDto>> GetPendingAsync()
    {
        using var response = await _http.GetAsync("api/requests/pending");
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Could not load pending requests ({(int)response.StatusCode}): {body}");
        }

        return JsonSerializer.Deserialize<List<AccessRequestDto>>(body, JsonOptions) ?? new List<AccessRequestDto>();
    }

    public async Task DecideAsync(int id, bool approve)
    {
        var path = approve ? $"api/requests/{id}/approve" : $"api/requests/{id}/reject";
        using var response = await _http.PutAsync(path, content: null);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Update failed ({(int)response.StatusCode}): {body}");
        }
    }
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class AccessRequestDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public int ResourceId { get; set; }
    public string ResourceName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
}
