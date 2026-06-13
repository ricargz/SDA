using Microsoft.AspNetCore.Mvc;
using VulnerableApp.Data;
using VulnerableApp.Models;

namespace VulnerableApp.Controllers
{
    public class SearchController : Controller
    {
        private readonly AppDbContext _db;

        public SearchController(AppDbContext db)
        {
            _db = db;
        }

        public IActionResult Index(string search)
        {
            if (string.IsNullOrWhiteSpace(search))
            {
                return View(new List<User>());
            }

            var normalizedSearch = search.Trim();
            var users = _db.Users
                .Where(u => u.Username.Contains(normalizedSearch))
                .ToList();

            return View(users);
        }
    }
}
