using System.Text.Json;
using ScribbleOpeAIAnalysis.Models;

namespace ScribbleOpeAIAnalysis.Services
{
    /// <summary>
    /// Service for managing feedback operations, including storing and retrieving feedback.
    /// </summary>
    public class FeedbackService
    {
        /// <summary>
        /// Path to the file where feedback data is stored.
        /// </summary>
        private readonly string _feedbackFilePath = "feedback.json";

        /// <summary>
        /// Submits feedback by saving it to the feedback file.
        /// </summary>
        /// <param name="feedbackType">The type of feedback (e.g., suggestion, issue).</param>
        /// <param name="feedbackText">The text content of the feedback.</param>
        public async Task SubmitFeedbackAsync(string feedbackType, string feedbackText)
        {
            // Create a new feedback object with the provided details.
            var feedback = new FeedbackModel
            {
                Id = Guid.NewGuid(),
                Type = feedbackType,
                Text = feedbackText,
                Timestamp = DateTime.UtcNow
            };

            // Retrieve the existing feedback list and add the new feedback.
            var feedbackList = await GetFeedbackListAsync();
            feedbackList.Add(feedback);

            // Serialize the feedback list to JSON and save it to the file.
            var json = JsonSerializer.Serialize(feedbackList, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_feedbackFilePath, json);
        }

        /// <summary>
        /// Retrieves the list of all submitted feedback.
        /// </summary>
        /// <returns>A list of feedback objects.</returns>
        public async Task<List<FeedbackModel>> GetFeedbackListAsync()
        {
            // Check if the feedback file exists; if not, return an empty list.
            if (!File.Exists(_feedbackFilePath))
            {
                return new List<FeedbackModel>();
            }

            // Read the feedback file and deserialize its content into a list of feedback objects.
            var json = await File.ReadAllTextAsync(_feedbackFilePath);
            return JsonSerializer.Deserialize<List<FeedbackModel>>(json);
        }
    }

    
}

