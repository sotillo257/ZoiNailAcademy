namespace ZoiNailAcademy.Application.Common.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string name, object key)
        : base($"No se encontró \"{name}\" ({key}).")
    {
    }
}
