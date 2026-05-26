using Acxess.Catalog.Domain.Errors;
using Acxess.IntegrationTests.Setup;
using Acxess.Membership.Application.Features.Members.Commands.NewMember;
using Acxess.Membership.Application.Features.Members.DTOs;
using Acxess.Membership.Domain.Entities;
using Acxess.Membership.Infrastructure.Persistence;
using Acxess.Shared.Abstractions;
using Acxess.Shared.Enums;
using Acxess.Shared.IntegrationEvents.Membership;
using Acxess.Shared.IntegrationServices.Catalog;
using Acxess.Shared.ResultManager;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using static Acxess.Catalog.Domain.Constants.AddOnDefaults;

namespace Acxess.IntegrationTests.Membership.Members;

[Collection("IntegrationTests")]
public class NewMemberHandlerTests(CustomWebApplicationFactory factory) : IAsyncLifetime
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_Should_Subscribe_NewMember(bool requireInscription)
    {
        // Arrange
        var catalogMock = new Mock<ICatalogIntegrationService>();
        var imageStorageMock = new Mock<IImageStorageService>();
        var mediatorMock = new Mock<IMediator>();

        catalogMock
            .Setup(x => x.GetPlanInfoAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlanIntegrationDto(1, "Mensualidad", 500m, 1, DurationSubscriptionUnit.Months, 1));

        var addOns = requireInscription 
            ? [new AddOnIntegrationDto(1, Inscription.Key, Inscription.Name, Inscription.Price)] 
            : new List<AddOnIntegrationDto>();

        catalogMock
            .Setup(x => x.GetAddOnPriceBatchAsync(It.IsAny<List<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(addOns); 

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
            MemberDto: new NewMemberDto(1,"Bruce", "Wayne", "12345", "base64String..."),
            SellingPlanId: 1,
            IdTenant: 1,
            AddOnIds: [],
            PaymentMethodId: 1,
            AmountPaid: 600m,
            Beneficiaries: [],
            CreatedUserId: 1,
            RequireInscription:requireInscription
        );


        // Act
        var result = await handler.Handle(command, CancellationToken.None);
        result.IsSuccess.Should().BeTrue(result.Error.Description);
        result.Value.Mensaje.Should().Be("Subscripción registrada correctamente.");

        using var assertScope = factory.Services.CreateScope();
        var assertDbContext = scope.ServiceProvider.GetRequiredService<MembershipModuleContext>();

        var miembroGuardado = await assertDbContext.Members.FindAsync(result.Value.IdMember);

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
    public async Task Handle_Should_Subscribe_NewMember_With_Beneficiries_One_Existing()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MembershipModuleContext>();

        var socioExistente = Member.Create(1, "Dick", "Grayson", 1, "000", null, null);
        dbContext.Members.Add(socioExistente);
        await dbContext.SaveChangesAsync();

        var catalogMock = new Mock<ICatalogIntegrationService>();
        catalogMock.Setup(c => c.GetPlanInfoAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PlanIntegrationDto>.Success(
                    new PlanIntegrationDto(1, "Plan Familiar 3", 700m, 1, DurationSubscriptionUnit.Months, 3)
                )
            );

        catalogMock.Setup(c => c.GetAddOnPriceBatchAsync(new List<int> { 1, 2 }, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new AddOnIntegrationDto(1, Inscription.Key,Inscription.Name, Inscription.Price),
                new AddOnIntegrationDto(2, "LOCK","Locker", 100m)
            ]);

        var imageStorageMock = new Mock<IImageStorageService>();
        imageStorageMock.Setup(i => i.SaveImageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Success("https://storage/fake-foto.jpg"));

        var mediatorSpy = new Mock<IMediator>();

        var handler = new NewMemberHandler(
            dbContext,
            Mock.Of<ILogger<NewMemberHandler>>(),
            catalogMock.Object,
            mediatorSpy.Object,
            imageStorageMock.Object);

        var titularDto = new NewMemberDto(1, "Bruce", "Wayne", "123", "base64-titular-img");

        var beneficiariosDtos = new List<NewMemberDto>
        {
            new(0, "Jason", "Todd", "456", "base64-ben-img"),
            new(socioExistente.IdMember, socioExistente.FirstName, socioExistente.LastName, "", "")
        };

        var command = new NewMemberCommand(
            IdTenant: 1,
            CreatedUserId: 1,
            SellingPlanId: 99,
            PaymentMethodId: 1,
            AmountPaid: 8150m, 
            MemberDto: titularDto,
            Beneficiaries: beneficiariosDtos,
            AddOnIds: [1, 2],
            RequireInscription: true
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);
        result.IsSuccess.Should().BeTrue(result.Error.Description);
        imageStorageMock.Verify(
            i => i.SaveImageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));

        mediatorSpy.Verify(
            m => m.Publish(
                It.Is<SubscriptionPurchasedIntegrationEvent>(e =>
                    e.AddOns.Count == 2 &&
                    e.AmountReceived == 8150m),
                It.IsAny<CancellationToken>()),
            Times.Once);

        using var assertScope = factory.Services.CreateScope();
        var assertDbContext = scope.ServiceProvider.GetRequiredService<MembershipModuleContext>();

        var totalMembers = await assertDbContext.Members.IgnoreQueryFilters().CountAsync();
        totalMembers.Should().Be(3, "inserted 2 beneficiaries and titular member");

        var titularEnBd = await assertDbContext.Members.IgnoreQueryFilters()
            .AsSplitQuery()
            .Include(m => m.OwnedSubscriptions)
                .ThenInclude(s => s.SubscriptionMembers)
            .Include(m => m.OwnedSubscriptions)
                .ThenInclude(s => s.AddOns)
            .FirstOrDefaultAsync(m => m.IdMember == result.Value.IdMember);

        titularEnBd.Should().NotBeNull();
        titularEnBd!.PhotoUrl.Should().Be("https://storage/fake-foto.jpg");

        var subscripcionGuardada = titularEnBd.OwnedSubscriptions.Single();
        subscripcionGuardada.AddOns.Should().HaveCount(2);

        var idsBeneficiariosGuardados = subscripcionGuardada.SubscriptionMembers.Select(sm => sm.IdMember).ToList();
        idsBeneficiariosGuardados.Should().HaveCount(3);
        idsBeneficiariosGuardados.Should().Contain(socioExistente.IdMember, "porque es el socio existente");
        idsBeneficiariosGuardados.Should().Contain(titularEnBd.IdMember, "is owner");
    }

    [Fact]
    public async Task Handle_Should_Subscribe_NewMember_With_NewBeneficiries()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MembershipModuleContext>();

        var addOnsIds = new List<int> { 1};

        var catalogMock = new Mock<ICatalogIntegrationService>();
        catalogMock.Setup(c => c.GetPlanInfoAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PlanIntegrationDto>.Success(
                    new PlanIntegrationDto(1, "Plan Familiar 2", 700m, 1, DurationSubscriptionUnit.Months, 2)
                )
            );

        catalogMock.Setup(c => c.GetAddOnPriceBatchAsync(addOnsIds, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new AddOnIntegrationDto(1, Inscription.Key,Inscription.Name, Inscription.Price)
            ]);

        var imageStorageMock = new Mock<IImageStorageService>();
        imageStorageMock.Setup(i => i.SaveImageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Success("https://storage/fake-foto.jpg"));

        var mediatorSpy = new Mock<IMediator>();

        var handler = new NewMemberHandler(
            dbContext,
            Mock.Of<ILogger<NewMemberHandler>>(),
            catalogMock.Object,
            mediatorSpy.Object,
            imageStorageMock.Object);

        var titularDto = new NewMemberDto(1, "Bruce", "Wayne", "123", "base64-titular-img");

        var beneficiariosDtos = new List<NewMemberDto>
        {
            new(0, "Jason", "Todd", "456", "base64-ben-img")
        };

        var command = new NewMemberCommand(
            IdTenant: 1,
            CreatedUserId: 1,
            SellingPlanId: 99,
            PaymentMethodId: 1,
            AmountPaid: 8150m,
            MemberDto: titularDto,
            Beneficiaries: beneficiariosDtos,
            AddOnIds: addOnsIds,
            RequireInscription: true
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Error.Description);
        imageStorageMock.Verify(
            i => i.SaveImageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));

        mediatorSpy.Verify(
            m => m.Publish(
                It.Is<SubscriptionPurchasedIntegrationEvent>(e =>
                    e.AddOns.Count == 1 &&
                    e.AmountReceived == 8150m),
                It.IsAny<CancellationToken>()),
            Times.Once);


        using var assertScope = factory.Services.CreateScope();
        var assertDbContext = scope.ServiceProvider.GetRequiredService<MembershipModuleContext>();

        var totalMembers = await assertDbContext.Members.IgnoreQueryFilters().CountAsync();
        totalMembers.Should().Be(2, "inserted 1 beneficiaries and titular member");

        var titularEnBd = await assertDbContext.Members.IgnoreQueryFilters()
            .AsSplitQuery()
            .Include(m => m.OwnedSubscriptions)
                .ThenInclude(s => s.SubscriptionMembers)
            .Include(m => m.OwnedSubscriptions)
                .ThenInclude(s => s.AddOns)
            .FirstOrDefaultAsync(m => m.IdMember == result.Value.IdMember);

        titularEnBd.Should().NotBeNull();
        titularEnBd!.PhotoUrl.Should().Be("https://storage/fake-foto.jpg");

        var subscripcionGuardada = titularEnBd.OwnedSubscriptions.Single();
        subscripcionGuardada.AddOns.Should().HaveCount(1);

        var idsBeneficiariosGuardados = subscripcionGuardada.SubscriptionMembers.Select(sm => sm.IdMember).ToList();
        idsBeneficiariosGuardados.Should().HaveCount(2);
        idsBeneficiariosGuardados.Should().Contain(titularEnBd.IdMember, "is owner");
    }

    [Fact]
    public async Task Handle_Should_Fail_SellingPlan_NotExists()
    {
        // Arrange
        var catalogMock = new Mock<ICatalogIntegrationService>();

        catalogMock
            .Setup(x => x.GetPlanInfoAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PlanIntegrationDto>.Failure(SellingPlansErrors.NotFound));

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
           AddOnIds: [],
           RequireInscription: true
       );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue(result.Error.Description);
        result.Error.Should().Be(SellingPlansErrors.NotFound);

        using var assertScope = factory.Services.CreateScope();
        var assertDbContext = scope.ServiceProvider.GetRequiredService<MembershipModuleContext>();

        var miembrosEnBD = assertDbContext.Members.ToList();
        miembrosEnBD.Should().BeEmpty();
    }

    public async Task InitializeAsync()
    {
        await factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;
}
