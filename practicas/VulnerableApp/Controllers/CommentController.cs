using Microsoft.AspNetCore.Mvc;
using VulnerableApp.Services;

namespace VulnerableApp.Controllers
{
    public class CommentController : InstrumentedController<CommentController>
    {
        private readonly ICommentStore _commentStore;

        public CommentController(
            ICommentStore commentStore,
            ILogger<CommentController> logger) : base(logger)
        {
            _commentStore = commentStore;
        }

        public IActionResult Index()
        {
            return ExecuteLogged(nameof(Index), safeParameters: null,
                () => View(_commentStore.GetAll()));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddComment(string comment)
        {
            return ExecuteLogged(
                nameof(AddComment),
                new { CommentLength = comment?.Length ?? 0 },
                () =>
                {
                    if (string.IsNullOrWhiteSpace(comment))
                    {
                        Logger.LogWarning("Se rechazo un comentario vacio");
                    }
                    else
                    {
                        _commentStore.Add(comment);
                        Logger.LogInformation(
                            "Comentario almacenado con longitud {CommentLength}",
                            comment.Length);
                    }

                    return RedirectToAction(nameof(Index));
                });
        }
    }
}
