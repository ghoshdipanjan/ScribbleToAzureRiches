using Newtonsoft.Json;

namespace ScribbleOpeAIAnalysis.Model
{
    /// <summary>
    /// Represents a model for a demo deployment template with metadata and configuration details.
    /// </summary>
    public class DemoDeployTemplateModel
    {
        /// <summary>
        /// Gets or sets the title of the demo deployment template.
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the description of the demo deployment template.
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the preview image URL for the demo deployment.
        /// </summary>
        [JsonProperty("preview")]
        public string Preview { get; set; }

        /// <summary>
        /// Gets or sets the website URL associated with the demo deployment.
        /// </summary>
        [JsonProperty("website")]
        public string Website { get; set; }

        /// <summary>
        /// Gets or sets the author of the demo deployment template.
        /// </summary>
        [JsonProperty("author")]
        public string Author { get; set; }

        /// <summary>
        /// Gets or sets the source URL for the demo deployment template.
        /// </summary>
        [JsonProperty("source")]
        public string Source { get; set; }

        /// <summary>
        /// Gets or sets the tags associated with the demo deployment template.
        /// </summary>
        [JsonProperty("tags")]
        public List<string> Tags { get; set; }

        /// <summary>
        /// Gets or sets the demo guide URL for the deployment.
        /// </summary>
        [JsonProperty("demoguide")]
        public string DemoGuide { get; set; }

        /// <summary>
        /// Gets or sets the estimated cost of the deployment.
        /// </summary>
        [JsonProperty("cost")]
        public string Cost { get; set; }

        /// <summary>
        /// Gets or sets the estimated deployment time in minutes.
        /// </summary>
        [JsonProperty("deploytime")]
        public string DeployTime { get; set; }

        /// <summary>
        /// Gets or sets the prerequisites URL for the deployment.
        /// </summary>
        [JsonProperty("prereqs")]
        public string PreReqs { get; set; }
    }
}
