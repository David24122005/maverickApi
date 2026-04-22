using System.ComponentModel.DataAnnotations;

namespace maverickApi.Models
{
    public class Proveedor
    {
        public int Id { get; set; }
        public string? Nombre { get; set; } = string.Empty;
        public string? Email { get; set; } = string.Empty;
        public string? Telefono { get; set; } = string.Empty;
        public string? Direccion { get; set; } = string.Empty;
        public string? Rfc { get; set; }
        public bool Activo { get; set; } = true;
        public ICollection<Producto>? Productos { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }
}