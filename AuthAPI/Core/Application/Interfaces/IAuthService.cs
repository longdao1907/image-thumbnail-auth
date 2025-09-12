using AuthAPI.Core.Application.DTOs;

namespace AuthAPI.Core.Application.Interfaces
{
    public interface IAuthService
    {
        Task<string> Register(RegistrationRequestDto registrationRequestDto);
        Task<LoginResponseDto> Login(LoginRequestDto loginRequestDto);
    }
}
