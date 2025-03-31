namespace ScribbleOpeAIAnalysis.Model
{
    /// <summary>
    /// Represents a template model containing details about Bicep and ARM templates.
    /// </summary>
    public class TemplateModel
    {
        /// <summary>
        /// Gets or sets the name of the template.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description of the template.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the Bicep template content.
        /// </summary>
        public string BicepTemplate { get; set; }

        /// <summary>
        /// Gets or sets the ARM template content.
        /// </summary>
        public string ArmTemplate { get; set; }
    }
}
