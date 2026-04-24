using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace maverickApi.Models
{
    public class OrdenCompra
    {
        public int Id { get; set; }
        public string? NumeroCompra { get; set; } = string.Empty;
        [Column(TypeName = "decimal(18,4)")]
        public decimal Total { get; set; }
        public int ProveedorId { get; set; }
        public Proveedor? Proveedor { get; set; }
        public int UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }
        public string? Estado { get; set; } = "Pendiente"; //Pendiente - Recibido - Cancelado
        public ICollection<DetalleOrdenCompra>? Detalles { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public DateTime? FechaEntrega { get; set; }
    }
}