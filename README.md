# Duende IdentityServer Demo 1

## Setup

I'm using .NET Core 6.

list donet sdks and make sure version 6 is available.
in my case, i have 6.0.412 so I use that.

```shell
dotnet --list-sdks
# 6.0.412 [C:\Program Files\dotnet\sdk]
# 7.0.403 [C:\Program Files\dotnet\sdk]
```

create a new global.json file in the directory where you will create the solution.
in my case, I will create the solution under D:\dev\dotnet folder
so I'll add the global.json folder in the dotnet folder

```shell
# create a new global.json file
dotnet new globaljson
```

edit global.json to target the version 6 sdk.
you should now have a global.json folder in D:\dev\dotnet folder

```json
{
  "sdk": {
    "version": "6.0.412"
  }
}
```

open the Command Prompt and make sure you are under D:\dev\dotnet directory.
uninstall and install/reinistall the duende identityserver template.
run `dotnet new --list` to ensure that the template is installed.

```shell
# uninstall
dotnet new --uninstall Duende.IdentityServer.Templates

# install
dotnet new --install Duende.IdentityServer.Templates

# list templates
dotnet new --list
```

first, create an empty solution project called `Duende.IdentityServer.Demo1`.
then cd into that directory.

```shell
cd /d/dev/dotnet
dotnet new sln -o Duende.IdentityServer.Demo1
cd Duende.IdentityServer.Demo1
```

create a new project using the in-memory store template.
I called mine `Duende.IdentityServer.InMemory`

```shell
cd /d/dev/dotnet/Duende.IdentityServer.Demo1
dotnet new isinmem -o Duende.IdentityServer.InMemory
```

open the solution in Visual Studio by double-clicking the `Duende.IdentityServer.Demo1.sln` file.
add existing project `Duende.IdentityServer.InMemory` to the solution.

## Demo 1

let's see if we can get an access token.

run `Duende.IdentityServer.InMemory`.

execute the following command in a shell using curl to get an access token

```shell
curl -X POST -H "content-type: application/x-www-form-urlencoded" -H "Cache-Control: no-cache" -d "client_id=m2m.client&scope=scope1&client_secret=511536EF-F270-4058-80CA-1C89C192F69A&grant_type=client_credentials" "https://localhost:5001/connect/token"
```

or use Postman to get access token

![Postman get access token](img/postman-get-access-token.png)

## Demo 2

Let's add a webapi project so we can protect the api w/ a bearer token from Duende Identity Server.

Right-click solution and add new webapi project called `Weather.API`

open `LaunchSettings.json` file and take note of the port number. In my case, the port is 7074 for https. You can change it, but make sure to keep note of it.

Open `WeatherForecastController.cs` and add the `[Authorize]` attribute to the controller.

Next, install `Microsoft.AspNetCore.Authentication.JwtBearer`.

```shell
# install using pmc
Install-Package Microsoft.AspNetCore.Authentication.JwtBearer -Version 6.0.25

# uninstall using pmc
Uninstall-Package Microsoft.AspNetCore.Authentication.JwtBearer
```

Next, edit `Program.cs` and configure authentication

Program.cs

```cs
builder.Services.AddSwaggerGen();

// begin add1
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        //the url of IdentityServer
        options.Authority = "https://localhost:5001";
        // name of the audience
        options.Audience = "weatherapi";

        options.TokenValidationParameters.ValidTypes = new[] { "at+jwt" };
    });
// end add1

var app = builder.Build();

// ...

app.UseHttpsRedirection();

// begin add2
app.UseAuthentication();
// end add2
app.UseAuthorization();

app.MapControllers();
```

Open `Config.cs` file in `Duende.IdentityServer.InMemory` project and add the following:

```cs
public static IEnumerable<ApiResource> ApiResources =>
    new ApiResource[]
    {
        new("weatherapi")
        {
            Scopes = { "scope1" },
            ApiSecrets = { new Secret("ScopeSecret".Sha256()) }
        }
    };
```

Then open `Program.cs` file in `Duende.IdentityServer.InMemory` project and add the following:

```cs
// in-memory, code config
isBuilder.AddInMemoryIdentityResources(Config.IdentityResources);
isBuilder.AddInMemoryApiScopes(Config.ApiScopes);
isBuilder.AddInMemoryClients(Config.Clients);
// add this
isBuilder.AddInMemoryApiResources(Config.ApiResources);
```

Now right-click on the solution and configure both `Duende.IdentityServer.InMemory` and `Weather.API` as startup projects. Run the projects afterwards.

Use curl or Postman to get the weatherforecast

```shell
# this won't work
curl -X GET -H "Cache-Control: no-cache" "https://localhost:7074/weatherforecast"
```

```shell
# get the token
curl -X POST -H "content-type: application/x-www-form-urlencoded" -H "Cache-Control: no-cache" -d "client_id=m2m.client&scope=scope1&client_secret=511536EF-F270-4058-80CA-1C89C192F69A&grant_type=client_credentials" "https://localhost:5001/connect/token"

# replace <ADDTOKENHERE> w/ the access token
curl -X GET -H "Authorization: Bearer <ADDTOKENHERE>" -H "Cache-Control: no-cache" "https://localhost:7074/weatherforecast"

# example here
curl -X GET -H "Authorization: Bearer eyJhbGciOiJSUzI1NiIsImtpZCI6IkQyNzY4NkVDOTJEOEZCQkZGNjYxREFFRjY4NkUwOTMzIiwidHlwIjoiYXQrand0In0.eyJpc3MiOiJodHRwczovL2xvY2FsaG9zdDo1MDAxIiwibmJmIjoxNzAwNTg5MzY1LCJpYXQiOjE3MDA1ODkzNjUsImV4cCI6MTcwMDU5Mjk2NSwiYXVkIjpbIndlYXRoZXJhcGkiLCJodHRwczovL2xvY2FsaG9zdDo1MDAxL3Jlc291cmNlcyJdLCJzY29wZSI6WyJzY29wZTEiXSwiY2xpZW50X2lkIjoibTJtLmNsaWVudCIsImp0aSI6IjQ3QjdDNzFBOUIyRERCQ0Q4NURGNkI1NTlEN0I2RTVDIn0.A_FVKb-VCNWvZm60dXZOyEPWtmB4UfZj-_C2RdjtYTNPzUgQkFte4NZ53kvnEe3sRCWAESoHzFYxOpewDpywFbOUontcY1dZEbJH-NxY16B8ofNrNgR7YHuVx28OXJinoGNohxr-Z_OVniQoHL09sBPsXy8lyN4B_esMuXtZiykRf-8p51gwHZZZhwGYsxv4yCnm06f5ac4DqJjIhIu6QcFCGBV4KdYv3baZUNIiXzgzSu3wh6K9QysNWmgPEETbG1shTMbzmzer1IZf8HvoxkEJzlnrt87HEvAIlZvaluLBlISsrVdxgfj1nNkHn0-qgmjfe_-Ya4V6lm5tkBhhhQ" -H "Cache-Control: no-cache" "https://localhost:7074/weatherforecast"
```

## Demo 3

Now let's create a MVC web application called `Weather.MVC` that accesses the protected Weather.API endpoint by requesting a token from Identity Server.

In my case, `Weather.MVC` web app is running on port 7267.

Open `Program.cs` and add the following code:

```cs
var builder = WebApplication.CreateBuilder(args);

// add this
builder.Services.AddHttpClient();
```

Create `WeatherData.cs` file in `Models` folder:

```cs
namespace Weather.MVC.Models;

public class WeatherData
{
    public DateTime Date { get; set; }
    public int TemperatureC { get; set; }
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    public string Summary { get; set; }
}
```

Open `HomeController.cs` and edit the code to the following:

```cs
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.Json;
using Weather.MVC.Models;

namespace Weather.MVC.Controllers;

public class HomeController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HomeController> _logger;

    public HomeController(IHttpClientFactory httpClientFactory, ILogger<HomeController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public async Task<IActionResult> Weather()
    {
        var httpClient = _httpClientFactory.CreateClient();

        var httpResponseMessage = await httpClient.GetAsync("https://localhost:7074/weatherforecast");
        if (httpResponseMessage.IsSuccessStatusCode)
        {
            using var contentStream = await httpResponseMessage.Content.ReadAsStreamAsync();
            var result = await JsonSerializer.DeserializeAsync<IEnumerable<WeatherData>>(contentStream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return View(result);
        }

        throw new Exception("Unable to get content");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
```

Under `View/Home/` folder, create `Weather.cshtml` and add the following:

```html
@model IEnumerable<WeatherData>
@{
    ViewData["Title"] = "Weather Page";
}

<h2>Weather</h2>

<table>
    @foreach (var item in Model)
    {
        <tr>
            <td>@item.Date</td>
            <td>@item.Summary</td>
            <td>@item.TemperatureC</td>
            <td>@item.TemperatureF</td>
        </tr>
    }
</table>
```

To test that the MVC webapp is able to hit the API endpoint, open `WeatherForecastController.cs` in the `Weather.API` project and comment out `[Authorize]` attribute.

Configure startup projects to include `Weather.MVC`.
Start the applications and go to <https://localhost:7267/home/weather>.
You should see the Weather table.

Now let's uncomment `[Authorize]` attribute in `WeatherForecastController.cs` file.
If we try to access <https://localhost:7267/home/weather>, we will now hit an exception.
Let's now fix that.

First, install `IdentityModel` nuget package.

```shell
# install using pmc
Install-Package IdentityModel -Version 6.2.0

# uninstall using pmc
Uninstall-Package IdentityModel
```

In `Weather.MVC` web application, add a new folder `Services`.
Then add files `IdentityServerSettings.cs`, `ITokenService.cs` and `TokenService.cs`.

IdentityServerSettings.cs

```cs
namespace Weather.MVC.Services;

public class IdentityServerSettings
{
    public string DiscoveryUrl { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientPassword { get; set; } = string.Empty;
    public bool UseHttps { get; set; }
}
```

ITokenService.cs

```cs
using IdentityModel.Client;

namespace Weather.MVC.Services;

public interface ITokenService
{
    Task<TokenResponse> GetTokenAsync(string scope);
}
```

TokenService.cs

```cs
using IdentityModel.Client;
using Microsoft.Extensions.Options;

namespace Weather.MVC.Services;

public class TokenService : ITokenService
{
    private readonly ILogger<TokenService> _logger;
    private readonly IOptions<IdentityServerSettings> _identityServerSettings;
    private readonly DiscoveryDocumentResponse _discoveryDocument;

    public TokenService(ILogger<TokenService> logger, IOptions<IdentityServerSettings> identityServerSettings)
    {
        _logger = logger;
        _identityServerSettings = identityServerSettings;

        using var httpClient = new HttpClient();
        _discoveryDocument = httpClient.GetDiscoveryDocumentAsync(identityServerSettings.Value.DiscoveryUrl).Result;
        if (_discoveryDocument.IsError)
        {
            _logger.LogError("Unable to get discovery document. Error is {Error}", _discoveryDocument.Error);
            throw new Exception("Unable to get discovery document", _discoveryDocument.Exception);
        }
    }

    public async Task<TokenResponse> GetTokenAsync(string scope)
    {
        using var client = new HttpClient();
        var tokenResponse = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
            Address = _discoveryDocument.TokenEndpoint,

            ClientId = _identityServerSettings.Value.ClientId,
            ClientSecret = _identityServerSettings.Value.ClientPassword,
            Scope = scope
        });

        if (tokenResponse.IsError)
        {
            _logger.LogError("Unable to get token. Error is {Error}", tokenResponse.Error);
            throw new Exception("Unable to get token", tokenResponse.Exception);
        }

        return tokenResponse;
    }
}
```

Open `appsettings.json` (or better yet, use user secrets `secrets.json`)
and add the following configuration

```json
{
  "IdentityServerSettings": {
    "DiscoveryUrl": "https://localhost:5001",
    "ClientId": "m2m.client",
    "ClientPassword": "511536EF-F270-4058-80CA-1C89C192F69A",
    "UseHttps": true
  },
}
```

Open `Program.cs` and add the following:

```cs
builder.Services.AddHttpClient();
// begin add
builder.Services.Configure<IdentityServerSettings>(builder.Configuration.GetSection(nameof(IdentityServerSettings)));
builder.Services.AddSingleton<ITokenService, TokenService>();
// end add
```

Open `HomeController.cs` and edit to the following:

```cs
using IdentityModel.Client;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.Json;
using Weather.MVC.Models;
using Weather.MVC.Services;

namespace Weather.MVC.Controllers;

public class HomeController : Controller
{
    private readonly ITokenService _tokenService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HomeController> _logger;

    public HomeController(ITokenService tokenService, IHttpClientFactory httpClientFactory, ILogger<HomeController> logger)
    {
        _tokenService = tokenService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public async Task<IActionResult> Weather()
    {
        var httpClient = _httpClientFactory.CreateClient();

        var token = await _tokenService.GetTokenAsync("scope1");
        if (token.AccessToken != null)
        {
            httpClient.SetBearerToken(token.AccessToken);
        }

        var httpResponseMessage = await httpClient.GetAsync("https://localhost:7074/weatherforecast");
        if (httpResponseMessage.IsSuccessStatusCode)
        {
            using var contentStream = await httpResponseMessage.Content.ReadAsStreamAsync();
            var result = await JsonSerializer.DeserializeAsync<IEnumerable<WeatherData>>(contentStream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return View(result);
        }

        throw new Exception("Unable to get content");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
```

So now we are able to access the protected weather api endpoint from our weather mvc web application.
However, the MVC web application is not protected.

To protect `Weather.MVC` MVC web application, add `[Authorize]` attribute to the Weather action method in `HomeController.cs` file.

Then open `appsettings.json` file in the mvc app and add the following configuration

```json
{
  "InteractiveServiceSettings": {
    "AuthorityUrl": "https://localhost:5001",
    "ClientId": "interactive",
    "ClientSecret": "49C1A7E1-0C79-4A89-A3D6-A37998FB86B0",
    "Scopes": [
      "scope1"
    ]
  },
}
```

Open `Config.cs` file in `Duende.IdentityServer.InMemory` add edit the ports for the interactive client to match the port of the MVC web app (port 7267). Also, make sure `scope1` is included in the AllowedScopes:

Config.cs

```cs
// interactive client using code flow + pkce
new Client
{
    ClientId = "interactive",
    ClientSecrets = { new Secret("49C1A7E1-0C79-4A89-A3D6-A37998FB86B0".Sha256()) },
        
    AllowedGrantTypes = GrantTypes.Code,

    // change ports to 7267 (so it matches the port of the MVC web app)
    RedirectUris = { "https://localhost:7267/signin-oidc" },
    FrontChannelLogoutUri = "https://localhost:7267/signout-oidc",
    PostLogoutRedirectUris = { "https://localhost:7267/signout-callback-oidc" },

    AllowOfflineAccess = true,
    AllowedScopes = { "openid", "profile", "scope1" }
},
```

In the `Weather.MVC` application, install the `Microsoft.AspNetCore.Authentication.OpenIdConnect` nuget package

```shell
Install-Package Microsoft.AspNetCore.Authentication.OpenIdConnect -Version 6.0.25

Uninstall-Package Microsoft.AspNetCore.Authentication.OpenIdConnect
```

Open `Program.cs` file configure authentication

Program.cs

```cs
builder.Services.AddHttpClient();
builder.Services.Configure<IdentityServerSettings>(builder.Configuration.GetSection("IdentityServerSettings"));
builder.Services.AddSingleton<ITokenService, TokenService>();

// begin add
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
    {
        options.Authority = builder.Configuration["InteractiveServiceSettings:AuthorityUrl"];
        options.ClientId = builder.Configuration["InteractiveServiceSettings:ClientId"];
        options.ClientSecret = builder.Configuration["InteractiveServiceSettings:ClientSecret"];
        options.Scope.Add(builder.Configuration["InteractiveServiceSettings:Scopes:0"]);

        options.ResponseType = "code";
        options.UsePkce = true;
        options.ResponseMode = "query";
        options.SaveTokens = true;
    });
// end add


app.UseRouting();

// begin add
app.UseAuthentication();
// end add
app.UseAuthorization();
```

So now that we have client authentication setup, the client will be able to access certain scopes.
And as part of this authentication, it will get back an access token.

Now that we are getting back an access token as part of our user authentication, we no longer need to use the TokenService to get the token.

So update the Weather action method in `HomeController.cs` to get the token from the http context:

```cs
[Authorize]
public async Task<IActionResult> Weather()
{
    var httpClient = _httpClientFactory.CreateClient();

    var token = await HttpContext.GetTokenAsync("access_token");
    if (token != null)
    {
        httpClient.SetBearerToken(token);
    }
    // var token = await _tokenService.GetTokenAsync("scope1");
    // if (token.AccessToken != null)
    // {
    //     httpClient.SetBearerToken(token.AccessToken);
    // }

    var httpResponseMessage = await httpClient.GetAsync("https://localhost:7074/weatherforecast");
    if (httpResponseMessage.IsSuccessStatusCode)
    {
        using var contentStream = await httpResponseMessage.Content.ReadAsStreamAsync();
        var result = await JsonSerializer.DeserializeAsync<IEnumerable<WeatherData>>(contentStream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return View(result);
    }

    throw new Exception("Unable to get content");
}
```
