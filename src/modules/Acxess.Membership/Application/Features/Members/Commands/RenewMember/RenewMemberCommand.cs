using Acxess.Membership.Application.Features.Members.DTOs;
using Acxess.Shared.Abstractions;
using Acxess.Shared.ResultManager;
using MediatR;

namespace Acxess.Membership.Application.Features.Members.Commands.RenewMember;

public record RenewMemberCommand
(
    int IdMember,
    int SellingPlanId,
    int IdTenant,
    List<int> AddOnIds,
    int PaymentMethodId,
    decimal AmountPaid,
    List<NewMemberDto> Beneficiaries,
    int CreatedUserId,
    Guid IdempotencyToken,
    string? PhotoBase64 = null
) : IRequest<Result<UpdatedSubMemberResponse>>, IIdempotentCommand;