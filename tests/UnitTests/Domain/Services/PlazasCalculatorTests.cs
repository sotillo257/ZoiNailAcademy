using FluentAssertions;
using ZoiNailAcademy.Domain.Enums;
using ZoiNailAcademy.Domain.Services;

namespace UnitTests.Domain.Services;

public class PlazasCalculatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 3, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Ocupadas_SinReservas_IsZero()
    {
        PlazasCalculator.Ocupadas([], Now).Should().Be(0);
    }

    [Fact]
    public void Ocupadas_ConPagadas_CountsThem()
    {
        PlazasCalculator.Ocupadas([(EstadoReserva.Pagada, null), (EstadoReserva.Pagada, Now.AddHours(-5))], Now)
            .Should().Be(2);
    }

    [Fact]
    public void Ocupadas_ConPendientesVigentes_CountsThem()
    {
        PlazasCalculator.Ocupadas([(EstadoReserva.Pendiente, Now.AddMinutes(1))], Now).Should().Be(1);
    }

    [Fact]
    public void Ocupadas_ConPendientesCaducadas_DoesNotCountThem()
    {
        PlazasCalculator.Ocupadas(
            [(EstadoReserva.Pendiente, Now), (EstadoReserva.Pendiente, Now.AddMinutes(-1)), (EstadoReserva.Pendiente, null)],
            Now).Should().Be(0);
    }

    [Fact]
    public void Ocupadas_ConExpiradasYCanceladas_DoesNotCountThem()
    {
        PlazasCalculator.Ocupadas(
            [(EstadoReserva.Expirada, Now.AddDays(1)), (EstadoReserva.Cancelada, Now.AddDays(1)), (EstadoReserva.Cancelada, null)],
            Now).Should().Be(0);
    }

    [Theory]
    [InlineData(10, 0, 10)]
    [InlineData(10, 4, 6)]
    [InlineData(10, 10, 0)]
    [InlineData(10, 12, 0)]
    public void Libres_IsTotalMinusOccupiedAndNeverNegative(int plazas, int ocupadas, int expected)
    {
        PlazasCalculator.Libres(plazas, ocupadas).Should().Be(expected);
    }

    [Theory]
    [InlineData(0, EstadoEdicionPublico.Completo)]
    [InlineData(1, EstadoEdicionPublico.UltimasPlazas)]
    [InlineData(3, EstadoEdicionPublico.UltimasPlazas)]
    [InlineData(4, EstadoEdicionPublico.Disponible)]
    public void EstadoPublico_DependsOnFreeSeatsAndThreshold(int libres, EstadoEdicionPublico expected)
    {
        PlazasCalculator.EstadoPublico(libres, umbralUltimasPlazas: 3).Should().Be(expected);
    }
}
