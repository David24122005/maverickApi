using maverickApi.Dtos;
using maverickApi.Interfaces;
using maverickApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace maverickApi.Controllers
{

    public class ProveedorController : ControllerBase
    {
        private readonly IProveedorService _iProveedorService;

        public ProveedorController(IProveedorService iProveedorService)
        {
            _iProveedorService = iProveedorService;
        }

        //http://localhost:5000/api/Proveedor/crear
        [HttpPost("crear")]
        public async Task<ActionResult<RespuestaApi<Proveedor>>> CrearProveedor(Proveedor proveedor)
        {
            var response = await _iProveedorService.CrearProveedorAsync(proveedor);
            if (!response.Exito)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
        //http://localhost:5000/api/Proveedor/obtener
        [HttpGet("obtener")]
        public async Task<ActionResult<RespuestaApi<List<Proveedor>>>> ObtenerProveedores()
        {
            var response = await _iProveedorService.ObtenerProveedoresAsync();
            if (!response.Exito)
            {
                return BadRequest(response);
            }
            if (response.Datos == null || response.Datos.Count == 0)
            {
                return NotFound(response);
            }
            return Ok(response);
        }
        //http://localhost:5000/api/Proveedor/obtener/filtro/{busqueda}
        [HttpGet("obtener/filtro/{busqueda}")]
        public async Task<ActionResult<RespuestaApi<Proveedor>>> ObtenerProveedoresPorFiltros(string busqueda)
        {
            var response = await _iProveedorService.ObtenerProveedoresPorFiltrosAsync(busqueda);
            if (!response.Exito)
            {
                return BadRequest(response);
            }
            if (response.Datos == null || response.Datos.Count == 0)
            {
                return NotFound(response);
            }
            return Ok(response);
        }
        //http://localhost:5000/api/Proveedor/editar
        [HttpPatch("editar")]
        public async Task<ActionResult<RespuestaApi<Proveedor>>> EditarProveedor(EditarProveedorDto proveedorDto)
        {
            var response = await _iProveedorService.EditarProveedorAsync(proveedorDto);
            if (!response.Exito)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
        //http://localhost:5000/api/Proveedor/editar/estado
        [HttpPatch("editar/estado")]
        public async Task<ActionResult<RespuestaApi<Proveedor>>> EditarEstado(EditarEstadoDto editarEstadoDto)
        {
            var response = await _iProveedorService.EditarEstadoAsync(editarEstadoDto);
            if (!response.Exito)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
    }

}