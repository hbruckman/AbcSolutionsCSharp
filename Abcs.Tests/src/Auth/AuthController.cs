using System.Collections;
using System.Net;
using Abcs.Http;

namespace Abcs.Tests;

public static class AuthController
{
	public static async Task LandingPageGet(HttpListenerRequest req, HttpListenerResponse res, Hashtable props, Func<Task> next)
	{
		await HttpUtils.SendOkResponse(req, res, props, "LandingPageGet");
	}

	public static async Task RegisterGet(HttpListenerRequest req, HttpListenerResponse res, Hashtable props, Func<Task> next)
	{
		await HttpUtils.SendOkResponse(req, res, props, "RegisterGet");
	}

	public static async Task RegisterPost(HttpListenerRequest req, HttpListenerResponse res, Hashtable props, Func<Task> next)
	{
		await HttpUtils.SendOkResponse(req, res, props, "RegisterPost");
	}

	public static async Task LoginGet(HttpListenerRequest req, HttpListenerResponse res, Hashtable props, Func<Task> next)
	{
		await HttpUtils.SendOkResponse(req, res, props, "LoginGet");
	}

	public static async Task LoginPost(HttpListenerRequest req, HttpListenerResponse res, Hashtable props, Func<Task> next)
	{
		await HttpUtils.SendOkResponse(req, res, props, "LoginPost");
	}

	public static async Task LogoutGet(HttpListenerRequest req, HttpListenerResponse res, Hashtable props, Func<Task> next)
	{
		await HttpUtils.SendOkResponse(req, res, props, "LogoutGet");
	}

	public static async Task LogoutPost(HttpListenerRequest req, HttpListenerResponse res, Hashtable props, Func<Task> next)
	{
		await HttpUtils.SendOkResponse(req, res, props, "LogoutPost");
	}
}
