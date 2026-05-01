using maverickApi.Models;
using maverickApi.Models.Kpis;

namespace maverickApi.Interfaces
{
    public interface IKpiService
    {
        Task<RespuestaApi<DashboardKpi>> ObtenerKpiDashboardAsync();
    }
}