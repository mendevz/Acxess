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
            new(1,"TOA", "Toalla", 50m),
            new(2, "AGU","Agua", 20m)
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

    [Fact]
    public void Subscribe_WhenHasActiveSubscription_ShouldStartOnPreviousEndDate()
    {
        // Arrange
        var titular = Member.Create(1, "Clark", "Kent", 1);
        var startActiveSubscription = new DateTime(2026, 4, 25);
        var startNewSubscription = new DateTime(2026, 5, 23);

        titular.Subscribe(1, "Mensualidad", 500m, 1, 1, DurationSubscriptionUnit.Months, [], [], startActiveSubscription);
        var subscripcionActiva = titular.OwnedSubscriptions.First();

        // Act
        titular.Subscribe(1, "Mensualidad", 500m, 1, 1, DurationSubscriptionUnit.Months, [], [], startNewSubscription);
        var nuevaSubscripcion = titular.OwnedSubscriptions.Last();

        // Assert
        titular.OwnedSubscriptions.Should().HaveCount(2);
        nuevaSubscripcion.StartDate.Date.Should().Be(subscripcionActiva.EndDate.Date, "Debe iniciar el día que termina la anterior para no perder días pagados");
    }

    [Fact]
    public void Subscribe_WhenInGracePeriod_ShouldStartOnPreviousEndDate()
    {
        // Arrange
        var titular = Member.Create(1, "Clark", "Kent", 1);
        var startProrrogaSubscription = new DateTime(2026, 4, 22);
        var startNewSubscription = new DateTime(2026, 5, 23);

        titular.Subscribe(1, "Mensualidad", 500m, 1, 1, DurationSubscriptionUnit.Months, [], [], startProrrogaSubscription);
        var subscripcionProrroga = titular.OwnedSubscriptions.First();

        // Act
        titular.Subscribe(1, "Mensualidad", 500m, 1, 1, DurationSubscriptionUnit.Months, [], [], startNewSubscription);
        var nuevaSubscripcion = titular.OwnedSubscriptions.Last();

        // Assert
        titular.OwnedSubscriptions.Should().HaveCount(2);
        nuevaSubscripcion.StartDate.Date.Should().Be(subscripcionProrroga.EndDate.Date, "Dentro de la prórroga, la nueva suscripción respeta la fecha de corte original");
    }
    
    [Fact]
    public void Subscribe_WhenGracePeriodPassed_ShouldStartToday()
    {
        // Arrange
        var titular = Member.Create(1, "Clark", "Kent", 1);
        var startExpiredSubscription = new DateTime(2026, 4, 19);
        var startNewSubscription = new DateTime(2026, 5, 23);

        titular.Subscribe(1, "Mensualidad", 500m, 1, 1, DurationSubscriptionUnit.Months, [], [], startExpiredSubscription);

        // Act
        titular.Subscribe(1, "Mensualidad", 500m, 1, 1, DurationSubscriptionUnit.Months, [], [], startNewSubscription);
        var nuevaSubscripcion = titular.OwnedSubscriptions.Last();

        // Assert
        titular.OwnedSubscriptions.Should().HaveCount(2);
        nuevaSubscripcion.StartDate.Date.Should().Be(startNewSubscription.Date, "Pasó la prórroga y se desactivó, por lo que la nueva suscripción inicia hoy");
    }



}
