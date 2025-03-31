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
    public class HomeController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly string _rootUrl;
        private readonly IStorageProvider _storageProvider;
        private readonly GitHubService _gitHubService;

        public HomeController(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, IStorageProvider storageProvider, GitHubService gitHubService)
        {
            _httpClient = httpClient;
            _storageProvider = storageProvider;
            _rootUrl = $"{httpContextAccessor.HttpContext.Request.Scheme}://{httpContextAccessor.HttpContext.Request.Host}{httpContextAccessor.HttpContext.Request.PathBase}";
            _gitHubService = gitHubService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult AnalyzeIndex()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Analyze(IFormFile uploadFile)
        {
            Guid id = GuidSequential.NewGuid();
            string url = string.Empty;
            if (uploadFile.Length > 0)
            {
                var fileName = $"{DateTimeOffset.UtcNow:yyyy-MM-dd-HH:mm:ss:zzz}-{id.ToString()}{Path.GetExtension(uploadFile.FileName)}";
                await _storageProvider.SaveBlobStreamAsync("images", $"{fileName}", uploadFile.OpenReadStream()).ConfigureAwait(false);

                url = _storageProvider.GetBlobSasUrl("images", $"{fileName}", DateTimeOffset.Now.AddDays(1));
            }

            if (!string.IsNullOrWhiteSpace(url))
            {
                var response = await _httpClient.GetAsync($"{_rootUrl}/api/Image/AnalysisImage?url={Uri.EscapeDataString(url)}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var jsonDocument = JsonDocument.Parse(content);
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

                ViewBag.imageUrl = url;
                ViewBag.id = id;

                return View();
            }

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Reference(List<string> content, string imageUrl)
        {
            var result = new List<string>();
            if (content.Any())
            {
                var input = string.Join(", ", content);
                var response = await _httpClient.GetAsync($"{_rootUrl}/api/Image/GetArchitectureBlurb/{input}");
                if (response.IsSuccessStatusCode)
                {
                    var markdown = await response.Content.ReadAsStringAsync();
                    var html = Markdown.ToHtml(markdown);
                    result.Add(html);
                }
            }

            ViewBag.imageUrl = imageUrl;
            ViewBag.content = content;
            ViewBag.result = result;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Template(Guid? id, List<string> target, string type, string imageUrl)
        {
            if (target.Count < 1)
                return BadRequest("No target specified.");

            var targetList = target.First().Split(",").ToList();

            if (targetList.Count < 1)
                return BadRequest("No target specified.");

            // Validate type and parameters
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
            var response = await _httpClient.GetAsync($"{_rootUrl}/api/Image/GetTemplates/{input}");
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

                    // write bicep to local temp file
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

                    // Generate DemoDeploy template json file
                    var model = new DemoDeployTemplateModel();
                    model.Title = templateModel.Name;
                    model.Description = templateModel.Description;
                    model.Preview = imageUrl;
                    model.Website = "https://github.com/ghoshdipanjan/ScribbleToAzureRiches";
                    model.Author = "ScribbleToAzureRiches";
                    model.Source = "https://github.com/ghoshdipanjan/ScribbleToAzureRiches";
                    model.Tags = targetList;
                    model.DemoGuide =
                        "https://raw.githubusercontent.com/ghoshdipanjan/ScribbleToAzureRiches/refs/heads/main/README.md";
                    model.Cost = "10.00";
                    model.DeployTime = "10";
                    model.PreReqs =
                        "https://raw.githubusercontent.com/petender/ConferenceAPI/refs/heads/main/prereqs.md";

                    //write  DemoDeploy template to local temp file
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
                            // add this map file into the "images" directory in the zip archive
                            zip.AddFile(tempArmFilePath, "/");
                            zip.AddFile(tempBicepFilePath, "/");
                            zip.AddFile(tempTemplateFilePath, "/");

                            // add the report into a different directory in the archive
                            zip.Save(zipStream);

                            // Save the zip file to blob storage
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

        [HttpPost]
        public async Task<IActionResult> BicepRegistry(Guid? id, string armtemplateLink, string architectureName)
        {
            if (string.IsNullOrWhiteSpace(armtemplateLink))
            {
                TempData["ErrorMessage"] = "ARM URL is invalid.";
                return RedirectToAction("Template");
            }
            {
                // For simplicity, we'll add the link as the content
                string bicepContent = armtemplateLink;

                try
                {
                    string folderName = await _gitHubService.CreateFolderAndAddBicepFileAsync(bicepContent, architectureName);
                    TempData["SuccessMessage"] = "Bicep template added to the registry successfully.";
                    ViewBag.folderName = folderName;
                    // Extract the portion of the string before the last hyphen
                    string displayFolderName = string.Concat(folderName.AsSpan(0, folderName.LastIndexOf('-')), "...");

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
}
