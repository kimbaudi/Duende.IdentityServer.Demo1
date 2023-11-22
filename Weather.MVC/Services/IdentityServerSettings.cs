namespace Weather.MVC.Services;

public class IdentityServerSettings
{
    public string DiscoveryUrl { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientPassword { get; set; } = string.Empty;
    public bool UseHttps { get; set; }
}