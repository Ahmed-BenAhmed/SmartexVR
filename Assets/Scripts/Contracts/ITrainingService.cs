using System.Threading.Tasks;

namespace Smartex.AR.Contracts
{
    /// <summary>
    /// Module F owns this.
    /// Production: hits /training/modules, /training/assessments, /training/progress.
    /// Dev/editor: MockTrainingService serves a canned loom module.
    /// </summary>
    public interface ITrainingService
    {
        Task<TrainingModule> GetModule(string deviceType, Locale locale);
        Task                 SubmitAssessment(Assessment a);
        Task<UserProgress>   GetProgress(string userId);
    }
}
