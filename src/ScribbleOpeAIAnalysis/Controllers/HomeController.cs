using System.Text.Json;
using Markdig;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;

namespace ScribbleOpeAIAnalysis.Controllers
{
    public class HomeController : Controller
    {
        private readonly HttpClient _httpClient;

        public HomeController(HttpClient httpClient)
        {
            _httpClient = httpClient;
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

        [HttpGet]
        public async Task<IActionResult> Analyze(string fileName = "")
        {
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                var response = await _httpClient.GetAsync($"https://localhost:4458/api/Image/AnalysisImage/{fileName}");
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

                return View();
            }

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Reference(List<string> content)
        {
            var result = new List<string>();
            if (content.Any())
            {
                var input = string.Join(", ", content);
                var response = await _httpClient.GetAsync($"https://localhost:4458/api/Image/GetArchitectureBlurb/{input}");
                if (response.IsSuccessStatusCode)
                {
                    var markdown = await response.Content.ReadAsStringAsync();
                    var html = Markdown.ToHtml(markdown);
                    result.Add(html);
                }
            }

            ViewBag.content = content;
            ViewBag.result = result;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Bicep(List<string> content)
        {
            if (content.Any())
            {
                var input = string.Join(", ", content);
                var response = await _httpClient.GetAsync($"https://localhost:4458/api/Image/GetArmTemplates/{input}");
                if (response.IsSuccessStatusCode)
                {
                    var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().UseBootstrap().Build();

                    var markdown = await response.Content.ReadAsStringAsync();
                    var html = Markdown.ToHtml(markdown, pipeline);
                    ViewBag.result = html;
                }
            }

            ViewBag.content = content;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Deploy(string item)
        {
            if (!string.IsNullOrWhiteSpace(item))
            {
                item = item.ToLower().Trim();
                var response = await _httpClient.GetAsync($"https://localhost:4458/api/Image/DeployResource/{item}");
                if (response.IsSuccessStatusCode)
                {
                    var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().UseBootstrap().Build();

                    var markdown = await response.Content.ReadAsStringAsync();
                    var html = Markdown.ToHtml(markdown, pipeline);
                    ViewBag.result = html;
                }
            }

            return Ok("Deploy complete, you can close this tab now.");
        }

        //[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        //public IActionResult Error()
        //{
        //    return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        //}
    }
}
