using maverickApi.Interfaces;
using maverickApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace maverickApi.Controllers
{
    [ApiController]
    [Route("api/{controller}")]
    [Authorize(Policy = "AdminOnly")]
    public class ClienteController : ControllerBase
    {
        private readonly IClienteService _iClienteService;
        public ClienteController(IClienteService clienteService)
        {
            _iClienteService = clienteService;
        }

        //http://localhost:5000/api/Cliente/crear
        [HttpPost("crear")]
        public async Task<ActionResult<RespuestaApi<Cliente>>> CrearCliente(Cliente cliente)
        {
            var response = await _iClienteService.CrearClienteAsync(cliente);
            if (!response.Exito)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
        //http://localhost:5000/api/Cliente/obtener
        [HttpGet("obtener")]
        public async Task<ActionResult<RespuestaApi<List<Cliente>>>> ObtenerClientes()
        {
            var response = await _iClienteService.ObtenerClientesAsync();
            if (!response.Exito)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
        //http://localhost:5000/api/Cliente/obtener/filtro/{busqueda}
        [HttpGet("obtener/filtro/{busqueda}")]
        public async Task<ActionResult<RespuestaApi<List<Cliente>>>> ObtenerClientesPorFiltros(string busqueda)
        {
            var response = await _iClienteService.ObtenerClientesPorFiltrosAsync(busqueda);
            if (!response.Exito)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
    }
}