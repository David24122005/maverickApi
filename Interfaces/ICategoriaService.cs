using maverickApi.Models;

namespace maverickApi.Interfaces
{
    public interface ICategoriaService
    {
        Task<RespuestaApi<Categoria>> CrearCategoriaAsync(Categoria categoria);
        Task<RespuestaApi<List<Categoria>>> ObtenerCategoriasAsync();
        Task<RespuestaApi<List<Categoria>>> ObtenerCategoriasPorFiltroAsync(string busqueda);
    }
}