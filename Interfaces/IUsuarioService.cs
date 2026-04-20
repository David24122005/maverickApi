using maverickApi.Dtos;
using maverickApi.Models;

namespace maverickApi.Interfaces
{
    public interface IUsuarioService
    {
        Task<RespuestaApi<Usuario>> CrearUsuarioAsync(Usuario usuario);
        Task<RespuestaApi<List<Usuario>>> ObtenerUsuariosAsync();
        Task<RespuestaApi<List<Usuario>>> ObtenerUsuariosPorFiltrosAsync(string busqueda);
       Task<RespuestaApi<Usuario>> EditarUsuarioAsync(EditarUsuarioDto editarUsuarioDto);
        Task<RespuestaApi<Usuario>> CambiarPasswordAsync(CambiarPasswordDto cambiarPasswordDto);
        Task<RespuestaApi<Usuario>> EditarEstadoAsync(EditarEstadoDto editarEstadoDto);
    }
}