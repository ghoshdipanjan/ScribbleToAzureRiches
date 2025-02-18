using System.Text.Json;
using Markdig;
using Microsoft.AspNetCore.Mvc;

namespace ScribbleOpeAIAnalysis.Controllers
{
    public class HomeController : Controller
    {
        private readonly HttpClient _httpClient;

        public HomeController( HttpClient httpClient)
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
        public async Task<IActionResult> Reference(string content)
        {
            var result = new List<string>();
            if (!string.IsNullOrWhiteSpace(content))
            {
                var contentList = content.Split(", ").ToList();
                foreach (var item in contentList)
                {
                    var response = await _httpClient.GetAsync($"https://localhost:4458/api/Image/GetArchitectureBlurb/{item}");
                    if (response.IsSuccessStatusCode)
                    {
                        var markdown = await response.Content.ReadAsStringAsync();
                        var html = Markdown.ToHtml(markdown);
                        result.Add(html);
                    }
                }

            }

            ViewBag.content = result;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Template(string content)
        {
            return View();
        }

        //[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        //public IActionResult Error()
        //{
        //    return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        //}
    }
}
