using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace maverickApi.Models
{
    public class Producto
    {
        public int Id { get; set; }
        public string? Sku { get; set; }
        public string? CodigoBarras { get; set; }
        public int Stock { get; set; } = 0;
        public string? Nombre { get; set; }
        public string? Descripcion { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal PrecioCompra { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal PrecioVenta { get; set; }
        public string? Marca { get; set; }
        public string? Modelo { get; set; }
        public int? CategoriaId { get; set; }
        public Categoria? Categoria { get; set; }
        public int? ProveedorId { get; set; }
        public Proveedor? Proveedor { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }
}