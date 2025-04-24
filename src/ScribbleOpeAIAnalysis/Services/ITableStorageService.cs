using ScribbleOpeAIAnalysis.Models;

namespace ScribbleOpeAIAnalysis.Services
{
    public interface ITableStorageService
    {
        Task<AnalysisResult> GetAnalysisResultAsync(string guid);
        Task UpsertAnalysisResultAsync(string guid, Dictionary<string, object> changes);
        Task<bool> DeleteAnalysisResultAsync(string guid);
    }
}
