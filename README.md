
<h1 align="center">💠 Cidaas .NET SDK</h1>
<h3 align="center">Hybrid Authentication (OIDC + Ahamatic) for ASP.NET Core</h3>

<p align="center">
  Developed by <a href="https://ahastudio.io/" target="_blank">ahaStudio</a>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-8.0-blueviolet?logo=dotnet&logoColor=white" />
  <img src="https://img.shields.io/badge/NuGet-Cidaas.Net.Sdk.Core-success?logo=nuget" />
  <img src="https://img.shields.io/badge/License-MIT-green" />
  <img src="https://img.shields.io/badge/Build-Passing-brightgreen?logo=githubactions" />
</p>

---

## 📖 Overview

The **Cidaas .NET SDK** (`Cidaas.Net.Sdk.Core`) is a **NuGet** package designed for **ASP.NET Core** applications that require secure federated authentication using **OpenID Connect (OIDC)** through **Cidaas**, complemented by **Ahamatic**, an extended authorization and validation service.

The goal is to provide a **dual authentication architecture** with:
- 🚀 Unified user authentication across enterprise systems.  
- 🔄 Automatic token renewal.  
- 🧱 Seamless integration into the ASP.NET Core pipeline.  
- 🔐 Centralized session and access policy management.  

---

## 🧩 Key Features

| Feature | Description | Core Components |
|----------|--------------|-----------------|
| 🔐 **OIDC Authentication (Cidaas)** | Full OpenID Connect flow (login, logout, refresh). | `CidaasAuthService`, `CidaasOptions` |
| ⚙️ **Ahamatic Integration** | Adds permissions, roles, and extended validation. | `AhamaticAuthService`, `AhamaticOptions` |
| 🍪 **Secure Session Handling** | Stores tokens and claims in secure cookies (SameSite, HttpOnly). | `AuthenticationProperties` |
| ♻️ **Automatic Token Renewal** | Keeps sessions active by refreshing tokens automatically. | `StartTokenRenewal()` |
| 🧱 **ASP.NET Core Integration** | Easily injects into the authentication pipeline. | `AddCidaasAuth()`, `AddAhamaticAuth()` |
| 🧠 **Multi-Environment Support** | Modular configuration for dev, staging, and production. | `appsettings.{Environment}.json` |

---

## 🧱 Architecture

The SDK follows a **decoupled dual-authentication pattern**, where each provider handles its own responsibilities.

```text
┌────────────────────────────┐
│        ASP.NET Core        │
│  Middleware / Controllers  │
└────────────┬───────────────┘
             │
             ▼
┌────────────────────────────┐
│     CidaasAuthService      │
│  - Login (OIDC)            │
│  - Token Handling          │
│  - Refresh Tokens          │
└────────────┬───────────────┘
             │
             ▼
┌────────────────────────────┐
│     AhamaticAuthService    │
│  - Role Validation         │
│  - Custom Claims           │
│  - Token Synchronization   │
└────────────────────────────┘
```

Both authentication flows work together to create a **combined user identity** within the ASP.NET Core authentication context.

---

## ⚙️ Installation

Install the NuGet package:

```bash
dotnet add package Cidaas.Net.Sdk.Core --version 1.0.22
```

**Requirements:**
- .NET 8.0 or higher  
- ASP.NET Core Authentication  
- Network access to external APIs  

---

## 🔧 Configuration (`appsettings.json`)

```json
{
  "Cidaas": {
    "ClientId": "your-client-id",
    "Issuer": "https://cidaas.example.com",
    "RedirectUri": "https://yourapp.com/signin-oidc",
    "Scopes": ["openid", "profile", "email"]
  },
  "Ahamatic": {
    "ApiBaseUrl": "https://api.ahamtic.com",
    "ApiKey": "your-api-key",
    "ModuleName": "YourModule",
    "ApplicationCode": "APP123"
  }
}
```

### Register in `Program.cs`

```csharp
builder.Services.AddCidaasAuth(builder.Configuration.GetSection("Cidaas"));
builder.Services.AddAhamaticAuth(builder.Configuration.GetSection("Ahamatic"));

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
```

---

## 🧠 Project Structure

```
cidaas_net_sdk/
├── Core/
│   ├── CidaasAuthService.cs
│   ├── AhamaticAuthService.cs
│   ├── Models/
│   │   └── AhamaticAccountData.cs
├── Options/
│   ├── CidaasOptions.cs
│   └── AhamaticOptions.cs
├── Extensions/
│   └── AuthenticationExtensions.cs
└── README.md
```

---

## 🔄 Authentication Flow

1. User logs in through **Cidaas OIDC**.  
2. The obtained token is exchanged with **Ahamatic** for extended validation.  
3. Combined user data is stored in the **ASP.NET Core authentication context**.  
4. The SDK monitors token expiration and renews it automatically.

---

## 🧪 Example Usage

```csharp
[Authorize]
public class DashboardController : Controller
{
    private readonly AhamaticAuthService _ahamtic;

    public DashboardController(AhamaticAuthService ahamtic)
    {
        _ahamtic = ahamtic;
    }

    public async Task<IActionResult> Index()
    {
        var userData = await _ahamtic.GetAccountDataAsync();
        return View(userData);
    }
}
```

---

## 🧰 Suggested CI/CD

Integrate with **GitHub Actions**:

```yaml
name: Build and Test

on: [push, pull_request]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      - name: Restore dependencies
        run: dotnet restore
      - name: Build
        run: dotnet build --configuration Release --no-restore
      - name: Test
        run: dotnet test --no-build --verbosity normal
```

---

## 🛡️ License

This project is licensed under the **MIT License**.  
See the `LICENSE` file for details.

---

<p align="center">
  Built with ❤️ by <strong>ahaStudio</strong> • <a href="https://ahastudio.com">ahastudio.com</a>
</p>
