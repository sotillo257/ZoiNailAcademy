using ZoiNailAcademy.Domain.Entities;

namespace ZoiNailAcademy.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    string Generate(User user);
}
