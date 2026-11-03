using System.Security.Cryptography;
using System.Text;
using ZoiNailAcademy.Application.Common.Interfaces;

namespace ZoiNailAcademy.Infrastructure.Authentication;

public class Sha256RefreshTokenHasher : IRefreshTokenHasher
{
    private const int RawTokenSizeBytes = 32;

    public string GenerateRawToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(RawTokenSizeBytes);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public string Hash(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes);
    }
}
