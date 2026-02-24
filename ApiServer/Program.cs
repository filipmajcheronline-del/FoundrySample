using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.IO;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapPost("/api/invoke", async (HttpRequest request) =>
{
    using var doc = await JsonDocument.ParseAsync(request.Body);
    var root = doc.RootElement;
    if (!root.TryGetProperty("url", out var urlElem) || !root.TryGetProperty("apiKey", out var keyElem) || !root.TryGetProperty("input", out var inputElem))
    {
        return Results.BadRequest(new { error = "Missing required properties: url, apiKey, input" });
    }

    var url = urlElem.GetString() ?? string.Empty;
    var apiKey = keyElem.GetString() ?? string.Empty;
    var input = inputElem.GetString() ?? string.Empty;
    // Optional: allow caller to specify which header name to use for the API key (default: "api-key")
    var headerName = "api-key";
    if (root.TryGetProperty("headerName", out var headerElem))
    {
        var hn = headerElem.GetString();
        if (!string.IsNullOrEmpty(hn)) headerName = hn;
    }

    // Log incoming request (mask apiKey except last 4 chars)
    var maskedKey = apiKey.Length > 4 ? new string('*', apiKey.Length - 4) + apiKey.Substring(apiKey.Length - 4) : apiKey;
    await Shared.Logger.LogAsync("api.log", $"Incoming invoke: url={url} headerName={headerName} apiKey={maskedKey} input={input}");

    try
    {
        using var client = new HttpClient();
        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        // Forward API key using the requested header name. If caller asked for Authorization
        // and the value doesn't look like a bearer token, add the "Bearer " prefix.
        if (headerName.Equals("Authorization", StringComparison.OrdinalIgnoreCase) && !apiKey.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            req.Headers.Add("Authorization", "Bearer " + apiKey);
        }
        else
        {
            req.Headers.Add(headerName, apiKey);
        }

        var payload = JsonSerializer.Serialize(new { input });
        req.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        using var resp = await client.SendAsync(req);
        var respText = await resp.Content.ReadAsStringAsync();
        // Log remote response (truncate body to 2000 chars)
        var bodySnippet = respText?.Length > 2000 ? respText.Substring(0, 2000) + "...[truncated]" : respText;
        await Shared.Logger.LogAsync("api.log", $"Forwarded to {url} returned {(int)resp.StatusCode} {resp.ReasonPhrase}: {bodySnippet}");
        // Propagate the remote status code to the caller and return the body and content-type
        request.HttpContext.Response.StatusCode = (int)resp.StatusCode;
        var contentType = resp.Content.Headers.ContentType?.ToString() ?? "application/json";
        return Results.Content(respText, contentType);
    }
    catch (Exception ex)
    {
        await Shared.Logger.LogAsync("api.log", $"Exception: {ex}");
        return Results.Problem(ex.Message);
    }
});

app.Run();
