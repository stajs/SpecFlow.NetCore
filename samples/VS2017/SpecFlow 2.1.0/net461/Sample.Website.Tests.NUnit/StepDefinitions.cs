using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using Sample.Website.Controllers;
using TechTalk.SpecFlow;

namespace Sample.Website.Tests
{
	[Binding]
	public class StepDefinitions
	{
		[Given(@"I am curious")]
		public void GivenIAmCurious()
		{
			//ScenarioContext.Current.Pending();
		}

		[When(@"I request the version")]
		public void WhenIRequestTheVersion()
		{
			var controller = new HomeController();
			ScenarioContext.Current.Add("versionResult", controller.Version());
		}

		[When(@"I yell '(.*)'")]
		public void WhenIYell(string exclamation)
		{
			var controller = new HomeController();
			ScenarioContext.Current.Add("echoResult", controller.Echo(exclamation));
		}

		[Then(@"the result is content")]
		public void ThenTheResultIsContent()
		{
			var versionResult = ScenarioContext.Current["versionResult"];
			Assert.True(versionResult is ContentResult);
		}

		[Then(@"the result is constant")]
		public void ThenTheResultIsConstant()
		{
			var versionResult = (ContentResult) ScenarioContext.Current["versionResult"];
			Assert.AreEqual(versionResult.Content, HomeController.AppVersion);
		}

		[Then(@"I hear '(.*)' echoed back")]
		public void ThenIHearEchoedBack(string exclamation)
		{
			var echoResult = (ContentResult) ScenarioContext.Current["echoResult"];
			Assert.AreEqual(echoResult.Content, exclamation);
		}
	}
}