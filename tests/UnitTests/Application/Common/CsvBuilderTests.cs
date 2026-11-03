using System.Text;
using FluentAssertions;
using ZoiNailAcademy.Application.Common.Csv;

namespace UnitTests.Application.Common;

public class CsvBuilderTests
{
    [Theory]
    [InlineData("Lucía", "Lucía")]
    [InlineData("", "")]
    [InlineData(null, "")]
    [InlineData("Dice \"hola\"", "\"Dice \"\"hola\"\"\"")]
    [InlineData("uno;dos", "\"uno;dos\"")]
    [InlineData("línea 1\nlínea 2", "\"línea 1\nlínea 2\"")]
    [InlineData("línea 1\r\nlínea 2", "\"línea 1\r\nlínea 2\"")]
    [InlineData("=SUMA(A1:A2)", "'=SUMA(A1:A2)")]
    [InlineData("+34612345678", "'+34612345678")]
    [InlineData("-5", "'-5")]
    [InlineData("@cmd", "'@cmd")]
    [InlineData("\tinicio", "'\tinicio")]
    [InlineData("=1;2", "\"'=1;2\"")]
    [InlineData("a=b", "a=b")]
    public void Escapar_QuotesSpecialCharactersAndNeutralizesFormulas(string? campo, string esperado)
    {
        CsvBuilder.Escapar(campo).Should().Be(esperado);
    }

    [Fact]
    public void ToBytes_StartsWithTheBomAndSeparatesWithSemicolonsAndCrlf()
    {
        var bytes = new CsvBuilder("Curso", "Teléfono").Fila("Acrílico", "+34612345678").ToBytes();

        bytes.Take(3).Should().Equal(0xEF, 0xBB, 0xBF);
        Encoding.UTF8.GetString(bytes[3..]).Should().Be("Curso;Teléfono\r\nAcrílico;'+34612345678\r\n");
    }
}
