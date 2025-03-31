using Octokit;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ScribbleOpeAIAnalysis.Model;

namespace ScribbleOpeAIAnalysis.Services
{
    public class GitHubService
    {
        private readonly GitHubClient _gitHubClient;
        private readonly GitHubOption _gitHubOptions;
        private readonly HttpClient _httpClient;

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
        /// <param name="bicepContent">The content or link to the Bicep file.</param>
        public async Task<string> CreateFolderAndAddBicepFileAsync(string bicepContentUrl, string architectureName)
        {

            // Fetch the content of the Bicep file from Blob Storage
            var response = await _httpClient.GetAsync(bicepContentUrl);
            response.EnsureSuccessStatusCode();
            var bicepContent = await response.Content.ReadAsStringAsync();
            // Generate a random folder name
            string folderName = (string.IsNullOrEmpty(architectureName) ? "" + Guid.NewGuid().ToString() : architectureName + "-" +  Guid.NewGuid().ToString());

            // Define the file path within the new folder
            string filePath = $"BicepDemoRegistry/{folderName}/bicepTemplate.bicep"; // Adjust the file extension as needed

            // Prepare the commit message
            string commitMessage = "Add new Bicep template";

            // Get the reference to the default branch (e.g., main)
            var branch = await _gitHubClient.Repository.Branch.Get(_gitHubOptions.Owner, _gitHubOptions.Repo, "main");

            // Create the file
            var createChangeSet = new CreateFileRequest(commitMessage, bicepContent, branch.Commit.Sha)
            {
                Branch = "main"
            };

            await _gitHubClient.Repository.Content.CreateFile(_gitHubOptions.Owner, _gitHubOptions.Repo, filePath, createChangeSet);
            
            return folderName;
        }
    }
}
