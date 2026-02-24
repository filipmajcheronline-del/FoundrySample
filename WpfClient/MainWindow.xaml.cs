using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.IO;

namespace WpfClient;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

        private void Log(string text)
        {
            Logger.Log(text);
        }

    private async void DirectBtn_Click(object sender, RoutedEventArgs e)
    {
        OutputBox.Text = "Calling Foundry directly...";
        var url = UrlBox.Text.Trim();
        var apiKey = KeyBox.Text.Trim();
        var headerName = HeaderBox.Text.Trim();
        var input = InputBox.Text ?? string.Empty;

        if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(apiKey))
        {
            OutputBox.Text = "Provide both URL and API key.";
            return;
        }

        try
        {
            using var client = new HttpClient();
            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            var headerToUse = string.IsNullOrWhiteSpace(headerName) ? "api-key" : headerName;
            if (headerToUse.Equals("Authorization", StringComparison.OrdinalIgnoreCase) && !apiKey.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                req.Headers.Add("Authorization", "Bearer " + apiKey);
            }
            else
            {
                req.Headers.Add(headerToUse, apiKey);
            }
            var payload = JsonSerializer.Serialize(new { input });
            req.Content = new StringContent(payload, Encoding.UTF8, "application/json");
            var resp = await client.SendAsync(req);
            var text = await resp.Content.ReadAsStringAsync();
            OutputBox.Text = text;
            Log($"Direct POST to {url} header={headerToUse} apiKey={(apiKey.Length>4?"****":"<short>")} status={(int)resp.StatusCode}");
        }
        catch (Exception ex)
        {
            OutputBox.Text = "Error: " + ex.Message;
            Log("Direct exception: " + ex.Message);
        }
    }

    private async void ProxyBtn_Click(object sender, RoutedEventArgs e)
    {
        OutputBox.Text = "Calling local API server...";
        var url = UrlBox.Text.Trim();
        var apiKey = KeyBox.Text.Trim();
        var headerName = HeaderBox.Text.Trim();
        var input = InputBox.Text ?? string.Empty;

        if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(apiKey))
        {
            OutputBox.Text = "Provide both URL and API key.";
            return;
        }

        try
        {
            using var client = new HttpClient();
            var proxyUrl = "http://localhost:5000/api/invoke";
            var headerToUse = string.IsNullOrWhiteSpace(headerName) ? "api-key" : headerName;
            var payloadObj = new { url, apiKey, input, headerName = headerToUse };
            var json = JsonSerializer.Serialize(payloadObj);
            var resp = await client.PostAsync(proxyUrl, new StringContent(json, Encoding.UTF8, "application/json"));
            var text = await resp.Content.ReadAsStringAsync();
            OutputBox.Text = text;
            Log($"Proxy POST to {proxyUrl} -> target {url} header={headerToUse} status={(int)resp.StatusCode}");
        }
        catch (Exception ex)
        {
            OutputBox.Text = "Error: " + ex.Message;
            Log("Proxy exception: " + ex.Message);
        }
    }

    private string PrettyJson(string input)
    {
        try
        {
            using var doc = JsonDocument.Parse(input);
            var options = new JsonSerializerOptions { WriteIndented = true };
            return JsonSerializer.Serialize(doc.RootElement, options);
        }
        catch
        {
            return input;
        }
    }

    private async void ListBtn_Click(object sender, RoutedEventArgs e)
    {
        ModelsBox.Text = "Listing models...";
        var url = UrlBox.Text.Trim();
        var apiKey = KeyBox.Text.Trim();
        var headerName = HeaderBox.Text.Trim();
        var useProxy = UseProxyForList.IsChecked == true;
        var modelsPath = ModelsPathBox.Text?.Trim();
        if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(apiKey))
        {
            ModelsBox.Text = "Provide both URL and API key.";
            return;
        }

        try
        {
            using var client = new HttpClient();
            if (!string.IsNullOrEmpty(modelsPath))
            {
                // Use explicit models path provided by user
                var target = url.TrimEnd('/') + (modelsPath.StartsWith("/") ? modelsPath : "/" + modelsPath);
                using var req = new HttpRequestMessage(HttpMethod.Get, target);
                var headerToUse = string.IsNullOrWhiteSpace(headerName) ? "api-key" : headerName;
                if (headerToUse.Equals("Authorization", StringComparison.OrdinalIgnoreCase) && !apiKey.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    req.Headers.Add("Authorization", "Bearer " + apiKey);
                else
                    req.Headers.Add(headerToUse, apiKey);

                var resp = await client.SendAsync(req);
                var text = await resp.Content.ReadAsStringAsync();
                ModelsBox.Text = PrettyJson(text);
                Log($"List direct to {target} header={headerToUse} status={(int)resp.StatusCode}");
                return;
            }
            if (useProxy)
            {
                var proxyUrl = "http://localhost:5000/api/invoke";
                var headerToUse = string.IsNullOrWhiteSpace(headerName) ? "api-key" : headerName;
                var payloadObj = new { url, apiKey, input = "", headerName = headerToUse, method = "GET" };
                var json = JsonSerializer.Serialize(payloadObj);
                var resp = await client.PostAsync(proxyUrl, new StringContent(json, Encoding.UTF8, "application/json"));
                var text = await resp.Content.ReadAsStringAsync();
                ModelsBox.Text = PrettyJson(text);
                Log($"List via proxy to {url} header={headerToUse} status={(int)resp.StatusCode}");
            }
            else
            {
                // Build candidate endpoints. Prefer Azure Foundry / OpenAI-style endpoints when detected.
                var baseUrl = url.TrimEnd('/');
                var candidatesList = new System.Collections.Generic.List<string>();
                try
                {
                    if (Uri.TryCreate(baseUrl, UriKind.Absolute, out var u))
                    {
                        var host = u.Host ?? string.Empty;
                        if (host.Contains("services.ai.azure.com", StringComparison.OrdinalIgnoreCase) || host.Contains("openai.azure.com", StringComparison.OrdinalIgnoreCase) || baseUrl.Contains("/openai/", StringComparison.OrdinalIgnoreCase))
                        {
                            // Azure Foundry / Azure OpenAI style
                            candidatesList.Add(baseUrl + "/openai/deployments?api-version=2023-11-15");
                            candidatesList.Add(baseUrl + "/openai/models?api-version=2023-11-15");
                            candidatesList.Add(baseUrl + "/openai/deployments?api-version=2023-05-15");
                        }
                    }
                }
                catch { }
                // Generic fallbacks
                candidatesList.Add(baseUrl + "/deployments");
                candidatesList.Add(baseUrl + "/models");
                candidatesList.Add(baseUrl);
                var candidates = candidatesList.ToArray();
                HttpResponseMessage? lastResp = null;
                string lastText = "";
                foreach (var target in candidates)
                {
                    using var req = new HttpRequestMessage(HttpMethod.Get, target);
                    var headerToUse = string.IsNullOrWhiteSpace(headerName) ? "api-key" : headerName;
                    if (headerToUse.Equals("Authorization", StringComparison.OrdinalIgnoreCase) && !apiKey.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                        req.Headers.Add("Authorization", "Bearer " + apiKey);
                    else
                        req.Headers.Add(headerToUse, apiKey);

                    var resp = await client.SendAsync(req);
                    lastResp = resp;
                    lastText = await resp.Content.ReadAsStringAsync();
                    if (resp.IsSuccessStatusCode)
                    {
                        ModelsBox.Text = PrettyJson(lastText);
                        Log($"List direct to {target} header={headerToUse} status={(int)resp.StatusCode}");
                        return;
                    }
                }
                ModelsBox.Text = PrettyJson(lastText);
                Log($"List direct attempts finished, last status={(int)(lastResp?.StatusCode ?? 0)}");
            }
        }
        catch (Exception ex)
        {
            ModelsBox.Text = "Error: " + ex.Message;
            Log("List exception: " + ex.Message);
        }
    }
}
