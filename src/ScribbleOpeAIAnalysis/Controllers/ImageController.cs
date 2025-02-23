using Azure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using Ci.Extension.Core;

namespace ScribbleOpeAIAnalysis.Controllers
{
    /// <summary>
    /// Provides endpoints to analyze images using Azure OpenAI service and manage Azure resource deployments.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ImageController : ControllerBase
    {
        private readonly IChatCompletionService _chatService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageController"/> class.
        /// Configures the Azure OpenAI chat completion service.
        /// </summary>
        /// <param name="configuration">The application configuration properties.</param>
        public ImageController(IConfiguration configuration)
        {
            var deploymentName = configuration["Azure:OpenAI:DeploymentName"];
            var endpoint = configuration["Azure:OpenAI:Endpoint"];
            var apiKey = configuration["Azure:OpenAI:ApiKey"];

            var builder = Kernel.CreateBuilder();
            builder.AddAzureOpenAIChatCompletion(deploymentName, endpoint, apiKey);
            var kernel = builder.Build();
            _chatService = kernel.GetRequiredService<IChatCompletionService>();
        }

        /// <summary>
        /// Analyzes an image blob using Azure OpenAI and returns a description of detected Azure resources.
        /// </summary>
        /// <param name="url">BLOB url to be analyzed.</param>
        /// <returns>An <see cref="IActionResult"/> containing the analysis description.</returns>
        [HttpGet("AnalysisImage")]
        public async Task<IActionResult> GetImageAnalysis([FromQuery] string url)
        {
            try
            {
                // string bloburl = "https://s33046.pcdn.co/wp-content/uploads/2022/03/architecture-overview.png";
                //string bloburl = "https://strdiptest.blob.core.windows.net/images/Whiteboard 234.png";
                if (url.IsNullOrWhiteSpace())
                    return BadRequest("Blob URL is required.");

                var imageContent = new ImageContent();
                imageContent.Uri = new Uri(url);
                // Call Azure OpenAI

                var history = new ChatHistory();
                history.AddSystemMessage("You are an AI assistant that helps an Azure Devops engineer understand an image that likely shows a Azure resources like VMs, sql, storage and webapps etc. please identify a list of azure resources from the image and if there are any connections. In the response just pass resources that you think are in the image, for example if you see a VM say VM , if you see sql say sql, if you see storage say storage and so on. Use comma to separate each entity.");

                var collectionItems = new ChatMessageContentItemCollection
                    {
                        new TextContent("What's in the image?"),
                        imageContent
                    };

                history.AddUserMessage(collectionItems);

                //history.AddUserMessage(new ImageContent { Uri = new Uri(blobClient.Uri.ToString()) });

                var result = await _chatService.GetChatMessageContentsAsync(history);

                // Return the response
                return Ok(new { Description = result[^1].Content });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Provides an architecture blurb based on the given Azure resource names.
        /// </summary>
        /// <param name="resourceNames">The Azure resource names.</param>
        /// <returns>An <see cref="IActionResult"/> with the architecture description.</returns>
        [HttpGet("GetArchitectureBlurb/{resourceNames}")]
        public async Task<IActionResult> GetArchitectureBlurb(string resourceNames)
        {
            try
            {
                var history = new ChatHistory();
                history.AddSystemMessage("You are an IT Architect who helps Students and IT professionals which different architectures and deployments with different kinds of Azure resources like VMs, sql, storage and webapps etc. Please provide brief understanding of each resource first. Followed by a write up om different architectures that can be done referring \"Microsoft learn\" and \"Microsoft architecture center\". Lastly also provided different citations and links. Keep it concise and good formatting, bullet points where you need to. And do not output any message beside the content.");

                var collectionItems = new ChatMessageContentItemCollection
                    {
                        new TextContent("please give a good understanding on Architecture with " + resourceNames)
                    };

                history.AddUserMessage(collectionItems);

                //history.AddUserMessage(new ImageContent { Uri = new Uri(blobClient.Uri.ToString()) });

                var result = await _chatService.GetChatMessageContentsAsync(history);

                // Return the response
                return Ok(result[^1].Content);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Retrieves ARM templates for specified Azure resources.
        /// </summary>
        /// <param name="resourceNames">The Azure resource names.</param>
        /// <returns>An <see cref="IActionResult"/> containing the ARM template in markdown format.</returns>
        [HttpGet("GetArmTemplates/{resourceNames}")]
        public async Task<IActionResult> GetTemplates(string resourceNames)
        {
            try
            {
                var history = new ChatHistory();
                history.AddSystemMessage("You are an Azure deployment expert, as you are asked about different resources you can let them know what the best bicep templates to use. Please use markdown as output format.");

                var collectionItems = new ChatMessageContentItemCollection
                    {
                        new TextContent("What will be templates for: " + resourceNames)
                    };

                history.AddUserMessage(collectionItems);

                var result = await _chatService.GetChatMessageContentsAsync(history);

                return Ok(result[^1].Content);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Deploys an Azure resource using a specified ARM template.
        /// </summary>
        /// <param name="resourceName">The type of the Azure resource to be deployed.</param>
        /// <returns>An <see cref="IActionResult"/> indicating the deployment status.</returns>
        /// <remarks>This action deploys resources one at a time based on the provided resource name.</remarks>
        [HttpGet("DeployResource/{resourceName}")]
        public async Task<IActionResult> DeployResource(string resourceName)
        {
            try
            {
                string subscriptionId = "5c7b45b6-5d6b-4c47-839a-9c732c1b761e";
                string templateUrl = string.Empty;
                string parametersUrl = string.Empty;

                switch (resourceName.ToLower())
                {
                    //case "vm":
                    //    templateUrl = "https://example.com/templates/VmTemplate.json";
                    //    parametersUrl = "https://example.com/templates/VmParameters.json";
                    //    break;
                    //case "sql":
                    //    templateUrl = "https://example.com/templates/SqlTemplate.json";
                    //    parametersUrl = "https://example.com/templates/SqlParameters.json";
                    //    break;
                    case "sql":
                    case "storage":
                        templateUrl = "https://strdiptest.blob.core.windows.net/templates/StorageTemplate.json";
                        break;
                    case "webapp":
                    case "vm":
                        templateUrl = "https://strdiptest.blob.core.windows.net/templates/WebAppTemplate.json";
                        break;
                    default:
                        return BadRequest("Unsupported resource type.");
                }

                using (var httpClient = new HttpClient())
                {
                    var templateResponse = await httpClient.GetAsync(templateUrl);
                    if (!templateResponse.IsSuccessStatusCode)
                    {
                        return StatusCode((int)templateResponse.StatusCode, $"Failed to fetch template from URL: {templateUrl}");
                    }
                    var templateContent = await templateResponse.Content.ReadAsStringAsync();

                    // Optionally fetch parameters if needed
                    string parametersContent = string.Empty;
                    if (!string.IsNullOrEmpty(parametersUrl))
                    {
                        var parametersResponse = await httpClient.GetAsync(parametersUrl);
                        if (!parametersResponse.IsSuccessStatusCode)
                        {
                            return StatusCode((int)parametersResponse.StatusCode, $"Failed to fetch parameters from URL: {parametersUrl}");
                        }
                        parametersContent = await parametersResponse.Content.ReadAsStringAsync();
                    }

                    var resourceGroupName = "azScribbletoAzure";
                    var deploymentName = $"{resourceName}-deployment";

                    // Use DefaultAzureCredential to authenticate with managed identity
                    var credential = new DefaultAzureCredential();
                    var armClient = new ArmClient(credential, subscriptionId);

                    var resourceGroup = await armClient.GetDefaultSubscriptionAsync().Result.GetResourceGroups().GetAsync(resourceGroupName);
                    var deployment = new ArmDeploymentContent(new ArmDeploymentProperties(ArmDeploymentMode.Incremental)
                    {
                        Template = BinaryData.FromString(templateContent),
                        Parameters = !string.IsNullOrEmpty(parametersContent) ? BinaryData.FromString(parametersContent) : null
                    });

                    var deploymentOperation = await resourceGroup.Value.GetArmDeployments().CreateOrUpdateAsync(WaitUntil.Completed, deploymentName, deployment);

                    if (deploymentOperation.Value.Data.Properties.ProvisioningState == "Failed")
                    {
                        var operations = deploymentOperation.Value.GetDeploymentOperations().ToList();
                        foreach (var operation in operations)
                        {
                            if (operation.Properties.ProvisioningState == "Failed")
                            {
                                var errorDetails = operation.Properties.StatusMessage;
                                Console.WriteLine($"Operation {operation.OperationId} failed: {errorDetails}");
                            }
                        }
                        return StatusCode(500, "Internal server error: At least one resource deployment operation failed. Check logs for details.");
                    }

                    return Ok($"Deployment status: {deploymentOperation.Value.Data.Properties.ProvisioningState}");
                }
            }
            catch (Exception ex)
            {
                // Log the exception details
                Console.WriteLine($"Exception: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }


}

