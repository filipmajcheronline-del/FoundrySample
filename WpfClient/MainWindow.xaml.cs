using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;

namespace WpfClient;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
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
        }
        catch (Exception ex)
        {
            OutputBox.Text = "Error: " + ex.Message;
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
        }
        catch (Exception ex)
        {
            OutputBox.Text = "Error: " + ex.Message;
        }
    }
}
