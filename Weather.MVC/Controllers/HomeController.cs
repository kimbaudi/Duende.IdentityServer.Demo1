using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.Json;
using Weather.MVC.Models;

namespace Weather.MVC.Controllers;

[Route("")]
public class HomeController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HomeController> _logger;

    public HomeController(IHttpClientFactory httpClientFactory, ILogger<HomeController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [Route("")]
    public IActionResult Index()
    {
        return View();
    }

    [Authorize]
    [Route("Client")]
    public IActionResult Client()
    {
        return View();
    }

    [Authorize]
    [Route("Weather")]
    public async Task<IActionResult> Weather()
    {
        var httpClient = _httpClientFactory.CreateClient();

        var token = await HttpContext.GetTokenAsync("access_token");
        if (token != null)
        {
            httpClient.SetBearerToken(token);
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

    [Route("Privacy")]
    public IActionResult Privacy()
    {
        return View();
    }

    [Route("Login")]
    public async Task Login(string? returlUrl = null)
    {
        var props = new AuthenticationProperties { RedirectUri = returlUrl };

        await HttpContext.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, props);
    }

    [Route("Signout")]
    public IActionResult Signout()
    {
        return SignOut(CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme);
    }

    [Route("Error")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}