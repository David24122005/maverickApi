using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace maverickApi.Models
{
    public class Usuario
    {
        public int? Id { get; set; }
        public string? Nombre { get; set; }
        public string? Apellidos { get; set; }
        public string? Email { get; set; }
        public bool Admin { get; set; } = false;
        public string? PasswordHash { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}
