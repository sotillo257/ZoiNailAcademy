namespace ZoiNailAcademy.Application.Common.Csv;

/// <summary>A CSV export ready to be returned as a file download.</summary>
public sealed record ArchivoCsv(string NombreArchivo, byte[] Contenido);
