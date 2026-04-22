using maverickApi.Models;
namespace maverickApi.Dtos
{
    public class EditarProductoDto
    {
        public int Id { get; set; }
        public string? Nombre { get; set; }
        public string? Descripcion { get; set; }
        public string? Sku { get; set; }
        public string? CodigoBarras { get; set; }
        public string? Marca { get; set; }
        public string? Modelo { get; set; }
        public int? CategoriaId { get; set; }
        public int? ProveedorId { get; set; }
        public int Stock { get; set; }
        public decimal PrecioCompra { get; set; }
        public decimal PrecioVenta { get; set; }
    }
}