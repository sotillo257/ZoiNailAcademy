namespace ZoiNailAcademy.Application.Common.Interfaces;

/// <summary>Email settings (<c>Email:*</c>), implemented in Infrastructure.</summary>
public interface IEmailSettings
{
    /// <summary>The client's email: Reply-To of the buyers' emails and the contact shown in them (<c>Email:ReplyTo</c>).</summary>
    string? ReplyTo { get; }

    /// <summary>Where the client's notices go (<c>Email:AvisosClienta</c>). Empty: those emails are skipped.</summary>
    string? AvisosClienta { get; }

    /// <summary>Send an acknowledgement to whoever uses the contact form (<c>Email:AcuseContacto</c>, off by default).</summary>
    bool AcuseContacto { get; }
}
