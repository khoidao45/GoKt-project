using Gokt.Application.DTOs;
using Gokt.Application.Interfaces;
using Gokt.Domain.Exceptions;
using MediatR;

namespace Gokt.Application.Commands.Users.UpdateProfile;

public record UpdateProfileCommand(
    Guid UserId,
    string? FirstName,
    string? LastName,
    string? AvatarUrl,
    DateOnly? DateOfBirth,
    string? Gender,
    string? Address
) : IRequest<UserDto>;

public sealed class UpdateProfileCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateProfileCommand, UserDto>
{
    public async Task<UserDto> Handle(UpdateProfileCommand cmd, CancellationToken ct)
    {
        var user = await userRepository.GetByIdWithRolesAsync(cmd.UserId, ct)
            ?? throw new NotFoundException("User", cmd.UserId);

        user.Profile.Update(cmd.FirstName, cmd.LastName, cmd.AvatarUrl,
            cmd.DateOfBirth, cmd.Gender, cmd.Address);

        await unitOfWork.SaveChangesAsync(ct);
        return UserDto.From(user);
    }
}
