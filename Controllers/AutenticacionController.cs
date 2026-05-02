using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using maverickApi.Data;
using maverickApi.Models;
using maverickApi.Interfaces;
namespace maverickApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AutenticacionController : ControllerBase
    {
        private readonly IAutenticacionService _iAutenticacionService;

        public AutenticacionController(IAutenticacionService iAutenticacionService)
        {
            _iAutenticacionService = iAutenticacionService;
        }

        //http://localhost:5000/api/Autenticacion/login
        [HttpPost("login")]
        public async Task<ActionResult<RespuestaApi<AutenticacionRespuesta>>> IniciarSesion(Autenticacion autenticacion)
        {

            var response = await _iAutenticacionService.IniciarSesionAsync(autenticacion);

            if (!response.Exito)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
    }

}