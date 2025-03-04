using System.Security.Policy;
using System.Text.Json;
using Ci.Sequential;
using Markdig;
using Microsoft.AspNetCore.Mvc;
using TwentyTwenty.Storage;

namespace ScribbleOpeAIAnalysis.Controllers
{
    public class HomeController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly string _rootUrl;
        private readonly IStorageProvider _storageProvider;

        public HomeController(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, IStorageProvider storageProvider)
        {
            _httpClient = httpClient;
            _storageProvider = storageProvider;
            _rootUrl = $"{httpContextAccessor.HttpContext.Request.Scheme}://{httpContextAccessor.HttpContext.Request.Host}{httpContextAccessor.HttpContext.Request.PathBase}";
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

                // Parse the JSON array
                var jsonArray = JsonSerializer.Deserialize<string[]>(jsonContent);

                if (jsonArray != null && jsonArray.Length >= 2)
                {
                    // The first element is the Bicep template
                    var bicepTemplate = jsonArray[0];
                    ViewBag.bicepTemplate = bicepTemplate;

                    // The second element is the ARM template
                    var armTemplate = jsonArray[1];
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
}
