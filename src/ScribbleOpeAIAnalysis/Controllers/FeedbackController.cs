using Microsoft.AspNetCore.Mvc;
using ScribbleOpeAIAnalysis.Services;

namespace ScribbleOpeAIAnalysis.Controllers
{
    /// <summary>
    /// Controller responsible for handling feedback-related operations.
    /// </summary>
    public class FeedbackController : Controller
    {
        /// <summary>
        /// Service for managing feedback operations.
        /// </summary>
        private readonly FeedbackService _feedbackService;

        /// <summary>
        /// Initializes a new instance of the <see cref="FeedbackController"/> class.
        /// </summary>
        /// <param name="feedbackService">The feedback service to handle feedback operations.</param>
        public FeedbackController(FeedbackService feedbackService)
        {
            _feedbackService = feedbackService;
        }

        /// <summary>
        /// Submits feedback provided by the user.
        /// </summary>
        /// <param name="feedbackType">The type of feedback (e.g., suggestion, issue).</param>
        /// <param name="feedbackText">The text content of the feedback.</param>
        /// <returns>A redirection to the home page after submission.</returns>
        [HttpPost]
        public async Task<IActionResult> Submit(string feedbackType, string feedbackText)
        {
            // Submit the feedback asynchronously using the feedback service.
            await _feedbackService.SubmitFeedbackAsync(feedbackType, feedbackText);
            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Retrieves a list of all submitted feedback for reporting purposes.
        /// </summary>
        /// <returns>A view displaying the list of feedback.</returns>
        [HttpGet]
        public async Task<IActionResult> Report()
        {
            // Fetch the list of feedback asynchronously using the feedback service.
            var feedbackList = await _feedbackService.GetFeedbackListAsync();
            return View(feedbackList);
        }
    }
}
