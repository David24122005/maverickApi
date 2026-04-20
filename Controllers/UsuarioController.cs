using Microsoft.AspNetCore.Mvc;
using maverickApi.Interfaces;
using maverickApi.Models;
using Microsoft.AspNetCore.Authorization;
using maverickApi.Dtos;

namespace maverickApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuarioController : ControllerBase
    {
        private readonly IUsuarioService _iUsuarioService;

        //Aqui se inicializa la interfaz del servicio de Usuario
        public UsuarioController(IUsuarioService iUsuarioService)
        {
            _iUsuarioService = iUsuarioService;
        }

        //http://localhost:5000/api/Usuario/crear
        [HttpPost("crear")]
        public async Task<ActionResult<RespuestaApi<Usuario>>?> CrearUsuario(Usuario usuario)
        {

            var response = await _iUsuarioService.CrearUsuarioAsync(usuario);

            if (!response.Exito)
            {
                return BadRequest(response);
            }

            return StatusCode(201, response);
        }
        //http://localhost:5000/api/Usuario/obtener
        [HttpGet("obtener")]
        public async Task<ActionResult<RespuestaApi<List<Usuario>>>?> ObtenerUsuarios()
        {

            var response = await _iUsuarioService.ObtenerUsuariosAsync();

            if (!response.Exito)
            {
                return BadRequest(response);
            }
            if (response.Datos == null || response.Datos.Count == 0)
            {
                return NotFound(response);
            }

            return StatusCode(200, response);
        }
        //http://localhost:5000/api/Usuario/obtener/filtro/{busqueda}
        [HttpGet("obtener/filtro/{busqueda}")]
        public async Task<ActionResult<RespuestaApi<List<Usuario>>>> ObtenerUsuariosPorFiltros(string busqueda)
        {
            var response = await _iUsuarioService.ObtenerUsuariosPorFiltrosAsync(busqueda);
            if (!response.Exito)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
        //http://localhost:5000/api/Usuario/editar
        [HttpPatch("editar")]
        public async Task<ActionResult<RespuestaApi<Usuario>>?> EditarUsuario(EditarUsuarioDto editarUsuarioDto)
        {
            var response = await _iUsuarioService.EditarUsuarioAsync(editarUsuarioDto);
            if (!response.Exito)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
        //http://localhost:5000/api/Usuario/editar/estado
        [HttpPatch("editar/estado")]
        public async Task<ActionResult<RespuestaApi<Usuario>>> EditarEstado(EditarEstadoDto editarEstadoDto)
        {
            var response = await _iUsuarioService.EditarEstadoAsync(editarEstadoDto);
            if (!response.Exito)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
        //http://localhost:5000/api/Usuario/editar/hash
        [HttpPatch("editar/hash")]
        public async Task<ActionResult<RespuestaApi<Usuario>>?> CambiarPassword(CambiarPasswordDto cambiarPasswordDto)
        {
            var response = await _iUsuarioService.CambiarPasswordAsync(cambiarPasswordDto);
            if (!response.Exito)
            {
                return StatusCode(400, response);
            }

            return Ok(response);
        }
    }
}