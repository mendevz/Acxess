using Acxess.Membership.Domain.Entities;
using Acxess.Shared.Enums;
using Acxess.Shared.IntegrationServices.Catalog;
using FluentAssertions;

namespace Acxess.UnitTests.Membership.Entities;

public class MemberTests
{
    [Theory]
    [InlineData("2026-01-01", 15, DurationSubscriptionUnit.Days, "2026-01-16")]
    [InlineData("2026-02-15", 1, DurationSubscriptionUnit.Months, "2026-03-15")]
    [InlineData("2026-05-20", 2, DurationSubscriptionUnit.Years, "2028-05-20")]
    //Escenario Bisiesto
    [InlineData("2024-02-29", 1, DurationSubscriptionUnit.Years, "2025-02-28")]
    public void Subscribe_ShouldLinkBeneficiaries_AddOns_Y_CalculateExpiration(
        string fechaInicioStr,
        int duration,
        DurationSubscriptionUnit durationUnit,
        string fechaEsperadaStr)
    {

        // Arrange
        var titular = Member.Create(
             1,
            "Bruce",
             "Wayne",
             1,
            phone: "123");

        var fechaInicio = DateTime.Parse(fechaInicioStr);
        var expectedEndDate = DateTime.Parse(fechaEsperadaStr);

        var addOns = new List<AddOnIntegrationDto>
        {
            new(1, "Toalla", 50m),
            new(2, "Agua", 20m)
        };
        var beneficiaryIds = new List<int> { 5, 8 };

        // Act
        titular.Subscribe(
            10,
            "Mensualidad",
            500m,
            duration,
            0,
            durationUnit,
            beneficiaryIds,
            addOns,
            fechaInicio
        );

        // Assert
        titular.OwnedSubscriptions.Should().HaveCount(1);
        var subscripcion = titular.OwnedSubscriptions.First();
        subscripcion.StartDate.Date.Should().Be(fechaInicio.Date);
        subscripcion.EndDate.Date.Should().Be(
            expectedEndDate.Date,
            $"iniciar el {fechaInicioStr} por {duration} {durationUnit} debería terminar el {fechaEsperadaStr}");

        subscripcion.AddOns.Should().HaveCount(2);
        subscripcion.AddOns.Select(sm => sm.IdAddOn).Should().BeEquivalentTo(addOns.Select(a => a.Id));

        // Verifica que se hayan creado los enlaces de Beneficiarios
        subscripcion.SubscriptionMembers.Should().HaveCount(3);
        subscripcion.SubscriptionMembers.Should().ContainSingle(sm => sm.Owner);
        var beneficiariosAgregados = subscripcion.SubscriptionMembers.Where(sm => !sm.Owner).ToList();
        beneficiariosAgregados.Should().HaveCount(2);
        beneficiariosAgregados.Select(sm => sm.IdMember).Should().BeEquivalentTo(beneficiaryIds);
    }
}
