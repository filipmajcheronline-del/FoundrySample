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

        private void LoadFileBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new Microsoft.Win32.OpenFileDialog();
                dlg.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
                var ok = dlg.ShowDialog();
                if (ok == true)
                {
                    var path = dlg.FileName;
                    var text = System.IO.File.ReadAllText(path);
                    InputBox.Text = text;
                    Log($"Loaded input from file: {System.IO.Path.GetFileName(path)}");
                }
            }
            catch (Exception ex)
            {
                Log("LoadFile error: " + ex.Message);
                OutputBox.Text = "Error loading file: " + ex.Message;
            }
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
        var system = SystemBox.Text ?? string.Empty;

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
            // If calling Azure/OpenAI chat completions directly, wrap system+input in messages
            if (url.IndexOf("/openai/deployments/", StringComparison.OrdinalIgnoreCase) >= 0 && url.IndexOf("/chat/completions", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var messagesList = new System.Collections.Generic.List<object>();
                if (!string.IsNullOrWhiteSpace(system)) messagesList.Add(new { role = "system", content = system });
                messagesList.Add(new { role = "user", content = input });
                var chatPayload = JsonSerializer.Serialize(new { messages = messagesList });
                req.Content = new StringContent(chatPayload, Encoding.UTF8, "application/json");
            }
            else
            {
                var payload = JsonSerializer.Serialize(new { input });
                req.Content = new StringContent(payload, Encoding.UTF8, "application/json");
            }
            var resp = await client.SendAsync(req);
            var text = await resp.Content.ReadAsStringAsync();
            OutputBox.Text = ExtractAssistantContent(text);
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
            var system = SystemBox.Text ?? string.Empty;
            var payloadObj = new { url, apiKey, system, input, headerName = headerToUse };
            var json = JsonSerializer.Serialize(payloadObj);
            var resp = await client.PostAsync(proxyUrl, new StringContent(json, Encoding.UTF8, "application/json"));
            var text = await resp.Content.ReadAsStringAsync();
            OutputBox.Text = ExtractAssistantContent(text);
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

    private string ExtractAssistantContent(string respText)
    {
        if (string.IsNullOrWhiteSpace(respText)) return respText;
        try
        {
            using var doc = JsonDocument.Parse(respText);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array && choices.GetArrayLength() > 0)
            {
                var first = choices[0];
                if (first.ValueKind == JsonValueKind.Object)
                {
                    // Newer chat response: choices[0].message.content
                    if (first.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.Object && message.TryGetProperty("content", out var content))
                    {
                        if (content.ValueKind == JsonValueKind.String) return content.GetString() ?? respText;
                        return content.ToString();
                    }
                    // Older style: choices[0].text
                    if (first.TryGetProperty("text", out var textProp) && textProp.ValueKind == JsonValueKind.String)
                    {
                        return textProp.GetString() ?? respText;
                    }
                }
            }
        }
        catch { }
        return respText;
    }

    
}
