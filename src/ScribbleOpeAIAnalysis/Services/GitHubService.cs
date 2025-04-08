using Octokit;
using Microsoft.Extensions.Options;
using ScribbleOpeAIAnalysis.Model;

namespace ScribbleOpeAIAnalysis.Services
{
    /// <summary>
    /// Service for interacting with GitHub repositories.
    /// </summary>
    public class GitHubService
    {
        /// <summary>
        /// GitHub client for interacting with the GitHub API.
        /// </summary>
        private readonly GitHubClient _gitHubClient;

        /// <summary>
        /// Configuration options for GitHub integration.
        /// </summary>
        private readonly GitHubOption _gitHubOptions;

        /// <summary>
        /// HTTP client for making HTTP requests.
        /// </summary>
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="GitHubService"/> class.
        /// </summary>
        /// <param name="options">The GitHub configuration options.</param>
        /// <param name="httpClient">The HTTP client for making requests.</param>
        public GitHubService(IOptions<GitHubOption> options, HttpClient httpClient)
        {
            _gitHubOptions = options.Value;
            _gitHubClient = new GitHubClient(new ProductHeaderValue("ScribbleOpeAIAnalysis"))
            {
                Credentials = new Credentials(_gitHubOptions.AccessToken)
            };
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        /// <summary>
        /// Creates a folder with a random name and adds a Bicep file inside it.
        /// </summary>
        /// <param name="bicepContentUrl">The URL to fetch the Bicep file content.</param>
        /// <param name="architectureName">The name of the architecture to include in the folder name.</param>
        /// <returns>The name of the created folder.</returns>
        public async Task<string> CreateFolderAndAddBicepFileAsync(string bicepContentUrl, string architectureName)
        {
            // Fetch the content of the Bicep file from the provided URL.
            var response = await _httpClient.GetAsync(bicepContentUrl);
            response.EnsureSuccessStatusCode();
            var bicepContent = await response.Content.ReadAsStringAsync();

            // Generate a random folder name, optionally including the architecture name.
            string folderName = (string.IsNullOrEmpty(architectureName) ? "" + Guid.NewGuid().ToString() : architectureName + "-" + Guid.NewGuid().ToString());

            // Define the file path within the new folder.
            string filePath = $"BicepDemoRegistry/{folderName}/bicepTemplate.bicep"; // Adjust the file extension as needed.

            // Prepare the commit message for the new file.
            string commitMessage = "Add new Bicep template";

            // Get the reference to the default branch (e.g., main).
            var branch = await _gitHubClient.Repository.Branch.Get(_gitHubOptions.Owner, _gitHubOptions.Repo, "main");

            // Create the file in the repository.
            var createChangeSet = new CreateFileRequest(commitMessage, bicepContent, branch.Commit.Sha)
            {
                Branch = "main"
            };

            await _gitHubClient.Repository.Content.CreateFile(_gitHubOptions.Owner, _gitHubOptions.Repo, filePath, createChangeSet);

            return folderName;
        }
    }
}
