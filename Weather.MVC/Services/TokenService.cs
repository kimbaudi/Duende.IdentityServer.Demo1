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