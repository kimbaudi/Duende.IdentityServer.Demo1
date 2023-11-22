using IdentityModel.Client;

namespace Weather.MVC.Services;

public interface ITokenService
{
    Task<TokenResponse> GetTokenAsync(string scope);
}