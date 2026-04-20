using System.ComponentModel.DataAnnotations;

namespace maverickApi.Models
{
    public class Cliente
    {
        public int Id { get; set; }
        public string? Nombre { get; set; }
        public string? Rfc { get; set; }
        public string? Telefono { get; set; }
        public string? Email { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }
}