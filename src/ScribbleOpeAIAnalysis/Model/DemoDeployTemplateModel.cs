using Newtonsoft.Json;

namespace ScribbleOpeAIAnalysis.Model
{
    public class DemoDeployTemplateModel
    {
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("preview")]
        public string Preview { get; set; }

        [JsonProperty("website")]
        public string Website { get; set; }

        [JsonProperty("author")]
        public string Author { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("tags")]
        public List<string> Tags { get; set; }

        [JsonProperty("demoguide")]
        public string DemoGuide { get; set; }

        [JsonProperty("cost")]
        public string Cost { get; set; }

        [JsonProperty("deploytime")]
        public string DeployTime { get; set; }

        [JsonProperty("prereqs")]
        public string PreReqs { get; set; }
    }


}
