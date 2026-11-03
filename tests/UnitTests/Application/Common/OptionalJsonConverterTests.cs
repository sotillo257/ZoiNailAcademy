using System.Text.Json;
using FluentAssertions;
using ZoiNailAcademy.Application.Common;

namespace UnitTests.Application.Common;

public class OptionalJsonConverterTests
{
    private sealed record TestCommand(Optional<string?> Name);

    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new OptionalJsonConverterFactory() }
    };

    [Fact]
    public void Deserialize_OmittedProperty_HasValueIsFalse()
    {
        var command = JsonSerializer.Deserialize<TestCommand>("{}", Options)!;

        command.Name.HasValue.Should().BeFalse();
    }

    [Fact]
    public void Deserialize_ExplicitNull_HasValueIsTrueWithNullValue()
    {
        var command = JsonSerializer.Deserialize<TestCommand>("""{"name": null}""", Options)!;

        command.Name.HasValue.Should().BeTrue();
        command.Name.Value.Should().BeNull();
    }

    [Fact]
    public void Deserialize_PresentValue_HasValueIsTrueWithValue()
    {
        var command = JsonSerializer.Deserialize<TestCommand>("""{"name": "old"}""", Options)!;

        command.Name.HasValue.Should().BeTrue();
        command.Name.Value.Should().Be("old");
    }
}
