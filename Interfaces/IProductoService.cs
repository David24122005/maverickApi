using maverickApi.Dtos;
using maverickApi.Models;

namespace maverickApi.Interfaces
{
    public interface IProductoService
    {
        Task<RespuestaApi<Producto>> CrearProductoAsync(Producto producto);
        Task<RespuestaApi<List<Producto>>> ObtenerProductosAsync();
        Task<RespuestaApi<List<Producto>>> ObtenerProductosPorFiltrosAsync(string busqueda);



        Task<RespuestaApi<Producto>> EditarProductoAsync(EditarProductoDto editarProductoDto);
        Task<RespuestaApi<Producto>> EditarEstadoAsync(EditarEstadoDto editarEstadoDto);
    }
}