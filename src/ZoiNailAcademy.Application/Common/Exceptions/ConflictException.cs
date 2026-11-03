namespace ZoiNailAcademy.Application.Common.Exceptions;

public class ConflictException : Exception
{
    /// <summary>Business error code of the contract (<c>extensions.code</c>), or null.</summary>
    public string? Code { get; }

    public IReadOnlyDictionary<string, object?> Extensions { get; }

    public ConflictException(string message) : base(message)
    {
        Extensions = new Dictionary<string, object?>();
    }

    public ConflictException(string code, string message, IReadOnlyDictionary<string, object?>? extensions = null)
        : base(message)
    {
        Code = code;
        Extensions = extensions ?? new Dictionary<string, object?>();
    }
}
