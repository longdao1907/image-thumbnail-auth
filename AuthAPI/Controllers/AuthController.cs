using AuthAPI.Core.Application.DTOs;
using AuthAPI.Core.Application.Interfaces;
using Azure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace AuthAPI.Controllers
{
    [ApiController]
    [Route("api/Auth")]
    public class AuthController: ControllerBase
    {
        private readonly IAuthService _authService;

        private readonly IConfiguration _configuration;
        protected ResponseDto _response;

        public AuthController(IAuthService authService,  IConfiguration configuration)
        {
            _authService = authService;
            _configuration = configuration;
          
            _response = new();
        }


        [HttpPost("register")]
        public async Task<IActionResult> Register(RegistrationRequestDto request)
        {
            var errorMessage = await _authService.Register(request);
            if (!string.IsNullOrEmpty(errorMessage))
            {
                _response.IsSuccess = false;
                _response.Message = errorMessage;
                return BadRequest(_response);
            }
            return Ok(_response);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequestDto request)
        {
            var loginResponse = await _authService.Login(request);
            if (loginResponse.User == null)
            {
                _response.IsSuccess = false;
                _response.Message = "Username or password is incorrect";
                return BadRequest(_response);
            }
            _response.Result = loginResponse;
            return Ok(_response);
        }
    }
}
