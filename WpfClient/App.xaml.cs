using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace WpfClient;

public partial class App : Application
{
	protected override async void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);
		// If started with --autotest, run a quick Direct and Proxy test and exit.
		var args = Environment.GetCommandLineArgs();
		if (Array.Exists(args, a => a.Equals("--autotest", StringComparison.OrdinalIgnoreCase)))
		{
			try
			{
				var url = "https://httpbin.org/status/404";
				var apiKey = "autotestkey";
				var input = "autotest";
				// Direct POST
				using (var client = new HttpClient())
				using (var req = new HttpRequestMessage(HttpMethod.Post, url))
				{
					req.Headers.Add("api-key", apiKey);
					var payload = JsonSerializer.Serialize(new { input });
					req.Content = new StringContent(payload, Encoding.UTF8, "application/json");
					var resp = await client.SendAsync(req);
					Logger.Log($"Autotest Direct POST to {url} status={(int)resp.StatusCode}");
				}

				// Proxy POST
				using (var client = new HttpClient())
				{
					var proxyUrl = "http://localhost:5000/api/invoke";
					var payloadObj = new { url, apiKey, input, headerName = "api-key" };
					var json = JsonSerializer.Serialize(payloadObj);
					var resp = await client.PostAsync(proxyUrl, new StringContent(json, Encoding.UTF8, "application/json"));
					Logger.Log($"Autotest Proxy POST to {proxyUrl} -> status={(int)resp.StatusCode}");
				}
			}
			catch (Exception ex)
			{
				Logger.Log("Autotest exception: " + ex.Message);
			}
			// Exit the app after autotest
			Environment.Exit(0);
		}
	}
}

