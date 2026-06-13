using Microsoft.AspNetCore.Mvc;
using VulnerableApp.Services;

namespace VulnerableApp.Controllers
{
    public class CommentController : Controller
    {
        private readonly ICommentStore _commentStore;

        public CommentController(ICommentStore commentStore)
        {
            _commentStore = commentStore;
        }

        public IActionResult Index()
        {
            return View(_commentStore.GetAll());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddComment(string comment)
        {
            if (!string.IsNullOrWhiteSpace(comment))
            {
                _commentStore.Add(comment);
            }

            return RedirectToAction("Index");
        }
    }
}
