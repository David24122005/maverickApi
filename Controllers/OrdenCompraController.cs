using maverickApi.Interfaces;
using maverickApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace maverickApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrdenCompraController : ControllerBase
    {
        private readonly IOrdenCompraService _iOrdenCompraService;
        public OrdenCompraController(IOrdenCompraService iOrdenCompraService)
        {
            _iOrdenCompraService = iOrdenCompraService;
        }

        //http://localhost:5000/api/OrdenCompra/crear
        [HttpPost("crear")]
        public async Task<ActionResult<RespuestaApi<OrdenCompra>>> CrearOrdenCompra(OrdenCompra ordenCompra)
        {
            var response = await _iOrdenCompraService.CrearOrdenCompraAsync(ordenCompra);
            if (!response.Exito)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        //http://localhost:5000/api/OrdenCompra/obtener
        [HttpGet("obtener")]
        public async Task<ActionResult<RespuestaApi<List<OrdenCompra>>>> ObtenerOrdenesCompra()
        {

            var response = await _iOrdenCompraService.ObtenerOrdenesCompraAsync();
            if (!response.Exito)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
        // http://localhost:5000/api/OrdenCompra/editar
        [HttpPatch("editar")]
        public async Task<ActionResult<RespuestaApi<OrdenCompra>>> EditarOrdenCompra(OrdenCompra ordenCompraEditar)
        {
            var response = await _iOrdenCompraService.EditarOrdenCompraAsync(ordenCompraEditar);
            if (!response.Exito)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
        //http://localhost:5000/api/OrdenCompra/marcar/recibida/{id}
        [HttpPatch("marcar/recibida/{id}")]
        public async Task<ActionResult<RespuestaApi<OrdenCompra>>> MarcarOrdenRecibida(int id)
        {
            var response = await _iOrdenCompraService.MarcarOrdenRecibidaAsync(id);
            if (!response.Exito)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }





    }
}