namespace ZoiNailAcademy.Application.Common.Interfaces;

/// <summary>Application settings (<c>App:*</c>), implemented in Infrastructure.</summary>
public interface IAppSettings
{
    /// <summary>Public URL of the website, without trailing slash (<c>App:BaseUrl</c>).</summary>
    string BaseUrl { get; }

    /// <summary>Time zone dates are shown in (<c>App:TimeZone</c>, Europe/Madrid).</summary>
    TimeZoneInfo TimeZone { get; }
}
