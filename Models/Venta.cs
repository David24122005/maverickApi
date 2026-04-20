using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace maverickApi.Models
{
    public class Venta
    {
        public int Id { get; set; }
        public String? NumeroVenta { get; set; }
        public int UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }
        public int ClienteId { get; set; }
        public Cliente? Cliente { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal Iva { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; }
        public decimal Descuento { get; set; } = 0;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public ICollection<DetalleVenta>? Detalles { get; set; }

    }
}