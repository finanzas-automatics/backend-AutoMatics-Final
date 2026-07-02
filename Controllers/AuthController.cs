using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AutoMatics.Application.IAM.Interfaces;
using AutoMatics.Domain.IAM.Model.Commands;
using AutoMatics.Shared.Responses;

namespace AutoMatics.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthCommandService _authService;
        public AuthController(IAuthCommandService authService) => _authService = authService;

        [HttpPost("register")]
        public async Task<IActionResult> Registrar([FromBody] RegistrarUsuarioCommand command)
        {
            await _authService.HandleAsync(command);
            return Ok(ApiResponse<bool>.Success(true, "Usuario registrado correctamente"));
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try {
                var token = await _authService.LoginAsync(request.Email, request.Password);
                return Ok(ApiResponse<string>.Success(token, "Login exitoso"));
            } catch (System.Exception ex) {
                return Unauthorized(ApiResponse<string>.Fail(ex.Message));
            }
        }
    }

    public record LoginRequest(string Email, string Password);
}