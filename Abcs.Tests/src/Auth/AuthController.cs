using System.Collections;
using System.Net;
using Abcs.Http;

namespace Abcs.Tests;

public static class AuthController
{
	public static async Task LandingPage(HttpListenerRequest req, HttpListenerResponse res, Hashtable props, Func<Task> next)
	{
		await HttpUtils.SendNotFoundResponse(req, res, props);
	}
}
