using maverickApi.Dtos;
using maverickApi.Models;

namespace maverickApi.Interfaces
{
    public interface IProveedorService
    {
        Task<RespuestaApi<Proveedor>> CrearProveedorAsync(Proveedor proveedor);
        Task<RespuestaApi<List<Proveedor>>> ObtenerProveedoresAsync();
        Task<RespuestaApi<List<Proveedor>>> ObtenerProveedoresPorFiltrosAsync(string busqueda);
        Task<RespuestaApi<Proveedor>> EditarProveedorAsync(EditarProveedorDto proveedorDto);
        Task<RespuestaApi<Proveedor>> EditarEstadoAsync(EditarEstadoDto editarEstadoDto);
    }
}