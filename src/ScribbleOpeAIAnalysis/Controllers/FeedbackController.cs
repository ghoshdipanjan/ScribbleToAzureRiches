using Microsoft.AspNetCore.Mvc;
using ScribbleOpeAIAnalysis.Services;
using System.Threading.Tasks;

namespace ScribbleOpeAIAnalysis.Controllers
{
    public class FeedbackController : Controller
    {
        private readonly FeedbackService _feedbackService;

        public FeedbackController(FeedbackService feedbackService)
        {
            _feedbackService = feedbackService;
        }

        [HttpPost]
        public async Task<IActionResult> Submit(string feedbackType, string feedbackText)
        {
            await _feedbackService.SubmitFeedbackAsync(feedbackType, feedbackText);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> Report()
        {
            var feedbackList = await _feedbackService.GetFeedbackListAsync();
            return View(feedbackList);
        }
    }
}
