﻿using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
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

    [Authorize]
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

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}