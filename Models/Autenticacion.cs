using System.ComponentModel.DataAnnotations;

namespace maverickApi.Models
{
    public class Autenticacion
    {
        public string? Email { get; set; }
        public string? PasswordHash { get; set; }
    }
    public class AutenticacionRespuesta
    {
        public string? Token { get; set; }
        public Usuario? Usuario { get; set; }
    }
}