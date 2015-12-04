using Microsoft.AspNet.Mvc;
using Sample.Website.Controllers;
using TechTalk.SpecFlow;
using Xunit;

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

		[Then(@"the result is content")]
		public void ThenTheResultIsContent()
		{
			var versionResult = ScenarioContext.Current["versionResult"];
			Assert.True(versionResult is ContentResult);
		}

		[Then(@"the result is constant")]
		public void ThenTheResultIsConstant()
		{
			var versionResult = ScenarioContext.Current["versionResult"] as ContentResult;
			Assert.Equal(versionResult.Content, HomeController.Dnx);
		}
	}
}