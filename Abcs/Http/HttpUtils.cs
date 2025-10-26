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

	public static async Task SendNotFoundResponse(HttpListenerRequest req, HttpListenerResponse res, Hashtable props)
	{
		await Respond(req, res, props, (int)HttpStatusCode.NotFound, "Not Found", "text/plain", "404 Not Found");
	}

	public static async Task Respond(HttpListenerRequest req, HttpListenerResponse res, Hashtable props, int statusCode, string statusDescription, string contentType, string body)
	{
		byte[] content = Encoding.UTF8.GetBytes(body);
		res.StatusCode = statusCode;
		res.StatusDescription = statusDescription;
		res.ContentEncoding = Encoding.UTF8;
		res.ContentType = contentType;
		res.ContentLength64 = content.LongLength;
		await res.OutputStream.WriteAsync(content);
		res.Close();
	}
}
