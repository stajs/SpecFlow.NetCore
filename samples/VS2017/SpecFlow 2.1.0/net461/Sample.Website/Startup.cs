using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Sample.Website
{
	public class Startup
	{
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddMvc();
		}

		public void Configure(IApplicationBuilder app)
		{
			app.UseMvcWithDefaultRoute();
		}

		public static void Main(string[] args)
		{
			var host = new WebHostBuilder()
				.UseStartup<Startup>()
				.UseKestrel()
				.Build();

			host.Run();
		}
	}
}