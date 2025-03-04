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
                history.AddSystemMessage("You are an AI assistant that helps an Azure Devops engineer understand an image that likely shows a Azure resources like VMs, sql, storage and webapps etc. please identify a list of azure resources from the image and if there are any connections. In the response just pass resources that you think are in the image, for example if you see a VM say VM , if you see sql say sql, if you see storage say storage and so on. Do not output resources with no relation to Azure cloud, like Jumpbox etc. Use comma to separate each entity.");

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
        /// Generates Bicep and ARM deployment templates for specified Azure resources.
        /// </summary>
        /// <param name="resourceNames">Comma-separated list of Azure resource names to include in the templates.</param>
        /// <returns>An <see cref="IActionResult"/> containing the generated Bicep and ARM templates as a JSON array.</returns>
        /// <remarks>
        /// The response is formatted as a JSON array with two elements:
        /// 1. A production-ready Bicep template 
        /// 2. The equivalent ARM (JSON) template
        /// Both templates follow Azure best practices and include all necessary dependencies.
        /// </remarks>

        [HttpGet("GetTemplates/{resourceNames}")]
        public async Task<IActionResult> GetTemplates(string resourceNames)
        {
            try
            {
                var history = new ChatHistory();
                history.AddSystemMessage(@"
                            You are an Azure deployment expert. I need a complete deployment template for the Azure resources I'll specify. IMPORTANT: Your response must be a JSON array with two elements:

                            1. A Bicep template with only the Bicep code, without any explanations, introductions, or conclusions. 
                            2. The equivalent ARM (JSON) template for the same resources.

                            Requirements for both templates:
                            - Production-ready and directly deployable
                            - Include appropriate parameters, variables, and outputs
                            - Follow Azure best practices
                            - Ignore any non-Azure resources mentioned
                            - Ensure all necessary dependencies between resources are properly configured

                            Provide the templates in this JSON array format:
                            [
                              ""Bicep template content here"",
                              ""ARM template JSON content here""
                            ]

                            Without ""```bicep"" and ""```"" or ""```json"" and ""```""");

                var collectionItems = new ChatMessageContentItemCollection
                    {
                        new TextContent("Please provide a template for : " + resourceNames)
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
    }
}

