using System.Text.Json;
using Ci.Extension.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Polly;

namespace ScribbleOpeAIAnalysis.Apis
{
    /// <summary>
    /// Provides endpoints to analyze images using Azure OpenAI service and manage Azure resource deployments.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AnalyzeController : ControllerBase
    {
        // Service for handling Azure OpenAI chat completions
        private readonly IChatCompletionService _chatService;

        /// <summary>
        /// Initializes a new instance of the <see cref="AnalyzeController"/> class.
        /// Configures the Azure OpenAI chat completion service.
        /// </summary>
        /// <param name="configuration">The application configuration properties.</param>
        public AnalyzeController(IConfiguration configuration)
        {
            // Retrieve Azure OpenAI configuration values
            var deploymentName = configuration.GetValue<string>("Azure:OpenAI:DeploymentName");
            var endpoint = configuration.GetValue<string>("Azure:OpenAI:Endpoint");
            var apiKey = configuration.GetValue<string>("Azure:OpenAI:ApiKey");

            // Build the Semantic Kernel with Azure OpenAI chat completion
            var builder = Kernel.CreateBuilder();
            builder.AddAzureOpenAIChatCompletion(deploymentName, endpoint, apiKey);
            var kernel = builder.Build();

            // Initialize the chat completion service
            _chatService = kernel.GetRequiredService<IChatCompletionService>();
        }

        /// <summary>
        /// Analyzes an image blob using Azure OpenAI and returns a description of detected Azure resources.
        /// </summary>
        /// <param name="url">BLOB URL to be analyzed.</param>
        /// <returns>An <see cref="IActionResult"/> containing the analysis description.</returns>
        [HttpGet("AnalyzeImage")]
        public async Task<IActionResult> AnalyzeImage([FromQuery] string url)
        {
            // Validate the input URL
            if (url.IsNullOrWhiteSpace())
                return BadRequest("Blob URL is required.");

            // Create an image content object with the provided URL
            var imageContent = new ImageContent
            {
                Uri = new Uri(url)
            };

            // Initialize chat history for the Azure OpenAI service
            var history = new ChatHistory();
            history.AddSystemMessage("You are an AI assistant that helps an Azure DevOps engineer understand an image that likely shows Azure resources like VMs, SQL, storage, and web apps. Please identify a list of Azure resources from the image and any connections. Use commas to separate each entity.");

            // Add user message with the image content
            var collectionItems = new ChatMessageContentItemCollection
            {
                new TextContent("What's in the image?"),
                imageContent
            };
            history.AddUserMessage(collectionItems);

            // Get the chat response from Azure OpenAI
            var result = await _chatService.GetChatMessageContentsAsync(history);

            // Return the response with the description
            return Ok(new { Description = result[^1].Content });
        }

        /// <summary>
        /// Provides an architecture blurb based on the given Azure resource names.
        /// </summary>
        /// <param name="resourceNames">The Azure resource names.</param>
        /// <returns>An <see cref="IActionResult"/> with the architecture description.</returns>
        [HttpGet("ArchitectureDetail/{resourceNames}")]
        public async Task<IActionResult> ArchitectureDetail(string resourceNames)
        {
            try
            {
                // Initialize chat history for the Azure OpenAI service
                var history = new ChatHistory();
                history.AddSystemMessage("You are an IT Architect who helps students and IT professionals with different architectures and deployments using Azure resources like VMs, SQL, storage, and web apps. Provide a brief understanding of each resource, followed by a write-up on different architectures referring to 'Microsoft Learn' and 'Microsoft Architecture Center'. Include citations and links. Use concise formatting with bullet points where needed.");

                // Add user message with the resource names
                var collectionItems = new ChatMessageContentItemCollection
                {
                    new TextContent("Please give a good understanding on architecture with " + resourceNames)
                };
                history.AddUserMessage(collectionItems);

                // Get the chat response from Azure OpenAI
                var result = await _chatService.GetChatMessageContentsAsync(history);

                // Return the response with the architecture description
                return Ok(result[^1].Content);
            }
            catch (Exception ex)
            {
                // Handle exceptions and return a 500 status code
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
        /// 1. A production-ready Bicep template.
        /// 2. The equivalent ARM (JSON) template.
        /// Both templates follow Azure best practices and include all necessary dependencies.
        /// </remarks>
        [HttpGet("Templates/{resourceNames}")]
        public async Task<IActionResult> Templates(string resourceNames)
        {
            try
            {
                // Define a retry policy with Polly to handle transient failures
                var retryPolicy = Policy
                    .Handle<Exception>()
                    .OrResult<string>(result => !IsValidJson(result))
                    .RetryAsync(2);

                string finalResult = string.Empty;

                // Initialize chat history for the Azure OpenAI service
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
                        {
                            ""name"": ""A short suitable custom ArchitectureName"",
                            ""description"": ""A suitable custom Architecture description"",
                            ""bicepTemplate"": ""BicepTemplateContent"",
                            ""armTemplate"": ""ArmTemplateContent""
                        }
                    ]");

                // Add user message with the resource names
                var collectionItems = new ChatMessageContentItemCollection
                {
                    new TextContent("Please provide a template for: " + resourceNames)
                };
                history.AddUserMessage(collectionItems);

                // Execute the retry policy to get the chat response
                await retryPolicy.ExecuteAsync(async () =>
                {
                    var result = await _chatService.GetChatMessageContentsAsync(history);
                    finalResult = result[^1].Content;

                    return finalResult;
                });

                // Return the generated templates
                return Ok(finalResult);
            }
            catch (Exception ex)
            {
                // Handle exceptions and return a 500 status code
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if a string is valid JSON.
        /// </summary>
        /// <param name="jsonString">The string to validate.</param>
        /// <returns>True if the string is valid JSON, false otherwise.</returns>
        private bool IsValidJson(string jsonString)
        {
            // Return false if the string is null or empty
            if (string.IsNullOrWhiteSpace(jsonString))
                return false;

            try
            {
                // Attempt to parse the string as JSON
                using (JsonDocument.Parse(jsonString))
                {
                    return true;
                }
            }
            catch (JsonException)
            {
                // Return false if parsing fails
                return false;
            }
        }
    }
}

