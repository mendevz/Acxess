using Acxess.Membership.Application.Features.Members.DTOs;
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
    int CreatedUserId) : IRequest<Result<UpdatedSubMemberResponse>>;