using System.Text.Json;

namespace ScribbleOpeAIAnalysis.Helper
{
    public static class AnalysisResultHelper
    {
        public static List<string> DeserializeComponentList(string componentJson)
        {
            return string.IsNullOrEmpty(componentJson) 
                ? new List<string>() 
                : JsonSerializer.Deserialize<List<string>>(componentJson);
        }

        public static string SerializeComponentList(List<string> components)
        {
            return JsonSerializer.Serialize(components);
        }
    }
}
