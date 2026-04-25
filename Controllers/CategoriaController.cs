using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using maverickApi.Interfaces;
using maverickApi.Models;

namespace maverickApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CategoriaController : ControllerBase
    {
        private readonly ICategoriaService _iCategoraService;
        public CategoriaController(ICategoriaService categoriaService)
        {
            _iCategoraService = categoriaService;
        }

        //http://localhost:5000/api/Categoria/crear
        [HttpPost("crear")]
        public async Task<ActionResult<RespuestaApi<Categoria>>> CrearCategoria(Categoria categoria)
        {
            var response = await _iCategoraService.CrearCategoriaAsync(categoria);
            if (!response.Exito)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
        //http://localhost:5000/api/Categoria/obtener 
        [HttpGet("obtener")]
        public async Task<ActionResult<RespuestaApi<List<Categoria>>>> ObtenerCategorias()
        {
            var response = await _iCategoraService.ObtenerCategoriasAsync();
            if (!response.Exito)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
        //http//localhost:5000/api/Categoria/obtener/filtro/{busqueda}
        [HttpGet("obtener/filtro/{busqueda}")]
        public async Task<ActionResult<RespuestaApi<List<Categoria>>>> ObtenerCategoriasPorFiltros(string busqueda)
        {
            var response = await _iCategoraService.ObtenerCategoriasPorFiltroAsync(busqueda);
            if (!response.Exito)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
    }
}