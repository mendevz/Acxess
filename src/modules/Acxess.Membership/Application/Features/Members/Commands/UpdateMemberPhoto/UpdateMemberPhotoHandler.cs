using Acxess.Membership.Infrastructure.Persistence;
using Acxess.Shared.Abstractions;
using Acxess.Shared.ResultManager;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Acxess.Membership.Application.Features.Members.Commands.UpdateMemberPhoto;

public class UpdateMemberPhotoHandler(
    MembershipModuleContext context,
    IImageStorageService imageStorage,
    ILogger<UpdateMemberPhotoHandler> logger) : IRequestHandler<UpdateMemberPhotoCommand, Result<string>>
{
    public async Task<Result<string>> Handle(UpdateMemberPhotoCommand request, CancellationToken cancellationToken)
    {
        var member = await context.Members.FirstOrDefaultAsync(m => m.IdMember == request.MemberId, cancellationToken);
        if (member == null)
        {
            logger.LogError("Member not found. MemberId: {MemberId}", request.MemberId);
            return Result<string>.Failure("Member.NotFound", "Socio no encontrado");
        }

        if (!string.IsNullOrEmpty(member.PhotoUrl))
        {
            await imageStorage.DeleteImageAsync(member.PhotoUrl, cancellationToken);
            logger.LogError("Deleted photoUrl already exists. MemberId: {MemberId}", request.MemberId); 
        }

        var cleanName = $"{member.FirstName}-{member.LastName}".ToLower().Replace(" ", "-");
        var newPhotoUrl = await imageStorage.SaveImageAsync(request.PhotoBase64, cleanName, cancellationToken);
        
        member.UpdatePhoto(newPhotoUrl.Value);
        await context.SaveChangesAsync(cancellationToken);
        
        logger.LogError("Photo Member updated successful. MemberId: {MemberId} PhotoUrl: {PhotoUrl}", request.MemberId, newPhotoUrl);

        return newPhotoUrl.Value;
    }
}