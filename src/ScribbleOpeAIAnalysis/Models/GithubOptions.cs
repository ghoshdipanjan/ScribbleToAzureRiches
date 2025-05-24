namespace ScribbleOpeAIAnalysis.Models
{
    /// <summary>
    /// Represents configuration options for interacting with a GitHub repository.
    /// </summary>
    public class GitHubOption
    {
        /// <summary>
        /// Gets or sets the owner of the GitHub repository.
        /// </summary>
        public string Owner { get; set; }

        /// <summary>
        /// Gets or sets the name of the GitHub repository.
        /// </summary>
        public string Repo { get; set; }

        /// <summary>
        /// Gets or sets the access token for authenticating with GitHub.
        /// </summary>
        public string AccessToken { get; set; }
    }
}
