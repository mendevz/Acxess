using System.Text.Json.Serialization;
using Acxess.Shared.ResultManager;
using Destructurama.Attributed;
using MediatR;

namespace Acxess.Identity.Application.Features.ApplicationUser.Commands.Login;

public record LoginCommand(string Username, [property: JsonIgnore, LogMasked] string Password) : IRequest<Result>;
