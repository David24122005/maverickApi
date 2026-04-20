using System.ComponentModel.DataAnnotations;

namespace maverickApi.Models
{
    public class OrdenCompra
    {
        public int Id { get; set; }
        public string? NumeroCompra { get; set; } = string.Empty;
        public int ProveedorId { get; set; }
        public int UsuarioId { get; set; }
        public DateTime Fecha { get; set; } = DateTime.UtcNow;
        public DateTime? FechaEntrega { get; set; }
        public string? Estado { get; set; } = "Pendiente"; //Pendiente - Recibido - Cancelado
    }
}