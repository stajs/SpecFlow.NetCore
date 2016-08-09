using Microsoft.AspNet.Mvc;

namespace Sample.Website.Controllers
{
	public class HomeController : Controller
	{
		public const string Dnx = "dnx46 1.0.0-rc1-final";

		public IActionResult Index()
		{
			return View((object)Dnx);
		}

		public IActionResult Version()
		{
			return Content(Dnx);
		}

		public IActionResult Echo(string s)
		{
			return Content(s);
		}
	}
}