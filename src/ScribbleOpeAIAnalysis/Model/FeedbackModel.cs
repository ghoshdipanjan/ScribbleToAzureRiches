namespace ScribbleOpeAIAnalysis.Model
{
    /// <summary>
    /// Represents a feedback entry submitted by a user.
    /// </summary>
    public class FeedbackModel
    {
        /// <summary>
        /// Unique identifier for the feedback.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The type of feedback (e.g., suggestion, issue).
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// The text content of the feedback.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// The timestamp when the feedback was submitted.
        /// </summary>
        public DateTime Timestamp { get; set; }
    }
}
