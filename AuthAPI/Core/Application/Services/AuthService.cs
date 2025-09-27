using AuthAPI.Core.Application.DTOs;
using AuthAPI.Core.Application.Interfaces;
using Dummy.SharedLib.Abstract;
using AuthAPI.Core.Domain.Entities;
using AuthAPI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace AuthAPI.Core.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _db;
        private readonly IRepository<ApplicationUser, string> _repository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;

        public AuthService(AppDbContext db, IJwtTokenGenerator jwtTokenGenerator,
            UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IRepository<ApplicationUser, string> repository)
        {
            _db = db;
            _jwtTokenGenerator = jwtTokenGenerator;
            _userManager = userManager;
            _roleManager = roleManager;
            _repository = repository;
        }

        public Task<string> GenerateServiceToken(string clientId, string clientSecret)
        {
            return Task.FromResult(_jwtTokenGenerator.GenerateServiceToken(clientId, clientSecret));
        }

        public async Task<LoginResponseDto> Login(LoginRequestDto loginRequestDto)
        {
            var user = _repository.FindAllAsync(u => u.UserName == loginRequestDto.Username, CancellationToken.None).Result.FirstOrDefault();
            if (user != null)
            {
                bool isValid = await _userManager.CheckPasswordAsync(user, loginRequestDto.Password);
                if (isValid)
                {
                    //if user was found , Generate JWT Token
                    var token = _jwtTokenGenerator.GenerateToken(user);

                    UserDto userDTO = new()
                    {
                        Email = user.Email ?? "",
                        Id = user.Id,
                        Name = user.Name
                    };

                    LoginResponseDto loginResponseDto = new LoginResponseDto()
                    {
                        User = userDTO,
                        Token = token
                    };

                    return loginResponseDto;
                }
            }

            return new LoginResponseDto();
        }

        public async Task<string> Register(RegistrationRequestDto registrationRequestDto)
        {
            ApplicationUser user = new()
            {
                UserName = registrationRequestDto.Email,
                Email = registrationRequestDto.Email,
                NormalizedEmail = registrationRequestDto.Email.ToUpper(),
                Name = registrationRequestDto.Name
            };

            try
            {
                var result = await _userManager.CreateAsync(user, registrationRequestDto.Password);
                if (result.Succeeded)
                {
                    var userToReturn = _db.ApplicationUsers.First(u => u.UserName == registrationRequestDto.Email);

                    UserDto userDto = new()
                    {
                        Email = userToReturn.Email ?? "",
                        Id = userToReturn.Id,
                        Name = userToReturn.Name,
                    };

                    return "";

                }
                else
                {
                    return result.Errors.FirstOrDefault()?.Description ?? "An unknown error occurred.";
                }

            }
            catch (Exception)
            {
                throw;
            }

        }
    }
}



