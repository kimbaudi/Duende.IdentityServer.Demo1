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
