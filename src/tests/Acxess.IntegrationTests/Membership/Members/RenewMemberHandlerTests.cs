using Acxess.IntegrationTests.Setup;
using Acxess.Membership.Application.Features.Members.Commands;
using Acxess.Membership.Domain.Entities;
using Acxess.Membership.Infrastructure.Persistence;
using Acxess.Shared.Abstractions;
using Acxess.Shared.Enums;
using Acxess.Shared.IntegrationEvents.Membership;
using Acxess.Shared.IntegrationServices;
using Acxess.Shared.ResultManager;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Acxess.IntegrationTests.Membership.Members;

[Collection("IntegrationTests")]
public class RenewMemberHandlerTests(CustomWebApplicationFactory factory) : IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        await factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Handle_Should_Renew_Subscription_Member()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MembershipModuleContext>();

        var mainMember = Member.Create(1,
            "Mauro",
            "Mendez",
            1,
            DateTime.UtcNow);

        var startActiveSubscription = new DateTime(2026, 4, 25);
        mainMember.Subscribe(1, "Mensualidad", 500m, 1, 1, DurationSubscriptionUnit.Months, [], [], startActiveSubscription);
        dbContext.Members.Add(mainMember);  
        await dbContext.SaveChangesAsync();

        var catalogMock = new Mock<ICatalogIntegrationService>();
        catalogMock.Setup(c => c.GetPlanInfoAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PlanIntegrationDto>.Success(
                  new PlanIntegrationDto(1, "Mensualidad", 400m, 1, DurationSubscriptionUnit.Months, 1)
            ));

        var timeServiceMock = new Mock<ITimeService>();
        var imageStorageMock = new Mock<IImageStorageService>();
        var mediatorSpy = new Mock<IMediator>();

        var renewMemberCommand = new RenewMemberCommand(
            IdMember: mainMember.IdMember,
            SellingPlanId: 1,
            AddOnIds: [],
            PaymentMethodId:   1,
            AmountPaid: 400m,
            Beneficiaries: [],
            CreatedUserId: 1,
            IdempotencyToken: Guid.NewGuid());

        var handler = new RenewMemberHandler(
            dbContext, 
            catalogMock.Object,
            mediatorSpy.Object, 
            imageStorageMock.Object,
            timeServiceMock.Object,
            Mock.Of<ILogger<RenewMemberHandler>>());

        // Act
        var result = await handler.Handle(renewMemberCommand, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue(result.Error.Description);
        mediatorSpy.Verify(
            m => m.Publish(
                It.Is<SubscriptionPurchasedIntegrationEvent>(e =>
                    e.AddOns.Count == 0 &&
                    e.AmountReceived == 400m),
                It.IsAny<CancellationToken>()),
            Times.Once);

        using var assertScope = factory.Services.CreateScope();
        var assertDbContext = scope.ServiceProvider.GetRequiredService<MembershipModuleContext>();


        var titularEnBd = await assertDbContext.Members.IgnoreQueryFilters()
            .AsSplitQuery()
            .Include(m => m.OwnedSubscriptions)
                .ThenInclude(s => s.SubscriptionMembers)
            .Include(m => m.OwnedSubscriptions)
                .ThenInclude(s => s.AddOns)
            .FirstOrDefaultAsync(m => m.IdMember == result.Value.IdMember);

        titularEnBd.Should().NotBeNull();

        var ownerSubscriptions = titularEnBd.OwnedSubscriptions.ToList();
        ownerSubscriptions.Should().HaveCount(2, "had active subscription");

        var subscriptions = titularEnBd.SubscriptionMemberships.ToList();
        subscriptions.Should().HaveCount(2, "had active subscription");
    }
}
