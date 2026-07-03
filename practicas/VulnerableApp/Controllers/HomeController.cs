using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using VulnerableApp.Models;

namespace VulnerableApp.Controllers;

public class HomeController : InstrumentedController<HomeController>
{
    public HomeController(ILogger<HomeController> logger) : base(logger)
    {
    }

    public IActionResult Index()
    {
        return ExecuteLogged(nameof(Index), safeParameters: null, View);
    }

    public IActionResult Privacy()
    {
        return ExecuteLogged(nameof(Privacy), safeParameters: null, View);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return ExecuteLogged(nameof(Error), safeParameters: null, () =>
        {
            var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            Logger.LogError("Se mostro la pagina de error para la solicitud {RequestId}", requestId);
            return View(new ErrorViewModel { RequestId = requestId });
        });
    }
}
