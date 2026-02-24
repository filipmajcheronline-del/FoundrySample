using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

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

    try
    {
        using var client = new HttpClient();
        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        // Forward API key in header named 'api-key'. Some Foundry endpoints may require different headers
        req.Headers.Add("api-key", apiKey);

        var payload = JsonSerializer.Serialize(new { input });
        req.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        using var resp = await client.SendAsync(req);
        var respText = await resp.Content.ReadAsStringAsync();
        return Results.Content(respText, resp.Content.Headers.ContentType?.ToString() ?? "application/json");
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

app.Run();
