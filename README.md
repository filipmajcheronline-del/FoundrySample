# Foundry WPF + API proxy sample

This repository contains two small projects:

- `ApiServer` - minimal ASP.NET Core proxy which accepts POST `/api/invoke` with JSON `{ "url": "...", "apiKey": "...", "input": "..." }` and forwards the request to the provided Foundry URL.
- `WpfClient` - simple WPF UI where you can enter the Foundry URL, API key, and input text; you can call Foundry directly or via the local API server.

Quick run (requires .NET 7+ SDK):

Open a Powershell terminal and run:

```powershell
dotnet run --project ApiServer --urls http://localhost:5000
```

In another terminal run the WPF app:

```powershell
dotnet run --project WpfClient
```

Usage notes:
- The proxy and client use a header named `api-key` when forwarding the API key. Some Foundry endpoints might expect `Authorization: Bearer <token>` or a different header  adjust `ApiServer/Program.cs` and `WpfClient/MainWindow.xaml.cs` accordingly.
- The sample sends a JSON body `{ "input": "..." }` to the Foundry URL. Change the payload shape as needed for your model endpoint.

Header name, logging and autotest
- **Header name**: The client UI now exposes a `Header name` field (defaults to `api-key`). To send an Authorization bearer token, set `Header name` to `Authorization` and either enter `Bearer <token>` in the API key field or the app will add the `Bearer ` prefix automatically.
- **Proxy payload**: The proxy accepts an optional `headerName` property in the JSON you post to `/api/invoke`. Example payload:

```
{
	"url": "https://your-foundry-endpoint",
	"apiKey": "<token-or-key>",
	"input": "your input",
	"headerName": "Authorization" // optional, defaults to "api-key"
}
```

- **Logs**: Both projects write rotating logs to the workspace `LOG` folder:
	- `LOG/api.log` — entries from the API proxy (incoming requests, forwarded responses, errors).
	- `LOG/wpfclient.log` — entries from the WPF client (direct and proxy calls, autotest runs).

- **Autotest**: The WPF client supports a non-interactive autotest mode which performs a quick direct + proxy POST and exits. Useful to verify connectivity and generate logs:

```powershell
dotnet run --project ApiServer --urls http://localhost:5000
dotnet run --project WpfClient -- --autotest
```

These commands start the API proxy and run the WPF autotest that issues one direct request and one proxied request; check `LOG` afterwards for results.
