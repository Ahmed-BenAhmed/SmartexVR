using System.Threading;
using System.Threading.Tasks;

namespace Smartex.AR.Contracts
{
    /// <summary>
    /// Module D owns this.
    /// Production implementation hits the IEIA FastAPI backend
    /// (/maintenance/procedures, /maintenance/logs).
    /// Dev/editor uses MockMaintenanceService returning canned procedures.
    /// </summary>
    public interface IMaintenanceService
    {
        Task<Procedure> GetProcedure(string deviceId, CancellationToken ct = default);
        Task            LogCompletion(string deviceId, string procedureId, int[] completedSteps, string userId);
    }
}
