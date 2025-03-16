using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace ScribbleOpeAIAnalysis.Services
{
    public class FeedbackService
    {
        private readonly string _feedbackFilePath = "feedback.json";

        public async Task SubmitFeedbackAsync(string feedbackType, string feedbackText)
        {
            var feedback = new Feedback
            {
                Id = Guid.NewGuid(),
                Type = feedbackType,
                Text = feedbackText,
                Timestamp = DateTime.UtcNow
            };

            var feedbackList = await GetFeedbackListAsync();
            feedbackList.Add(feedback);

            var json = JsonSerializer.Serialize(feedbackList, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_feedbackFilePath, json);
        }

        public async Task<List<Feedback>> GetFeedbackListAsync()
        {
            if (!File.Exists(_feedbackFilePath))
            {
                return new List<Feedback>();
            }

            var json = await File.ReadAllTextAsync(_feedbackFilePath);
            return JsonSerializer.Deserialize<List<Feedback>>(json);
        }
    }

    public class Feedback
    {
        public Guid Id { get; set; }
        public string Type { get; set; }
        public string Text { get; set; }
        public DateTime Timestamp { get; set; }
    }
}

