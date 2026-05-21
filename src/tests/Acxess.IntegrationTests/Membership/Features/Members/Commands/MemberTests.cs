using Acxess.IntegrationTests.Setup;
using Acxess.Membership.Domain.Entities;
using Acxess.Membership.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Acxess.IntegrationTests.Membership.Features.Members.Commands;

[Collection("IntegrationTests")]
public class MemberTests(CustomWebApplicationFactory factory)
{
    [Fact]
    public async Task Should_Saver_Member_In_Database()
    {
        // Arrange 
        using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MembershipModuleContext>();

        var nuevoSocio =  Member.Create(
            tenantId: 1,
            firstName: "Mauro",
            lastName: "Mendez",
            createdByUser: 1,
            phone: "123456789",
            email: "");

        // Act 
        dbContext.Members.Add(nuevoSocio);
        await dbContext.SaveChangesAsync();

        // Assert
        var socioGuardado = await dbContext.Members.FindAsync(nuevoSocio.IdMember);

        socioGuardado.Should().NotBeNull();
        socioGuardado!.FirstName.Should().Be("Mauro");
    }
}
