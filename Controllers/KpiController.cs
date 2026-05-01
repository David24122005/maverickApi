using maverickApi.Interfaces;
using Microsoft.AspNetCore.Authorization;
using maverickApi.Models;
using maverickApi.Models.Kpis;
using Microsoft.AspNetCore.Mvc;

namespace maverickApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class KpiController : ControllerBase
    {
        private readonly IKpiService _iKpiService;
        public KpiController(IKpiService iKpiService)
        {
            _iKpiService = iKpiService;
        }

        //http://localhost:5000/api/Kpi/obtener/dashboard
        [HttpGet("obtener/dashboard")]
        public async Task<ActionResult<RespuestaApi<DashboardKpi>>> ObtenerDashboardKpi()
        {
            var response = await _iKpiService.ObtenerKpiDashboardAsync();
            if (!response.Exito)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }



    }
}