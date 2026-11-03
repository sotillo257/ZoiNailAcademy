namespace ZoiNailAcademy.Infrastructure.Email;

/// <summary>Section <c>Email</c>.</summary>
public class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary><c>Resend</c> (default) or <c>Log</c>. Resend without API key or sender falls back to <c>Log</c>.</summary>
    public string Provider { get; set; } = EmailProviders.Resend;

    /// <summary>Sender, e.g. <c>Zoi Nail Academy &lt;reservas@dominio&gt;</c> (domain verified in Resend).</summary>
    public string From { get; set; } = string.Empty;

    /// <summary>The client's email: Reply-To of the buyers' emails.</summary>
    public string ReplyTo { get; set; } = string.Empty;

    /// <summary>Where the client's notices go.</summary>
    public string AvisosClienta { get; set; } = string.Empty;

    public bool AcuseContacto { get; set; }

    /// <summary>The <c>Log</c> provider also writes each email as an HTML file here (development only; empty = no files).</summary>
    public string DirectorioLog { get; set; } = string.Empty;
}

/// <summary>Section <c>Resend</c>.</summary>
public class ResendOptions
{
    public const string SectionName = "Resend";

    /// <summary>Secret (<c>re_…</c>).</summary>
    public string ApiKey { get; set; } = string.Empty;
}

public static class EmailProviders
{
    public const string Resend = "Resend";
    public const string Log = "Log";
}
