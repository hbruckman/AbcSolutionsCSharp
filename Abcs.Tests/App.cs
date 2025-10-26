using System.Collections;
using System.Net;
using System.Threading.Tasks;
using Abcs.Http;

namespace Abcs.Tests;

public class App
{
	private HttpRouter router;
	private HttpListener server;

	public App()
	{
		router = new HttpRouter();
		router.UseRouteMatching();
		router.MapGet("/", AuthController.LandingPageGet);
		router.UseRouter("/auth", new AuthRouter());

		string host = "http://localhost:8080/";
		server = new HttpListener();
		server.Prefixes.Add(host);
		Console.WriteLine("Server started at " + host);
	}

	public async Task Start()
	{
		server.Start();

		while (server.IsListening)
		{
			HttpListenerContext ctx = await server.GetContextAsync();

			_ = router.HandleContextAsync(ctx);
		}
	}

	public void Stop()
	{
		if (server.IsListening)
		{
			server.Stop();
			server.Close();
			Console.WriteLine("Server stopped.");
		}
	}
}
