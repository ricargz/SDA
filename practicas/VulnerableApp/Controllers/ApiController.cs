using Microsoft.AspNetCore.Mvc;
using VulnerableApp.Data;

namespace VulnerableApp.Controllers
{
    [ApiController]
    [Route("api")]
    public class ApiController : ControllerBase
    {
        private readonly AppDbContext _db;

        public ApiController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("user/{id}")]
        public IActionResult GetUser(int id)
        {
            var currentUserId = HttpContext.Session.GetInt32("UserId");
            if (!currentUserId.HasValue)
            {
                return Unauthorized();
            }

            if (id != currentUserId.Value)
            {
                return Forbid();
            }

            var user = _db.Users.Find(id);
            if (user == null)
            {
                return NotFound();
            }

            return Ok(new
            {
                user.Id,
                user.Username,
                user.Email
            });
        }

        [HttpGet("users")]
        public IActionResult GetAllUsers()
        {
            var currentUserId = HttpContext.Session.GetInt32("UserId");
            if (!currentUserId.HasValue)
            {
                return Unauthorized();
            }

            var users = _db.Users
                .Select(user => new { user.Id, user.Username, user.Email })
                .ToList();

            return Ok(users);
        }
    }
}
