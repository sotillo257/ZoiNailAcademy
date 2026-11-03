namespace ZoiNailAcademy.Application.Common.Interfaces;

/// <summary>Lifetimes of the tokens issued by the authentication use cases.</summary>
public interface IAuthTokenSettings
{
    TimeSpan AccessTokenLifetime { get; }
    TimeSpan RefreshTokenLifetime { get; }
}
