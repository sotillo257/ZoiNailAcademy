namespace ZoiNailAcademy.Application.Common.Exceptions;

/// <summary>A business limit was reached (HTTP 429 with <c>extensions.code</c>).</summary>
public class TooManyRequestsException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
