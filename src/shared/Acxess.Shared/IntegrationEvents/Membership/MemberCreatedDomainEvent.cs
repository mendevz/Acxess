using MediatR;

namespace Acxess.Shared.IntegrationEvents.Membership;

public record MemberCreatedDomainEvent(int IdMember) : INotification;