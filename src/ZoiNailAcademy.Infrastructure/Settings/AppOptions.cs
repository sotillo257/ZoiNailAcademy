namespace ZoiNailAcademy.Infrastructure.Settings;

/// <summary>Section <c>App</c>.</summary>
public class AppOptions
{
    public const string SectionName = "App";

    /// <summary>Public URL of the website (Next.js), used for Stripe's success and cancel URLs.</summary>
    public string BaseUrl { get; set; } = string.Empty;

    public string TimeZone { get; set; } = "Europe/Madrid";

    /// <summary>Shown in the emails (sender name, footer).</summary>
    public string NombreAcademia { get; set; } = "Zoi Nail Academy";

    /// <summary>Postal address in the footer of the emails (optional).</summary>
    public string DireccionAcademia { get; set; } = string.Empty;
}
