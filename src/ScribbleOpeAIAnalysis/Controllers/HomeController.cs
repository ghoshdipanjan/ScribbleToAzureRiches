using System.Text.Json;
using Ci.Sequential;
using Ionic.Zip;
using Markdig;
using Microsoft.AspNetCore.Mvc;
using ScribbleOpeAIAnalysis.Model;
using ScribbleOpeAIAnalysis.Services;
using TwentyTwenty.Storage;

namespace ScribbleOpeAIAnalysis.Controllers
{
    /// <summary>
    /// Controller for handling home-related actions, including file uploads, analysis, and template generation.
    /// </summary>
    public class HomeController : Controller
    {
        // HttpClient for making HTTP requests
        private readonly HttpClient _httpClient;

        // Root URL of the application
        private readonly string _rootUrl;

        // Storage provider for handling blob storage operations
        private readonly IStorageProvider _storageProvider;

        // GitHub service for interacting with GitHub repositories
        private readonly GitHubService _gitHubService;

        /// <summary>
        /// Constructor for HomeController.
        /// </summary>
        /// <param name="httpClient">HttpClient for making HTTP requests.</param>
        /// <param name="httpContextAccessor">Accessor for HTTP context to retrieve request details.</param>
        /// <param name="storageProvider">Storage provider for blob storage operations.</param>
        /// <param name="gitHubService">GitHub service for repository interactions.</param>
        public HomeController(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, IStorageProvider storageProvider, GitHubService gitHubService)
        {
            _httpClient = httpClient;
            _storageProvider = storageProvider;
            _rootUrl = $"{httpContextAccessor.HttpContext.Request.Scheme}://{httpContextAccessor.HttpContext.Request.Host}{httpContextAccessor.HttpContext.Request.PathBase}";
            _gitHubService = gitHubService;
        }

        /// <summary>
        /// Default action for the home page.
        /// </summary>
        /// <returns>View for the home page.</returns>
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Displays the analysis index page.
        /// </summary>
        /// <returns>View for the analysis index page.</returns>
        [HttpGet]
        public IActionResult AnalyzeIndex()
        {
            return View();
        }

        /// <summary>
        /// Handles file upload and performs image analysis.
        /// </summary>
        /// <param name="uploadFile">Uploaded file to be analyzed.</param>
        /// <returns>View with analysis results.</returns>
        [HttpPost]
        public async Task<IActionResult> Analyze(IFormFile uploadFile)
        {
            // Generate a new GUID for the operation
            Guid id = GuidSequential.NewGuid();
            string url = string.Empty;

            // Check if the uploaded file has content
            if (uploadFile.Length > 0)
            {
                // Generate a unique file name
                var fileName = $"{DateTimeOffset.UtcNow:yyyy-MM-dd-HH:mm:ss:zzz}-{id.ToString()}{Path.GetExtension(uploadFile.FileName)}";

                // Save the file to blob storage
                await _storageProvider.SaveBlobStreamAsync("images", $"{fileName}", uploadFile.OpenReadStream()).ConfigureAwait(false);

                // Generate a SAS URL for the uploaded file
                url = _storageProvider.GetBlobSasUrl("images", $"{fileName}", DateTimeOffset.Now.AddDays(1));
            }

            if (!string.IsNullOrWhiteSpace(url))
            {
                // Call the API to analyze the image
                var response = await _httpClient.GetAsync($"{_rootUrl}/api/Analyze/AnalyzeImage?url={Uri.EscapeDataString(url)}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var jsonDocument = JsonDocument.Parse(content);

                    // Extract the description from the JSON response
                    if (jsonDocument.RootElement.TryGetProperty("description", out JsonElement descriptionElement))
                    {
                        var list = descriptionElement.GetString().Split(", ").ToList();
                        ViewBag.content = list;
                    }
                    else
                    {
                        ViewBag.content = "Description not found.";
                    }
                }
                else
                {
                    ViewBag.content = "Error retrieving analysis.";
                }

                // Pass the image URL and ID to the view
                ViewBag.imageUrl = url;
                ViewBag.id = id;

                return View();
            }

            return View();
        }

        /// <summary>
        /// Retrieves reference information based on analyzed content.
        /// </summary>
        /// <param name="content">List of analyzed content descriptions.</param>
        /// <param name="imageUrl">URL of the analyzed image.</param>
        /// <returns>View with reference information.</returns>
        [HttpGet]
        public async Task<IActionResult> Reference(List<string> content, string imageUrl)
        {
            var result = new List<string>();

            // Check if there is any content to process
            if (content.Any())
            {
                var input = string.Join(", ", content);

                // Call the API to get architecture blurbs
                var response = await _httpClient.GetAsync($"{_rootUrl}/api/Analyze/ArchitectureDetail/{input}");
                if (response.IsSuccessStatusCode)
                {
                    var markdown = await response.Content.ReadAsStringAsync();

                    // Convert markdown to HTML
                    var html = Markdown.ToHtml(markdown);
                    result.Add(html);
                }
            }

            // Pass the results to the view
            ViewBag.imageUrl = imageUrl;
            ViewBag.content = content;
            ViewBag.result = result;
            return View();
        }

        /// <summary>
        /// Generates templates based on the provided targets and type.
        /// </summary>
        /// <param name="id">Optional GUID for the operation.</param>
        /// <param name="target">List of target descriptions.</param>
        /// <param name="type">Type of template generation (e.g., single or multiple).</param>
        /// <param name="imageUrl">URL of the analyzed image.</param>
        /// <returns>View with generated templates.</returns>
        [HttpGet]
        public async Task<IActionResult> Template(Guid? id, List<string> target, string type, string imageUrl)
        {
            if (target.Count < 1)
                return BadRequest("No target specified.");

            var targetList = target.First().Split(",").ToList();

            if (targetList.Count < 1)
                return BadRequest("No target specified.");

            // Validate the type of template generation
            switch (type)
            {
                case "single":
                    if (targetList.Count != 1)
                        return BadRequest("Only one target is allowed for single type.");
                    break;
                case "multiple":
                    break;
                default:
                    return BadRequest($"Wrong type: {type}");
            }

            id ??= GuidSequential.NewGuid();

            // Get the bicep template
            string armFileName = "azuredeploy.json";
            var tempArmFilePath = Path.Combine(Path.GetTempPath(), armFileName);
            string bicepFileName = "azuredeploy.bicep";
            var tempBicepFilePath = Path.Combine(Path.GetTempPath(), bicepFileName);
            string templateFileName = "template.json";
            var tempTemplateFilePath = Path.Combine(Path.GetTempPath(), templateFileName);
            var input = string.Join(",", targetList);
            var response = await _httpClient.GetAsync($"{_rootUrl}/api/Analyze/Templates/{input}");
            if (response.IsSuccessStatusCode)
            {
                var jsonContent = await response.Content.ReadAsStringAsync();

                // Parse the JSON array with case-insensitive deserialization
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                // Parse the JSON array
                var templateModels = JsonSerializer.Deserialize<List<TemplateModel>>(jsonContent, options);
                var templateModel = templateModels.FirstOrDefault();

                if (templateModel != null)
                {
                    ViewBag.templateModel = templateModel;

                    // Convert ARM template string to stream
                    using (var armStream = new MemoryStream())
                    using (var armWriter = new StreamWriter(armStream))
                    {
                        armWriter.Write(templateModel.ArmTemplate);
                        armWriter.Flush();

                        // Save to local temp file
                        armStream.Position = 0;
                        using (var fileStream = new FileStream(tempArmFilePath, FileMode.Create, FileAccess.Write))
                        {
                            await armStream.CopyToAsync(fileStream);
                        }

                        // Save ARM template to blob storage
                        armStream.Position = 0;
                        await _storageProvider.SaveBlobStreamAsync("arm-templates", $"{id}.json", armStream)
                            .ConfigureAwait(false);

                        // Generate SAS URL for the uploaded ARM template file
                        var armUrl = _storageProvider.GetBlobSasUrl("arm-templates", $"{id}.json",
                            DateTimeOffset.Now.AddDays(1));
                        ViewBag.armUrl = armUrl;
                    }

                    // Write Bicep template to local temp file
                    using (var bicepStream = new MemoryStream())
                    using (var bicepWriter = new StreamWriter(bicepStream))
                    {
                        bicepWriter.Write(templateModel.BicepTemplate);
                        bicepWriter.Flush();
                        bicepStream.Position = 0;

                        // Save Bicep template to local temp file
                        using (var fileStream = new FileStream(tempBicepFilePath, FileMode.Create, FileAccess.Write))
                        {
                            await bicepStream.CopyToAsync(fileStream);
                        }
                    }

                    // Generate DemoDeploy template JSON file
                    var model = new DemoDeployTemplateModel
                    {
                        Title = templateModel.Name,
                        Description = templateModel.Description,
                        Preview = imageUrl,
                        Website = "https://github.com/ghoshdipanjan/ScribbleToAzureRiches",
                        Author = "ScribbleToAzureRiches",
                        Source = "https://github.com/ghoshdipanjan/ScribbleToAzureRiches",
                        Tags = targetList,
                        DemoGuide = "https://raw.githubusercontent.com/ghoshdipanjan/ScribbleToAzureRiches/refs/heads/main/README.md",
                        Cost = "10.00",
                        DeployTime = "10",
                        PreReqs = "https://raw.githubusercontent.com/petender/ConferenceAPI/refs/heads/main/prereqs.md"
                    };

                    // Write DemoDeploy template to local temp file
                    using (var templateStream = new MemoryStream())
                    using (var templateWriter = new StreamWriter(templateStream))
                    {
                        templateWriter.Write(JsonSerializer.Serialize(model));
                        templateWriter.Flush();
                        templateStream.Position = 0;

                        // Save DemoDeploy template to local temp file
                        using (var fileStream = new FileStream(tempTemplateFilePath, FileMode.Create, FileAccess.Write))
                        {
                            await templateStream.CopyToAsync(fileStream);
                        }
                    }

                    // Add the DemoDeploy template JSON file to the zip
                    using (var zipStream = new MemoryStream())
                    {
                        using (ZipFile zip = new ZipFile())
                        {
                            // Add files to the zip archive
                            zip.AddFile(tempArmFilePath, "/");
                            zip.AddFile(tempBicepFilePath, "/");
                            zip.AddFile(tempTemplateFilePath, "/");

                            // Save the zip file to blob storage
                            zip.Save(zipStream);
                            zipStream.Position = 0;
                            await _storageProvider.SaveBlobStreamAsync("demo-deploy-zip", $"{id}.zip", zipStream)
                                .ConfigureAwait(false);

                            // Generate SAS URL for the uploaded zip file
                            var zipUrl = _storageProvider.GetBlobSasUrl("demo-deploy-zip", $"{id}.zip",
                                DateTimeOffset.Now.AddDays(1));
                            ViewBag.zipUrl = zipUrl;
                        }
                    }
                }
            }

            ViewBag.target = targetList;
            return View();
        }

        /// <summary>
        /// Adds a Bicep template to the registry.
        /// </summary>
        /// <param name="id">Optional GUID for the operation.</param>
        /// <param name="armtemplateLink">Link to the ARM template.</param>
        /// <param name="architectureName">Name of the architecture.</param>
        /// <returns>View with the result of the operation.</returns>
        [HttpPost]
        public async Task<IActionResult> BicepRegistry(Guid? id, string armtemplateLink, string architectureName)
        {
            if (string.IsNullOrWhiteSpace(armtemplateLink))
            {
                TempData["ErrorMessage"] = "ARM URL is invalid.";
                return RedirectToAction("Template");
            }

            // For simplicity, we'll add the link as the content
            string bicepContent = armtemplateLink;

            try
            {
                // Create a folder and add the Bicep file to the GitHub repository
                string folderName = await _gitHubService.CreateFolderAndAddBicepFileAsync(bicepContent, architectureName);
                TempData["SuccessMessage"] = "Bicep template added to the registry successfully.";
                ViewBag.folderName = folderName;

                // Extract the portion of the string before the last hyphen
                string displayFolderName = string.Concat(folderName.AsSpan(0, folderName.LastIndexOf('-')), "...");

                // Display success message
                ViewBag.content = "<b>Congratulations!</b><br/>Your project Bicep template is now successfully added to the Bicep template registry at folder <b>" + displayFolderName + "</b>.<br/><br/>Feel free to <b>visit, use, and share</b> your project template from the <b>Template Registry Hub at <a href='https://github.com/ghoshdipanjan/ScribbleToAzureRiches/tree/main/BicepDemoRegistry'>GitHub</a></b>.";
            }
            catch (Exception ex)
            {
                // Log the exception (you can use a logging framework)
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
            }

            return View();
        }
    }
}
