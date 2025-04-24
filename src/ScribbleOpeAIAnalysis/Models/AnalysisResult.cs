using System;
using Azure;
using Azure.Data.Tables;
using ScribbleOpeAIAnalysis.Helper;

namespace ScribbleOpeAIAnalysis.Models
{
    public class AnalysisResult : ITableEntity
    {
        public string PartitionKey { get; set; } = "Analysis";
        public string RowKey { get; set; }  // GUID
        public string Component { get; set; }  // JSON serialized List<string>
        public string ArchitectureDetail { get; set; }  // Markdown content
        public string BicepTemplate { get; set; }
        public string ArmTemplate { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public List<string> GetComponentList() => AnalysisResultHelper.DeserializeComponentList(Component);
        public void SetComponentList(List<string> components) => Component = AnalysisResultHelper.SerializeComponentList(components);
    }
}
