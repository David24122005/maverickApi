using System.Security.Cryptography.X509Certificates;
using maverickApi.Interfaces;
using maverickApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace maverickApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class VentaController : ControllerBase
    {
        private readonly IVentaService _iVentaService;
        public VentaController(IVentaService iVentaService)
        {
            _iVentaService = iVentaService;
        }

        //http://localhost:5000/api/Venta/crear 
        [HttpPost("crear")]
        public async Task<ActionResult<RespuestaApi<Venta>>> CrearVentaAsync(Venta venta)
        {
            var response = await _iVentaService.CrearVentaAsync(venta);
            if (!response.Exito)
            {
                return BadRequest(response);
            }
            return Ok(response);

        }

        //http://localhost:5000/api/Venta/obtener
        [HttpGet("obtener")]
        public async Task<ActionResult<RespuestaApi<List<Venta>>>> ObtenerVentas()
        {
            var response = await _iVentaService.ObtenerVentasAsync();
            if (!response.Exito)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }



    }

}