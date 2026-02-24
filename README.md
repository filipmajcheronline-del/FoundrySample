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
