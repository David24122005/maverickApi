using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;

using maverickApi.Data;
using maverickApi.Interfaces;
using maverickApi.Models;

namespace maverickApi.Services
{
    public class AutenticacionService : IAutenticacionService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AutenticacionService> _logger;
        public AutenticacionService(ApplicationDbContext DbContext, IConfiguration Configuration, ILogger<AutenticacionService> logger)
        {
            _dbContext = DbContext;
            _configuration = Configuration;
            _logger = logger;
        }
        public async Task<RespuestaApi<AutenticacionRespuesta>> IniciarSesionAsync(Autenticacion autenticacion)
        {
            if (string.IsNullOrWhiteSpace(autenticacion.Email))
            {
                _logger.LogWarning("El correo no debe estar vacio");
                return new RespuestaApi<AutenticacionRespuesta>
                {
                    Exito = false,
                    Mensaje = "El correo no puede estar vacío.",
                    Datos = null
                };
            }

            var usuario = await _dbContext.Usuarios.FirstOrDefaultAsync(u => u.Email == autenticacion.Email.ToString());
            if (usuario == null)
            {
                _logger.LogWarning("No se encontro algun usuario que corresponda al email: {Email}", autenticacion.Email);
                return new RespuestaApi<AutenticacionRespuesta>
                {
                    Exito = false,
                    Mensaje = "Usuario o contraseña incorrectos.",
                    Datos = null
                };
            }

            if (!usuario.Activo)
            {
                _logger.LogWarning("El usuario: {Nombre} se dio de baja anteriormente.", usuario.Nombre);
                return new RespuestaApi<AutenticacionRespuesta>
                {
                    Exito = false,
                    Mensaje = "El usuario con ese email a sido dado de baja",
                    Datos = null
                };
            }

            bool passwordValida = BCrypt.Net.BCrypt.Verify(autenticacion.PasswordHash, usuario.PasswordHash);

            if (!passwordValida)
            {
                _logger.LogWarning("La contraseña es incorrecta.");
                return new RespuestaApi<AutenticacionRespuesta>
                {
                    Exito = false,
                    Mensaje = "Usuario o contraseña incorrectos.",
                    Datos = null
                };
            }

            var token = GenerateJwtToken(usuario);
            usuario.BorrarHash();

            _logger.LogInformation("El usuario: {Nombre} se autentico correctamente.", usuario.Nombre);
            return new RespuestaApi<AutenticacionRespuesta>
            {
                Exito = true,
                Mensaje = "Se inicio sesion con exito",
                Datos = new AutenticacionRespuesta
                {
                    Token = token,
                    Usuario = usuario
                }
            };
        }
        private string GenerateJwtToken(Usuario usuario)
        {
            var secretKey = _configuration["Jwt:Key"];
            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];

            int expireMinutes = int.Parse(_configuration["Jwt:ExpireMinutes"] ?? "480");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                    new Claim(ClaimTypes.Name, usuario.Nombre ?? "Usuario"),
                    new Claim(ClaimTypes.Email, usuario.Email ?? ""),
                    new Claim("Admin", usuario.Admin.ToString())
                };
            var token = new JwtSecurityToken(
                    issuer: issuer,
                    audience: audience,
                    claims: claims,
                    expires: DateTime.Now.AddMinutes(expireMinutes),
                    signingCredentials: credentials
                );
            _logger.LogInformation("Un nuevo token se genero correctamente.");
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}