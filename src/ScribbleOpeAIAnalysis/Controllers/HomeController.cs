using System.Security.Policy;
using System.Text.Json;
using Ci.Sequential;
using Markdig;
using Microsoft.AspNetCore.Mvc;
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
            string url = string.Empty;
            if (uploadFile.Length > 0)
            {
                var fileName = $"{DateTimeOffset.UtcNow:yyyy-MM-dd-HH:mm:ss:zzz}-{GuidSequential.NewGuid().ToString()}{Path.GetExtension(uploadFile.FileName)}";
                await _storageProvider.SaveBlobStreamAsync("images", $"{fileName}", uploadFile.OpenReadStream()).ConfigureAwait(false);

                url = _storageProvider.GetBlobSasUrl("images", $"{fileName}", DateTimeOffset.Now.AddDays(1));
            }

            if (!string.IsNullOrWhiteSpace(url))
            {
                var sss = $"{_rootUrl}/api/Image/AnalysisImage?url={Uri.EscapeDataString(url)}";
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

                return View();
            }

            return View();
        }
        [HttpPost]
        public async Task<IActionResult> BicepRegistry(string armtemplateLink, string architectureName)
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
        public async Task<IActionResult> Template(List<string> target, string type)
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

            // Get the bicep template
            var input = string.Join(",", targetList);
            var response = await _httpClient.GetAsync($"{_rootUrl}/api/Image/GetTemplates/{input}");
            if (response.IsSuccessStatusCode)
            {
                var jsonContent = await response.Content.ReadAsStringAsync();
                // Log the JSON content for inspection
                Console.WriteLine(jsonContent);
                // Parse the JSON array with case-insensitive deserialization
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                // Parse the JSON array
                // var jsonArray = JsonSerializer.Deserialize<string[]>(jsonContent);
                var jsonArray = JsonSerializer.Deserialize<TemplateData[]>(jsonContent, options);

                if (jsonArray != null && jsonArray.Length >= 1)
                {

                    //var templateData =  JsonSerializer.Deserialize<TemplateData>(jsonArray[0]);
                    var templateData = jsonArray[0];
                    // The first element is the name template
                    var nameTemplate = templateData.NameTemplate;
                    ViewBag.nameTemplate = nameTemplate;

                    // The second element is the Bicep template
                    var bicepTemplate = templateData.BicepTemplate;
                    ViewBag.bicepTemplate = bicepTemplate;

                    // The third element is the ARM template
                    var armTemplate = templateData.ArmTemplate;
                    ViewBag.armTemplate = armTemplate;

                    // Upload the ARM template to Azure Storage
                    string armFileName = $"{DateTimeOffset.UtcNow:yyyy-MM-dd-HH-mm-ss}-{GuidSequential.NewGuid()}.json";

                    // Convert ARM template string to stream
                    using (var stream = new MemoryStream())
                    using (var writer = new StreamWriter(stream))
                    {
                        writer.Write(armTemplate);
                        writer.Flush();
                        stream.Position = 0;

                        // Save ARM template to blob storage
                        await _storageProvider.SaveBlobStreamAsync("arm-templates", armFileName, stream).ConfigureAwait(false);

                        // Generate SAS URL for the uploaded ARM template file
                        var armUrl = _storageProvider.GetBlobSasUrl("arm-templates", armFileName, DateTimeOffset.Now.AddDays(1));
                        ViewBag.armUrl = armUrl;
                    }
                }
            }

            ViewBag.target = targetList;
            return View();
        }

    }
    public class TemplateData
    {
        public string NameTemplate { get; set; }
        public string BicepTemplate { get; set; }
        public string ArmTemplate { get; set; }
    }
}
