using Microsoft.AspNetCore.Mvc;
using maverickApi.Interfaces;
using maverickApi.Models;
using Microsoft.AspNetCore.Authorization;
using maverickApi.Dtos;

namespace maverickApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "AdminOnly")]
    public class ProductoController : ControllerBase
    {
        private readonly IProductoService _iProductoService;
        public ProductoController(IProductoService productoService)
        {
            _iProductoService = productoService;
        }
        //http://localhost:5000/api/Producto/crear
        [HttpPost("crear")]
        public async Task<ActionResult<RespuestaApi<Producto>>> CrearProducto(Producto producto)
        {
            var response = await _iProductoService.CrearProductoAsync(producto);
            if (!response.Exito)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
        //http://localhost:5000/api/Producto/obtener
        [HttpGet("obtener")]
        public async Task<ActionResult<RespuestaApi<List<Producto>>>> ObtenerProductos()
        {
            var response = await _iProductoService.ObtenerProductosAsync();
            if (!response.Exito)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
        //http://localhost:5000/api/Producto/obtener/filtro/{busqueda}
        [HttpGet("obtener/filtro/{busqueda}")]
        public async Task<ActionResult<RespuestaApi<List<Producto>>>> ObtenerProductosPorFiltro(string busqueda)
        {
            var response = await _iProductoService.ObtenerProductosPorFiltrosAsync(busqueda);
            if (!response.Exito)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
        //http://localhost:5000/api/Producto/editar
        [HttpPatch("editar")]
        public async Task<ActionResult<RespuestaApi<Producto>>> EditarProducto(EditarProductoDto editarProductoDto)
        {
            var response = await _iProductoService.EditarProductoAsync(editarProductoDto);
            if (!response.Exito)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
        //http://localhost:5000/api/Producto/editar/estado
        [HttpPatch("editar/estado")]
        public async Task<ActionResult<RespuestaApi<Producto>>> EditarEstado(EditarEstadoDto editarEstadoDto)
        {
            var response = await _iProductoService.EditarEstadoAsync(editarEstadoDto);
            if (!response.Exito)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
    }
}