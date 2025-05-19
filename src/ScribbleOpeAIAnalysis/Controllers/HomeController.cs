using System.Text.Json;
using Ci.Sequential;
using Ionic.Zip;
using Markdig;
using Microsoft.AspNetCore.Mvc;
using ScribbleOpeAIAnalysis.Model;
using ScribbleOpeAIAnalysis.Models;
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

        // Table storage service for storing analysis results
        private readonly ITableStorageService _tableStorageService;

        // Logger for logging information
        private readonly ILogger<HomeController> _logger;

        /// <summary>
        /// Constructor for HomeController.
        /// </summary>
        public HomeController(
            HttpClient httpClient,
            IHttpContextAccessor httpContextAccessor,
            IStorageProvider storageProvider,
            GitHubService gitHubService,
            ITableStorageService tableStorageService,
            ILogger<HomeController> logger)
        {
            _httpClient = httpClient;
            _storageProvider = storageProvider;
            _rootUrl = $"{httpContextAccessor.HttpContext.Request.Scheme}://{httpContextAccessor.HttpContext.Request.Host}{httpContextAccessor.HttpContext.Request.PathBase}";
            _gitHubService = gitHubService;
            _tableStorageService = tableStorageService;
            _logger = logger;
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
        /// Handles file upload and performs image analysis. Saves the uploaded file to blob storage, calls the analysis API, and stores the result in Table Storage.
        /// </summary>
        /// <param name="uploadFile">Uploaded file to be analyzed.</param>
        /// <returns>View with analysis results or redirects to Analyze with the analysis ID.</returns>
        [HttpPost]
        public async Task<IActionResult> Analyze(IFormFile uploadFile)
        {
            _logger.LogInformation("Beginning file analysis at {Time}", DateTime.UtcNow);

            if (uploadFile == null || uploadFile.Length == 0)
            {
                _logger.LogWarning("No file uploaded or empty file");
                return View("AnalyzeIndex");
            }

            // Generate a new GUID for the operation
            Guid id = GuidSequential.NewGuid();
            _logger.LogInformation("Generated analysis ID: {Id}", id);

            string url = string.Empty;

            // Upload the file
            try
            {
                var fileName = $"{id.ToString()}{Path.GetExtension(uploadFile.FileName)}";
                _logger.LogInformation("Saving file {FileName}", fileName);
                
                await _storageProvider.SaveBlobStreamAsync("images", fileName, uploadFile.OpenReadStream());
                url = _storageProvider.GetBlobSasUrl("images", fileName, DateTimeOffset.Now.AddYears(10));
                _logger.LogInformation("File saved successfully with URL {Url}", url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving file to blob storage");
                return View("AnalyzeIndex");
            }

            if (!string.IsNullOrWhiteSpace(url))
            {
                try
                {
                    _logger.LogInformation("Calling analysis API with URL {Url}", url);
                    var response = await _httpClient.GetAsync($"{_rootUrl}/api/Analyze/AnalyzeImage?url={Uri.EscapeDataString(url)}");
                    
                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("Analysis API call successful");
                        var content = await response.Content.ReadAsStringAsync();
                        var jsonDocument = JsonDocument.Parse(content);

                        if (jsonDocument.RootElement.TryGetProperty("description", out JsonElement descriptionElement))
                        {
                            var list = descriptionElement.GetString().Split(", ").ToList();
                            _logger.LogInformation("Found {Count} components: {Components}", list.Count, string.Join(", ", list));

                            await _tableStorageService.UpsertAnalysisResultAsync(id.ToString(), new Dictionary<string, object>
                            {
                                { nameof(AnalysisResult.Component), list },
                                { nameof(AnalysisResult.ImageUrl), url }
                            });

                            TempData["ImageUrl"] = url;
                            TempData["AnalysisId"] = id.ToString();

                            return RedirectToAction("Analyze", new { id = id.ToString() });
                        }
                        else
                        {
                            _logger.LogWarning("Description not found in response");
                            ViewBag.content = "Description not found.";
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Analysis API call failed with status {Status}", response.StatusCode);
                        ViewBag.content = "Error retrieving analysis.";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during analysis");
                    ViewBag.content = $"Error during analysis: {ex.Message}";
                }
            }

            return View();
        }

        /// <summary>
        /// Loads analysis results from Table Storage by ID and displays them.
        /// </summary>
        /// <param name="id">The analysis result ID.</param>
        /// <returns>View with analysis content and image URL if found.</returns>
        [HttpGet]
        public async Task<IActionResult> Analyze(string id = null)
        {
            if (!string.IsNullOrEmpty(id))
            {
                // Restore data from Table Storage
                var storedResult = await _tableStorageService.GetAnalysisResultAsync(id);
                if (storedResult != null)
                {
                    ViewBag.content = storedResult.GetComponentList();
                    ViewBag.imageUrl = storedResult.ImageUrl;
                }
                ViewBag.id = id;
            }
            return View();
        }

        /// <summary>
        /// Retrieves reference information based on analyzed content from Table Storage. If architecture details are missing, calls the API to generate them and saves the result.
        /// </summary>
        /// <param name="id">The GUID of the analysis result to retrieve.</param>
        /// <returns>View with reference information.</returns>
        [HttpGet]
        public async Task<IActionResult> Reference(Guid? id)
        {
            if (!id.HasValue)
                return RedirectToAction("Index");

            // Get stored data from Table Storage
            var storedResult = await _tableStorageService.GetAnalysisResultAsync(id.ToString());
            if (storedResult == null)
            {
                return RedirectToAction("Index");
            }

            // Get required data
            var content = storedResult.GetComponentList();
            var imageUrl = storedResult.ImageUrl;
            var result = new List<string>();

            // If ArchitectureDetail already exists, return it directly
            if (!string.IsNullOrEmpty(storedResult.ArchitectureDetail))
            {
                result.Add(Markdown.ToHtml(storedResult.ArchitectureDetail));
            }
            // If ArchitectureDetail does not exist but content exists, call API to get it
            else if (content.Any())
            {
                var input = string.Join(", ", content);
                var response = await _httpClient.GetAsync($"{_rootUrl}/api/Analyze/ArchitectureDetail/{input}");

                if (response.IsSuccessStatusCode)
                {
                    var markdown = await response.Content.ReadAsStringAsync();
                    result.Add(Markdown.ToHtml(markdown));

                    // Save new ArchitectureDetail
                    await _tableStorageService.UpsertAnalysisResultAsync(id.ToString(), new Dictionary<string, object>
                    {
                        { "ArchitectureDetail", markdown }
                    });
                }
            }

            // Pass data to view
            ViewBag.imageUrl = imageUrl;
            ViewBag.content = content;
            ViewBag.result = result;
            ViewBag.id = id;

            return View();
        }

        /// <summary>
        /// Generates templates based on the provided targets and type. Retrieves or generates ARM/Bicep templates and stores them in Table Storage.
        /// </summary>
        /// <param name="id">The GUID of the analysis result.</param>
        /// <param name="type">Type of template generation (single or multiple).</param>
        /// <returns>View with generated templates.</returns>
        [HttpGet]
        public async Task<IActionResult> Template(Guid? id, string type)
        {
            // Check required parameters
            if (!id.HasValue)
                return RedirectToAction("Index");

            // Get stored data from Table Storage
            var storedResult = await _tableStorageService.GetAnalysisResultAsync(id.Value.ToString());
            if (storedResult == null)
                return RedirectToAction("Index");

            // Get target and imageUrl from Table Storage
            var targetList = storedResult.GetComponentList();
            var imageUrl = storedResult.ImageUrl;

            if (targetList == null || !targetList.Any())
                return BadRequest("No target specified.");

            ViewBag.id = id;

            // Validate template generation type
            switch (type?.ToLower())
            {
                case "single" when targetList.Count != 1:
                    return BadRequest("Only one target is allowed for single type.");
                case "single":
                case "multiple":
                    break;
                default:
                    return BadRequest($"Wrong type: {type}");
            }

            TemplateModel resultTemplate;            // If multiple type, try to read existing template from Table Storage first
            if (type?.ToLower() == "multiple" &&
                !string.IsNullOrEmpty(storedResult.BicepTemplate) &&
                !string.IsNullOrEmpty(storedResult.ArmTemplate))
            {
                resultTemplate = new TemplateModel
                {
                    BicepTemplate = storedResult.BicepTemplate,
                    ArmTemplate = storedResult.ArmTemplate,
                    Name = storedResult.GetType().GetProperty("TemplateName") != null ? (string)storedResult.GetType().GetProperty("TemplateName").GetValue(storedResult) : null,
                    Description = storedResult.GetType().GetProperty("TemplateDescription") != null ? (string)storedResult.GetType().GetProperty("TemplateDescription").GetValue(storedResult) : null
                };
                // fallback default value
                if (string.IsNullOrEmpty(resultTemplate.Name))
                    resultTemplate.Name = targetList != null && targetList.Any() ? string.Join(", ", targetList) : string.Empty;
                if (string.IsNullOrEmpty(resultTemplate.Description))
                    resultTemplate.Description = "Generated template for: " + (targetList != null && targetList.Any() ? string.Join(", ", targetList) : string.Empty);
                ViewBag.templateModel = resultTemplate;
                ViewBag.target = targetList;
                ViewBag.armUrl = storedResult.ArmUrl ?? string.Empty;
                return View();
            }

            // Call API to get new template
            var input = string.Join(",", targetList);
            var response = await _httpClient.GetAsync($"{_rootUrl}/api/Analyze/Templates/{input}");
            if (!response.IsSuccessStatusCode)
                return BadRequest("Failed to generate templates.");

            var jsonContent = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var templateModels = JsonSerializer.Deserialize<List<TemplateModel>>(jsonContent, options);
            resultTemplate = templateModels?.FirstOrDefault();
            if (resultTemplate == null)
                return BadRequest("No template generated.");
            // Only generate armUrl, do not generate zip
            var armUrl = await GenerateArmTemplateFile(id.Value, resultTemplate.ArmTemplate);

            // Save template and URLs to Table Storage
            var updateData = new Dictionary<string, object>
            {
                { "ArmUrl", armUrl }
            };

            // If multiple type, also save template content
            if (type?.ToLower() == "multiple")
            {
                updateData.Add("TemplateName", resultTemplate.Name);
                updateData.Add("TemplateDescription", resultTemplate.Description);
                updateData.Add("BicepTemplate", resultTemplate.BicepTemplate);
                updateData.Add("ArmTemplate", resultTemplate.ArmTemplate);
            }

            await _tableStorageService.UpsertAnalysisResultAsync(id.Value.ToString(), updateData);

            // Pass data to view
            ViewBag.templateModel = resultTemplate;
            ViewBag.target = targetList;
            ViewBag.armUrl = armUrl;
            return View();
        }

        /// <summary>
        /// Generates and uploads the ARM template file to blob storage, then returns a SAS URL for download.
        /// </summary>
        /// <param name="id">The GUID for the template.</param>
        /// <param name="armTemplateContent">The ARM template content.</param>
        /// <returns>SAS URL to the uploaded ARM template file.</returns>
        private async Task<string> GenerateArmTemplateFile(Guid id, string armTemplateContent)
        {
            string armFileName = "azuredeploy.json";
            var tempArmFilePath = Path.Combine(Path.GetTempPath(), armFileName);
            using (var armStream = new MemoryStream())
            using (var armWriter = new StreamWriter(armStream))
            {
                armWriter.Write(armTemplateContent);
                armWriter.Flush();
                armStream.Position = 0;
                using (var fileStream = new FileStream(tempArmFilePath, FileMode.Create))
                    await armStream.CopyToAsync(fileStream);
                armStream.Position = 0;
                await _storageProvider.SaveBlobStreamAsync("arm-templates", $"{id}.json", armStream);
            }
            return _storageProvider.GetBlobSasUrl("arm-templates", $"{id}.json", DateTimeOffset.Now.AddDays(1));
        }

        /// <summary>
        /// Generates a temporary Bicep template file and returns its file path.
        /// </summary>
        /// <param name="bicepTemplateContent">The Bicep template content.</param>
        /// <returns>File path to the generated Bicep template file.</returns>
        private async Task<string> GenerateBicepTemplateFile(string bicepTemplateContent)
        {
            string bicepFileName = "azuredeploy.bicep";
            var tempBicepFilePath = Path.Combine(Path.GetTempPath(), bicepFileName);
            using (var bicepStream = new MemoryStream())
            using (var bicepWriter = new StreamWriter(bicepStream))
            {
                bicepWriter.Write(bicepTemplateContent);
                bicepWriter.Flush();
                bicepStream.Position = 0;
                using (var fileStream = new FileStream(tempBicepFilePath, FileMode.Create))
                    await bicepStream.CopyToAsync(fileStream);
            }
            return tempBicepFilePath;
        }

        /// <summary>
        /// Creates a zip file containing the ARM, Bicep, and demo template files, uploads it to blob storage, and returns a SAS URL.
        /// </summary>
        /// <param name="id">The GUID for the zip file.</param>
        /// <param name="tempArmFilePath">Path to the ARM template file.</param>
        /// <param name="tempBicepFilePath">Path to the Bicep template file.</param>
        /// <param name="tempTemplateFilePath">Path to the demo template JSON file.</param>
        /// <returns>SAS URL to the uploaded zip file.</returns>
        private async Task<string> GenerateDemoZipFile(Guid id, string tempArmFilePath, string tempBicepFilePath, string tempTemplateFilePath)
        {
            string zipUrl = string.Empty;
            using (var zipStream = new MemoryStream())
            {
                using (var zip = new ZipFile())
                {
                    zip.AddFile(tempArmFilePath, "/");
                    zip.AddFile(tempBicepFilePath, "/");
                    zip.AddFile(tempTemplateFilePath, "/");
                    zip.Save(zipStream);
                }
                zipStream.Position = 0;
                await _storageProvider.SaveBlobStreamAsync("demo-deploy-zip", $"{id}.zip", zipStream);
                zipUrl = _storageProvider.GetBlobSasUrl("demo-deploy-zip", $"{id}.zip", DateTimeOffset.Now.AddDays(1));
            }
            return zipUrl;
        }

        /// <summary>
        /// Generates the demo deploy package zip file by creating the Bicep template, demo template JSON, and zipping them with the ARM template.
        /// </summary>
        /// <param name="id">The GUID for the package.</param>
        /// <param name="templateModel">The template model containing template data.</param>
        /// <param name="targetList">List of target components.</param>
        /// <param name="imageUrl">Image URL for the demo template.</param>
        /// <returns>SAS URL to the generated zip file.</returns>
        private async Task<string> GenerateTemplateFiles(Guid id, TemplateModel templateModel, List<string> targetList, string imageUrl)
        {
            // Bicep template
            var tempBicepFilePath = await GenerateBicepTemplateFile(templateModel.BicepTemplate);
            // Demo template JSON
            string templateFileName = "template.json";
            var tempTemplateFilePath = Path.Combine(Path.GetTempPath(), templateFileName);
            var demoModel = new DemoDeployTemplateModel
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
            using (var templateStream = new MemoryStream())
            using (var templateWriter = new StreamWriter(templateStream))
            {
                templateWriter.Write(JsonSerializer.Serialize(demoModel));
                templateWriter.Flush();
                templateStream.Position = 0;
                using (var fileStream = new FileStream(tempTemplateFilePath, FileMode.Create))
                    await templateStream.CopyToAsync(fileStream);
            }

            // Zip
            var zipUrl = await GenerateDemoZipFile(id, Path.Combine(Path.GetTempPath(), "azuredeploy.json"), tempBicepFilePath, tempTemplateFilePath);
            return zipUrl;
        }

        /// <summary>
        /// Adds a Bicep template to the registry by creating a folder and uploading the Bicep file to GitHub.
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

        /// <summary>
        /// Generates and returns a download link for the demo deploy package zip file based on user input and stored template data.
        /// </summary>
        /// <param name="id">The GUID for the analysis result.</param>
        /// <param name="title">Title for the demo package.</param>
        /// <param name="description">Description for the demo package.</param>
        /// <param name="imageUrl">Image URL for the demo package.</param>
        /// <returns>JSON result with the download URL for the zip file.</returns>
        [HttpPost]
        public async Task<IActionResult> DownloadDemoDeployZip(Guid id, string title, string description, string imageUrl)
        {
            // Get Table Storage content
            var storedResult = await _tableStorageService.GetAnalysisResultAsync(id.ToString());
            if (storedResult == null)
                return NotFound();

            var targetList = storedResult.GetComponentList();
            var templateModel = new TemplateModel
            {
                Name = title,
                Description = description,
                BicepTemplate = storedResult.BicepTemplate,
                ArmTemplate = storedResult.ArmTemplate
            };
            // Only generate zip, do not generate armUrl
            var zipUrl = await GenerateTemplateFiles(id, templateModel, targetList, imageUrl);
            return Json(new { url = zipUrl });
        }
    }
}
