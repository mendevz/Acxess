using Acxess.IntegrationTests.Setup;
using Acxess.Membership.Application.Features.Members.Commands.NewMember;
using Acxess.Membership.Application.Features.Members.DTOs;
using Acxess.Membership.Infrastructure.Persistence;
using Acxess.Shared.Abstractions;
using Acxess.Shared.Enums;
using Acxess.Shared.IntegrationEvents.Membership;
using Acxess.Shared.IntegrationServices.Catalog;
using Acxess.Shared.ResultManager;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Acxess.IntegrationTests.Membership.Features.Members.Commands;

public class NewMemberHandlerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Handle_HappyPath()
    {
        // Arrange
        var catalogMock = new Mock<ICatalogIntegrationService>();
        var imageStorageMock = new Mock<IImageStorageService>();
        var mediatorMock = new Mock<IMediator>();

        catalogMock
            .Setup(x => x.GetPlanInfoAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlanIntegrationDto(1, "Mensualidad", 500m, 1, DurationSubscriptionUnit.Months, 1));

        catalogMock
            .Setup(x => x.GetAddOnPriceBatchAsync(It.IsAny<List<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]); 

        imageStorageMock
            .Setup(x => x.SaveImageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Success("https://mi-storage.com/foto.jpg"));

        var client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped(_ => catalogMock.Object);
                services.AddScoped(_ => imageStorageMock.Object);
            });
        });

        using var scope = client.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MembershipModuleContext>();

        var handler = new NewMemberHandler(
            dbContext,
            Mock.Of<ILogger<NewMemberHandler>>(), // Logger falso, no nos importa
            catalogMock.Object,
            mediatorMock.Object,
            imageStorageMock.Object
        );

        var command = new NewMemberCommand(
            IdTenant: 1,
            CreatedUserId: 1,
            SellingPlanId: 1,
            PaymentMethodId: 1,
            AmountPaid: 600m,
            MemberDto: new NewMemberDto(1,"Bruce", "Wayne", "12345", "base64String..."),
            Beneficiaries: [],
            AddOnIds: []
        );


        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Mensaje.Should().Be("Subscripción registrada correctamente.");

        var miembroGuardado = await dbContext.Members.FindAsync(result.Value.IdMember);

        miembroGuardado.Should().NotBeNull();
        miembroGuardado!.FirstName.Should().Be("Bruce");
        miembroGuardado.PhotoUrl.Should().Be("https://mi-storage.com/foto.jpg", "porque el Mock de imágenes debía retornar esto");

        mediatorMock.Verify(
            m => m.Publish(
                It.Is<SubscriptionPurchasedIntegrationEvent>(e =>
                    e.IdMember == miembroGuardado.IdMember &&
                    e.AmountReceived == 600m),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "El evento de facturación debió haberse disparado exactamente una vez."
        );

    }

    [Fact]
    public async Task Handle_WhenSellingPlan_NotExists_ShouldFail()
    {
        // Arrange
        var catalogMock = new Mock<ICatalogIntegrationService>();

        catalogMock
            .Setup(x => x.GetPlanInfoAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PlanIntegrationDto?)null);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MembershipModuleContext>();

        var handler = new NewMemberHandler(
            dbContext, Mock.Of<ILogger<NewMemberHandler>>(), catalogMock.Object, Mock.Of<IMediator>(), Mock.Of<IImageStorageService>()
        );

        var command = new NewMemberCommand(
           IdTenant: 1,
           CreatedUserId: 1,
           SellingPlanId: 1,
           PaymentMethodId: 1,
           AmountPaid: 600m,
           MemberDto: new NewMemberDto(1, "Bruce", "Wayne", "12345", "base64String..."),
           Beneficiaries: [],
           AddOnIds: []
       );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Plan.NotFound");

        var miembrosEnBD = dbContext.Members.ToList();
        miembrosEnBD.Should().BeEmpty();
    }
}
