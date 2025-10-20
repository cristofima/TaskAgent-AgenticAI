using Microsoft.AspNetCore.Mvc;

namespace TaskAgent.WebApp.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => View();
}
