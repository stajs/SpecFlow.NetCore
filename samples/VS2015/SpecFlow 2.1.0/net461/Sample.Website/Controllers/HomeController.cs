using Microsoft.AspNetCore.Mvc;

namespace Sample.Website.Controllers
{
	public class HomeController : Controller
	{
		public const string AppVersion = "1.33.7";

		public IActionResult Index()
		{
			return View((object)AppVersion);
		}

		public IActionResult Version()
		{
			return Content(AppVersion);
		}

		public IActionResult Echo(string s)
		{
			return Content(s);
		}
	}
}