using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using System;
using System.IO;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;
using Azure;

namespace ScribbleOpeAIAnalysis.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImageController : ControllerBase
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _blobContainerName;
        private readonly IChatCompletionService _chatService;
        private readonly string _subscriptionId;
        private readonly ArmClient _armClient;

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

        [HttpGet("{blobName}")]
        public async Task<IActionResult> GetImageAnalysis(string blobName)
        {
            try
            {
                string bloburl = "https://strdiptest.blob.core.windows.net/imageforcode/" + blobName;
                var imageContent = new ImageContent();
                imageContent.Uri = new Uri(bloburl);
                // Call Azure OpenAI

                var history = new ChatHistory();
                history.AddSystemMessage("You are an AI assistant that helps an Azure Devops engineer understand an image that likely shows a Azure resources like VMs, sql, storage and webapps etc. please identify a list of azure resources from the image and if there are any connections. In the response just pass resources that you think are in the image, for example if you see a VM say VM , if you see sql say sql, if you see storage say storage and so on.");

                var collectionItems = new ChatMessageContentItemCollection
                {
                    new TextContent("What's in the image?"),
                    imageContent
                };

                history.AddUserMessage(collectionItems);

                //history.AddUserMessage(new ImageContent { Uri = new Uri(blobClient.Uri.ToString()) });

                var result = await _chatService.GetChatMessageContentsAsync(history);

                // Return the response
                return Ok(new { Description = "Image description:" + result[^1].Content });
                }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        [HttpGet(Name = "GetArchitectureBlurb")]
        public async Task<IActionResult> GetArchitectureBlurb(string ResourceNames)
        {
            try
            {
                var history = new ChatHistory();
                history.AddSystemMessage("You are an IT Architect who helps Students and IT professionals which different architectures and deployments with different kinds of Azure resources like VMs, sql, storage and webapps etc. Please provide brief understanding of each resource first. Followed by a write up om different architectures that can be done referring \"Microsoft learn\" and \"Microsoft architecture center\". Lastly also provided different citations and links. Keep it concise and good formatting, bullet points where you need to.");

                var collectionItems = new ChatMessageContentItemCollection
                {
                    new TextContent("please give a good understanding on Architecture with " + ResourceNames)
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
        [HttpPost("deploy")]
        public async Task<IActionResult> DeployResource([FromBody] string resourceType)
        {
            try
            {
                string templateFilePath = string.Empty;
                string parametersFilePath = string.Empty;

                switch (resourceType.ToLower())
                {
                    case "vm":
                        templateFilePath = Path.Combine(".", "Assets", "VmTemplate.json");
                        parametersFilePath = Path.Combine(".", "Assets", "VmParameters.json");
                        break;
                    case "sql":
                        templateFilePath = Path.Combine(".", "Assets", "SqlTemplate.json");
                        parametersFilePath = Path.Combine(".", "Assets", "SqlParameters.json");
                        break;
                    case "storage":
                        templateFilePath = Path.Combine(".", "Assets", "StorageTemplate.json");
                        //parametersFilePath = Path.Combine(".", "Assets", "StorageParameters.json");
                        break;
                    case "webapp":
                        templateFilePath = Path.Combine(".", "Assets", "WebAppTemplate.json");
                       // parametersFilePath = Path.Combine(".", "Assets", "WebAppParameters.json");
                        break;
                    default:
                        return BadRequest("Unsupported resource type.");
                }
                var templateContent = System.IO.File.ReadAllText(templateFilePath).TrimEnd();
               // var parametersContent = System.IO.File.ReadAllText(parametersFilePath).TrimEnd();

                var resourceGroupName = "azScribbletoAzure";
                var deploymentName = $"{resourceType}-deployment";

                var resourceGroup = await _armClient.GetDefaultSubscriptionAsync().Result.GetResourceGroups().GetAsync(resourceGroupName);
                var deployment = new ArmDeploymentContent(new ArmDeploymentProperties(ArmDeploymentMode.Incremental)
                { 
                    Template = BinaryData.FromString(templateContent)
                   // Parameters = BinaryData.FromString(parametersContent)
                });


                var deploymentOperation = await resourceGroup.Value.GetArmDeployments().CreateOrUpdateAsync(WaitUntil.Completed, deploymentName, deployment);
                return Ok($"Deployment status: {deploymentOperation.Value.Data.Properties.ProvisioningState}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }


}

