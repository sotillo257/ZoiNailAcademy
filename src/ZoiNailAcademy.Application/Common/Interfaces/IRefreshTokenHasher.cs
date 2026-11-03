namespace ZoiNailAcademy.Application.Common.Interfaces;

public interface IRefreshTokenHasher
{
    string GenerateRawToken();
    string Hash(string rawToken);
}
