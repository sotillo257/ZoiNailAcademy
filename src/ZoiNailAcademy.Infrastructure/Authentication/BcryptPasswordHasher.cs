using ZoiNailAcademy.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace ZoiNailAcademy.Infrastructure.Authentication;

public class BcryptPasswordHasher(IOptions<PasswordHasherOptions> options) : IPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, options.Value.WorkFactor);

    public bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}
