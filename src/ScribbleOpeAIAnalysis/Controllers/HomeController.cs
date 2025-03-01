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

            // Validate type and parameters
            switch (type)
            {
                case "single":
                    if (target.Count != 1)
                        return BadRequest("Only one target is allowed for single type.");
                    break;
                case "multiple":
                    break;
                default:
                    return BadRequest($"Wrong type: {type}");
            }

            // Get the template
            var input = string.Join(",", target);
            var response = await _httpClient.GetAsync($"{_rootUrl}/api/Image/GetTemplate/{input}");
            if (response.IsSuccessStatusCode)
            {
                var template = await response.Content.ReadAsStringAsync();
                ViewBag.result = template;
            }

            ViewBag.target = target;
            return View();
        }

    }
}
