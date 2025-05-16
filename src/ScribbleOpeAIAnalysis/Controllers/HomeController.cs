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

        /// <summary>
        /// Constructor for HomeController.
        /// </summary>
        public HomeController(
            HttpClient httpClient,
            IHttpContextAccessor httpContextAccessor,
            IStorageProvider storageProvider,
            GitHubService gitHubService,
            ITableStorageService tableStorageService)
        {
            _httpClient = httpClient;
            _storageProvider = storageProvider;
            _rootUrl = $"{httpContextAccessor.HttpContext.Request.Scheme}://{httpContextAccessor.HttpContext.Request.Host}{httpContextAccessor.HttpContext.Request.PathBase}";
            _gitHubService = gitHubService;
            _tableStorageService = tableStorageService;
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
                var fileName = $"{id.ToString()}{Path.GetExtension(uploadFile.FileName)}";

                // Save the file to blob storage
                await _storageProvider.SaveBlobStreamAsync("images", $"{fileName}", uploadFile.OpenReadStream()).ConfigureAwait(false);

                // Generate a SAS URL for the uploaded file
                url = _storageProvider.GetBlobSasUrl("images", $"{fileName}", DateTimeOffset.Now.AddYears(10));
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
                        // Store component list and image URL in Table Storage
                        await _tableStorageService.UpsertAnalysisResultAsync(id.ToString(), new Dictionary<string, object>
                        {
                            { nameof(AnalysisResult.Component), list },
                            { nameof(AnalysisResult.ImageUrl), url }
                        });// Pass data to view via TempData
                        TempData["ImageUrl"] = url;
                        TempData["AnalysisId"] = id.ToString();

                        return RedirectToAction("Analyze", new { id = id.ToString() });
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
            }

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Analyze(string id = null)
        {
            if (!string.IsNullOrEmpty(id))
            {
                // 從 Table Storage 恢復資料
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
        /// Retrieves reference information based on analyzed content from Table Storage.
        /// </summary>
        /// <param name="id">The GUID of the analysis result to retrieve.</param>
        /// <returns>View with reference information.</returns>
        [HttpGet]
        public async Task<IActionResult> Reference(Guid? id)
        {
            if (!id.HasValue)
                return RedirectToAction("Index");

            // 從 Table Storage 取得儲存的資料
            var storedResult = await _tableStorageService.GetAnalysisResultAsync(id.ToString());
            if (storedResult == null)
            {
                return RedirectToAction("Index");
            }

            // 取得必要資料
            var content = storedResult.GetComponentList();
            var imageUrl = storedResult.ImageUrl;
            var result = new List<string>();

            // 如果已有 Architecture 細節，直接返回
            if (!string.IsNullOrEmpty(storedResult.ArchitectureDetail))
            {
                result.Add(Markdown.ToHtml(storedResult.ArchitectureDetail));
            }
            // 如果沒有 Architecture 細節但有 content，就呼叫 API 取得
            else if (content.Any())
            {
                var input = string.Join(", ", content);
                var response = await _httpClient.GetAsync($"{_rootUrl}/api/Analyze/ArchitectureDetail/{input}");

                if (response.IsSuccessStatusCode)
                {
                    var markdown = await response.Content.ReadAsStringAsync();
                    result.Add(Markdown.ToHtml(markdown));

                    // 儲存新的 Architecture 細節
                    await _tableStorageService.UpsertAnalysisResultAsync(id.ToString(), new Dictionary<string, object>
                    {
                        { "ArchitectureDetail", markdown }
                    });
                }
            }

            // 傳遞資料給 view
            ViewBag.imageUrl = imageUrl;
            ViewBag.content = content;
            ViewBag.result = result;
            ViewBag.id = id;

            return View();
        }

        /// <summary>
        /// Generates templates based on the provided targets and type.
        /// </summary>
        /// <param name="id">The GUID of the analysis result.</param>
        /// <param name="type">Type of template generation (single or multiple).</param>
        /// <returns>View with generated templates.</returns>
        [HttpGet]
        public async Task<IActionResult> Template(Guid? id, string type)
        {
            // 檢查必要參數
            if (!id.HasValue)
                return RedirectToAction("Index");

            // 從 Table Storage 取得儲存的資料
            var storedResult = await _tableStorageService.GetAnalysisResultAsync(id.Value.ToString());
            if (storedResult == null)
                return RedirectToAction("Index");

            // 從 Table Storage 取得 target 和 imageUrl
            var targetList = storedResult.GetComponentList();
            var imageUrl = storedResult.ImageUrl;

            if (targetList == null || !targetList.Any())
                return BadRequest("No target specified.");

            ViewBag.id = id;

            // 驗證模板生成類型
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

            TemplateModel resultTemplate;            // 如果是 multiple 類型，先嘗試從 Table Storage 讀取現有模板
            if (type?.ToLower() == "multiple" &&
                !string.IsNullOrEmpty(storedResult.BicepTemplate) &&
                !string.IsNullOrEmpty(storedResult.ArmTemplate))
            {
                resultTemplate = new TemplateModel
                {
                    BicepTemplate = storedResult.BicepTemplate,
                    ArmTemplate = storedResult.ArmTemplate
                };
                ViewBag.templateModel = resultTemplate;
                ViewBag.target = targetList;
                // 從 Table Storage 讀取 URLs
                ViewBag.armUrl = storedResult.ArmUrl ?? string.Empty;
                ViewBag.zipUrl = storedResult.ZipUrl ?? string.Empty;
                return View();
            }

            // 呼叫 API 取得新的模板
            var input = string.Join(",", targetList);
            var response = await _httpClient.GetAsync($"{_rootUrl}/api/Analyze/Templates/{input}");
            if (!response.IsSuccessStatusCode)
                return BadRequest("Failed to generate templates.");

            var jsonContent = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var templateModels = JsonSerializer.Deserialize<List<TemplateModel>>(jsonContent, options);
            resultTemplate = templateModels?.FirstOrDefault();
            if (resultTemplate == null)
                return BadRequest("No template generated.");            // 建立相關檔案
            var (armUrl, zipUrl) = await GenerateTemplateFiles(id.Value, resultTemplate, targetList, imageUrl);

            // 儲存模板和URLs到 Table Storage
            var updateData = new Dictionary<string, object>
            {
                { "ArmUrl", armUrl },
                { "ZipUrl", zipUrl }
            };

            // 如果是 multiple 類型，也儲存模板內容
            if (type?.ToLower() == "multiple")
            {
                updateData.Add("BicepTemplate", resultTemplate.BicepTemplate);
                updateData.Add("ArmTemplate", resultTemplate.ArmTemplate);
            }

            await _tableStorageService.UpsertAnalysisResultAsync(id.Value.ToString(), updateData);

            // 傳遞資料給 view
            ViewBag.templateModel = resultTemplate;
            ViewBag.target = targetList;
            ViewBag.armUrl = armUrl;
            ViewBag.zipUrl = zipUrl;
            return View();
        }

        private async Task<(string armUrl, string zipUrl)> GenerateTemplateFiles(Guid id, TemplateModel templateModel, List<string> targetList, string imageUrl)
        {
            string armUrl = string.Empty;
            string zipUrl = string.Empty;

            // 準備臨時檔案路徑
            string armFileName = "azuredeploy.json";
            string bicepFileName = "azuredeploy.bicep";
            string templateFileName = "template.json";
            var tempArmFilePath = Path.Combine(Path.GetTempPath(), armFileName);
            var tempBicepFilePath = Path.Combine(Path.GetTempPath(), bicepFileName);
            var tempTemplateFilePath = Path.Combine(Path.GetTempPath(), templateFileName);

            // 建立 ARM template 檔案
            using (var armStream = new MemoryStream())
            using (var armWriter = new StreamWriter(armStream))
            {
                armWriter.Write(templateModel.ArmTemplate);
                armWriter.Flush();
                armStream.Position = 0;

                // 儲存到本地臨時檔案
                using (var fileStream = new FileStream(tempArmFilePath, FileMode.Create))
                    await armStream.CopyToAsync(fileStream);

                // 儲存到 blob storage
                armStream.Position = 0;
                await _storageProvider.SaveBlobStreamAsync("arm-templates", $"{id}.json", armStream);
                armUrl = _storageProvider.GetBlobSasUrl("arm-templates", $"{id}.json", DateTimeOffset.Now.AddDays(1));
            }

            // 建立 Bicep template 檔案
            using (var bicepStream = new MemoryStream())
            using (var bicepWriter = new StreamWriter(bicepStream))
            {
                bicepWriter.Write(templateModel.BicepTemplate);
                bicepWriter.Flush();
                bicepStream.Position = 0;

                using (var fileStream = new FileStream(tempBicepFilePath, FileMode.Create))
                    await bicepStream.CopyToAsync(fileStream);
            }

            // 建立 Demo template 檔案
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

            // 建立並儲存 zip 檔案
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

            return (armUrl, zipUrl);
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
