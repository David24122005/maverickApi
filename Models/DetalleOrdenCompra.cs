using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.SignalR;

namespace maverickApi.Models
{
    public class DetalleOrdenCompra
    {
        public int Id { get; set; }
        public int OrdenCompraId { get; set; }
        public OrdenCompra? OrdenCompra { get; set; }
        public int ProductoId { get; set; }
        public Producto? Producto { get; set; }
        public int Cantidad { get; set; }
        [Column(TypeName = "decimal(18,4)")]
        public decimal PrecioUnitario { get; set; }
        [Column(TypeName = "decimal(18,4)")]
        public decimal Total { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}