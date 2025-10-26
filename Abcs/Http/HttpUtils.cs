namespace Abcs.Http;

using System;
using System.Collections;
using System.Net;
using System.Text;
using System.IO;

public static class HttpUtils
{
	public static Hashtable? ParseUrlParams(string reqPath, string routePath)
	{
		string[] reqParts = reqPath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
		string[] routeParts = routePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);

		if (reqParts.Length != routeParts.Length)
		{
			return null;
		}

		Hashtable parameters = new Hashtable();

		for (int i = 0; i < routeParts.Length; i++)
		{
			string reqPart = reqParts[i];
			string routePart = routeParts[i];

			if (routePart.StartsWith(":"))
			{
				string paramName = routePart.Substring(1);
				parameters[paramName] = WebUtility.UrlDecode(reqPart); // RFC 3986 style (%20 for space, not +)
			}
			else if (reqPart != routePart)
			{
				return null;
			}
		}

		return parameters;
	}

	public static string DetectContentType(string input)
	{
		string s = input.TrimStart();

		if (s.StartsWith("{") || s.StartsWith("["))
		{
			return "application/json";
		}
		else if (s.StartsWith("<!DOCTYPE html", StringComparison.OrdinalIgnoreCase) || s.StartsWith("<html", StringComparison.OrdinalIgnoreCase))
		{
			return "text/html";
		}
		else if (s.StartsWith("<", StringComparison.Ordinal))
		{
			return "application/xml";
		}
		else
		{
			return "text/plain";
		}
	}

	public static async Task SendOkResponse(HttpListenerRequest req, HttpListenerResponse res, Hashtable props)
	{
		await SendOkResponse(req, res, props, string.Empty, "text/plain");
	}

	public static async Task SendOkResponse(HttpListenerRequest req, HttpListenerResponse res, Hashtable props, string content)
	{
		await SendOkResponse(req, res, props, content, DetectContentType(content));
	}

	public static async Task SendOkResponse(HttpListenerRequest req, HttpListenerResponse res, Hashtable props, string content, string contentType)
	{
		await Respond(req, res, props, (int)HttpStatusCode.OK, "OK", content, contentType);
	}

	public static async Task SendNotFoundResponse(HttpListenerRequest req, HttpListenerResponse res, Hashtable props)
	{
		await SendNotFoundResponse(req, res, props, string.Empty, "text/plain");
	}

	public static async Task SendNotFoundResponse(HttpListenerRequest req, HttpListenerResponse res, Hashtable props, string content)
	{
		await SendNotFoundResponse(req, res, props, content, DetectContentType(content));
	}

	public static async Task SendNotFoundResponse(HttpListenerRequest req, HttpListenerResponse res, Hashtable props, string content, string contentType)
	{
		await Respond(req, res, props, (int)HttpStatusCode.NotFound, "Not Found", content, contentType);
	}

	public static async Task Respond(HttpListenerRequest req, HttpListenerResponse res, Hashtable props, int statusCode, string statusDescription, string content)
	{
		await Respond(req, res, props, statusCode, statusDescription, content, DetectContentType(content));
	}

	public static async Task Respond(HttpListenerRequest req, HttpListenerResponse res, Hashtable props, int statusCode, string statusDescription, string content, string contentType)
	{
		byte[] contentBytes = Encoding.UTF8.GetBytes(content);
		res.StatusCode = statusCode;
		res.StatusDescription = statusDescription;
		res.ContentEncoding = Encoding.UTF8;
		res.ContentType = contentType;
		res.ContentLength64 = contentBytes.LongLength;
		await res.OutputStream.WriteAsync(contentBytes);
		res.Close();
	}
}
