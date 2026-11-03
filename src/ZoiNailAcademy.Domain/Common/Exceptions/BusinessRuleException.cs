namespace ZoiNailAcademy.Domain.Common.Exceptions;

/// <summary>
/// A business rule of the contract was violated (HTTP 409 with <c>extensions.code</c>). Unlike
/// <see cref="DomainException"/> (invalid data, 400), the request is well-formed but conflicts with
/// the current state.
/// </summary>
public class BusinessRuleException : Exception
{
    public string Code { get; }
    public IReadOnlyDictionary<string, object?> Extensions { get; }

    public BusinessRuleException(string code, string message, IReadOnlyDictionary<string, object?>? extensions = null)
        : base(message)
    {
        Code = code;
        Extensions = extensions ?? new Dictionary<string, object?>();
    }
}
