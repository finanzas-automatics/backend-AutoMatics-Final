using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using AutoMatics.Application.IAM.Interfaces;
using AutoMatics.Domain.IAM.Model.Aggregates;
using AutoMatics.Domain.IAM.Model.Commands;
using AutoMatics.Domain.IAM.Repositories;
using AutoMatics.Domain.Common;
using BCrypt.Net;

namespace AutoMatics.Application.IAM.Internal
{
    public class AuthCommandService : IAuthCommandService
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;

        public AuthCommandService(
            IUsuarioRepository usuarioRepository,
            IUnitOfWork unitOfWork,
            IConfiguration configuration)
        {
            _usuarioRepository = usuarioRepository;
            _unitOfWork = unitOfWork;
            _configuration = configuration;
        }

        public async Task<string> LoginAsync(string correo, string password)
        {
            var usuario = await _usuarioRepository.FindByCorreoAsync(correo);

            if (usuario == null || !BCrypt.Net.BCrypt.Verify(password, usuario.PasswordHash))
                throw new Exception("Credenciales inválidas.");

            return GenerarTokenJwt(usuario);
        }

        public async Task HandleAsync(RegistrarUsuarioCommand command)
        {
            if (await _usuarioRepository.FindByCorreoAsync(command.Correo) != null)
                throw new Exception("El correo ya está registrado.");

            var hash = HashPassword(command.Password);

            var usuario = new Usuario(
                command.Nombres,
                command.Apellidos,
                command.Correo,
                hash,
                command.Dni
            );

            await _usuarioRepository.AddAsync(usuario);
            await _unitOfWork.CompleteAsync();
        }

        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        private string GenerarTokenJwt(Usuario usuario)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!)
            );

            var creds = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256
            );

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, usuario.Correo),
                new Claim("nombres", usuario.Nombres),
                new Claim("dni", usuario.Dni)
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(4),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}