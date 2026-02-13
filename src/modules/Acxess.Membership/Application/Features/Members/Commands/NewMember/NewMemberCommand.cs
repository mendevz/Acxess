using Acxess.Membership.Application.Features.Members.DTOs;
using Acxess.Shared.ResultManager;
using MediatR;

namespace Acxess.Membership.Application.Features.Members.Commands.NewMember;

public record NewMemberCommand(
    NewMemberDto MemberDto,
    int SellingPlanId,
    int IdTenant,
    List<int> AddOnIds,
    int PaymentMethodId,
    decimal AmountPaid,
    List<NewMemberDto> Beneficiaries,
    int CreatedUserId) : IRequest<Result<UpdatedSubMemberResponse>>;