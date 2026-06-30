using Acxess.Shared.ResultManager;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Acxess.Identity.Application.Features.ApplicationUser.Commands.Login;

public class LoginHandler(
    SignInManager<Domain.Entities.ApplicationUser> signInManager) : IRequestHandler<LoginCommand, Result>
{
    public async Task<Result> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var result = await signInManager.PasswordSignInAsync(
            request.Username, 
            request.Password, 
            isPersistent: true, 
            lockoutOnFailure: false);
        
        return result.Succeeded 
            ? Result.Success() 
            : Result.Failure("Login", "Invalid credentials");
    }
}