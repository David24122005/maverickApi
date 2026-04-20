using maverickApi.Models;

namespace maverickApi.Interfaces
{
    public interface IClienteService
    {
        Task<RespuestaApi<Cliente>> CrearClienteAsync(Cliente cliente);
        Task<RespuestaApi<List<Cliente>>> ObtenerClientesAsync();
        Task<RespuestaApi<List<Cliente>>> ObtenerClientesPorFiltrosAsync(string busqueda);
    }
}