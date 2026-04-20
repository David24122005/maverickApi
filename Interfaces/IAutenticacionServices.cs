using maverickApi.Models;

namespace maverickApi.Interfaces
{
    public interface IAutenticacionService
    {
        Task<RespuestaApi<AutenticacionRespuesta>> IniciarSesionAsync(Autenticacion autenticacion);
    }
}