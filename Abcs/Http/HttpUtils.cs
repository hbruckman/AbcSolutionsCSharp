namespace Abcs.Http;

using System;
using System.Collections;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Web;
using System.Xml.Linq;

public static class HttpUtils
{
	// https://john:secret@example.com:8080/api/v1/users?id=123&active=true#profile
	// protocol://user:pass@host:port/path?query#fragment

	public static Hashtable ParseUrl(string url)
	{
		int i = -1;

		var (scheme, apqf) = (i = url.IndexOf("://")) >= 0 ? (url.Substring(0, i), url.Substring(i + 3)) : ("", url);
		var (uphp, pqf) = (i = apqf.IndexOf("/")) >= 0 ? (apqf.Substring(0, i), apqf.Substring(i)) : (apqf, "");
		var (up, hp) = (i = uphp.IndexOf("@")) >= 0 ? (uphp.Substring(0, i), uphp.Substring(i + 1)) : ("", uphp);
		var (user, pass) = (i = up.IndexOf(":")) >= 0 ? (up.Substring(0, i), up.Substring(0, i + 1)) : (up, "");
		var (host, port) = (i = hp.IndexOf(":")) >= 0 ? (hp.Substring(0, i), up.Substring(0, i + 1)) : (hp, "");
		var (pq, fragment) = (i = pqf.IndexOf("#")) >= 0 ? (pqf.Substring(0, i), pqf.Substring(i + 1)) : (pqf, "");
		var (path, query) = (i = pq.IndexOf("?")) >= 0 ? (pq.Substring(0, i), pq.Substring(i + 1)) : (pq, "");
		var protocol = scheme.Substring(0, scheme.Length - 3);
		var parts = new Hashtable();

		parts["scheme"] = scheme;
		parts["protocol"] = protocol;
		parts["user"] = user;
		parts["pass"] = pass;
		parts["host"] = host;
		parts["port"] = port;
		parts["path"] = path;
		parts["query"] = query;
		parts["fragment"] = fragment;

		return parts;
	}

	public static Hashtable? ParseUrlParams(string uPath, string rPath)
	{
		string[] uParts = uPath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
		string[] rParts = rPath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);

		if (uParts.Length != rParts.Length) {	return null; }

		Hashtable parameters = new Hashtable();

		for (int i = 0; i < rParts.Length; i++)
		{
			string uPart = uParts[i];
			string rPart = rParts[i];

			if (rPart.StartsWith(":"))
			{
				string paramName = rPart.Substring(1);
				parameters[paramName] = HttpUtility.UrlDecode(uPart); // Decodes "+" -> " "
				//parameters[paramName] = WebUtility.UrlDecode(rPart); // Official but does not
			}
			else if (uPart != rPart)
			{
				return null;
			}
		}

		return parameters;
	}

	public static Hashtable ParseQueryString(string text, string duplicateSeparator = ",")
	{
		return ParseFormData(text, duplicateSeparator);
	}

	public static Hashtable ParseFormData(string text, string duplicateSeparator = ",")
	{
		var result = new Hashtable();
		var pairs = text.Split('&', StringSplitOptions.RemoveEmptyEntries);

		foreach (var pair in pairs)
		{
			var kv = pair.Split('=', 2, StringSplitOptions.None);
			var key = HttpUtility.UrlDecode(kv[0]);
			var value = kv.Length > 1 ? HttpUtility.UrlDecode(kv[1]) : string.Empty;
			var oldValue = result[key];
			result[key] = oldValue == null ? value : oldValue + duplicateSeparator + value;
		}

		return result;
	}

	public static async Task CentralizedErrorHandling(HttpListenerRequest req, HttpListenerResponse res, Hashtable props, Func<Task> next)
	{
		Console.WriteLine("CentralizedErrorHandling middleware is running.");

		try
		{
			await next();
		}
		catch (Exception e)
		{
			await SendResponse(req, res, props, (int)HttpStatusCode.InternalServerError, e.ToString(), "text/plain");
		}
		finally
		{

		}
	}

	public static async Task ParseRequestUrl(HttpListenerRequest req, HttpListenerResponse res, Hashtable props, Func<Task> next)
	{
		props["url"] = ParseUrl(req.RawUrl!);

		await next();
	}

	public static async Task ReadRequestBodyAsBytes(HttpListenerRequest req, HttpListenerResponse res, Hashtable props, Func<Task> next)
	{
		using var ms = new MemoryStream();
		await req.InputStream.CopyToAsync(ms);
		props["req.body"] = ms.ToArray();

		await next();
	}

	public static async Task ReadRequestBodyAsText(HttpListenerRequest req, HttpListenerResponse res, Hashtable props, Func<Task> next)
	{
		Console.WriteLine("ReadRequestBodyAsText middleware is running.");

		using StreamReader sr = new StreamReader(req.InputStream, Encoding.UTF8);
		props["req.text"] = await sr.ReadToEndAsync();

		await next();
	}

	public static async Task ReadRequestBodyAsForm(HttpListenerRequest req, HttpListenerResponse res, Hashtable props, Func<Task> next)
	{
		using StreamReader sr = new StreamReader(req.InputStream, Encoding.UTF8);
		string formData = await sr.ReadToEndAsync();
		props["req.form"] = ParseFormData(formData);

		await next();
	}

	public static async Task ReadRequestBodyAsJson(HttpListenerRequest req, HttpListenerResponse res, Hashtable props, Func<Task> next)
	{
		props["req.json"] = await JsonDocument.ParseAsync(req.InputStream);

		await next();
	}

	public static async Task ReadRequestBodyAsXml(HttpListenerRequest req, HttpListenerResponse res, Hashtable props, Func<Task> next)
	{
		props["req.xml"] = await XDocument.LoadAsync(req.InputStream, LoadOptions.None, CancellationToken.None);

		await next();
	}

	public static string DetectContentType(string text)
	{
		string s = text.TrimStart();

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

	public static void AddPaginationHeaders<T>(HttpListenerRequest req, HttpListenerResponse res, Hashtable props, PagedResult<T> pagedResult, int page, int size)
	{
		var baseUrl = $"{req.Url!.Scheme}://{req.Url!.Authority}{req.Url!.AbsolutePath}";
		int totalPages = Math.Max(1, (int) Math.Ceiling((double) pagedResult.TotalCount / size));

		string self = $"{baseUrl}?page={page}&size={size}";
		string? first = page == 1 ? null : $"{baseUrl}?page={1}&size={size}";
		string? last = page == totalPages ? null : $"{baseUrl}?page={totalPages}&size={size}";
		string? prev = page > 1 ? $"{baseUrl}?page={page - 1}&size={size}" : null;
		string? next = page < totalPages ? $"{baseUrl}?page={page + 1}&size={size}" : null;

		res.Headers["Content-Type"] = "application/json; charset=utf-8";
		res.Headers["X-Total-Count"] = pagedResult.TotalCount.ToString();
		res.Headers["X-Page"] = page.ToString();
		res.Headers["X-Page-Size"] = size.ToString();
		res.Headers["X-Total-Pages"] = totalPages.ToString();

		// Optional RFC 5988 Link header for discoverability

		var linkParts = new List<string>();
		if(prev != null) { linkParts.Add($"<{prev}>;  rel=\"prev\""); }
		if(next != null) { linkParts.Add($"<{next}>;  rel=\"next\""); }
		if(first != null) { linkParts.Add($"<{first}>; rel=\"first\""); }
		if(last != null) { linkParts.Add($"<{last}>;  rel=\"last\""); }
		if(linkParts.Count > 0) { res.Headers["Link"] = string.Join(", ", linkParts); }
	}

	public static async Task SendPagedResultResponse<T>(HttpListenerRequest req, HttpListenerResponse res, Hashtable props, Result<PagedResult<T>> result, int page, int size)
	{
		if(result.IsError)
		{
			res.Headers["Cache-Control"] = "no-store";

			await HttpUtils.SendResponse(req, res, props, result.StatusCode, result.Error!.ToString()!);
		}
		else
		{
			var pagedResult = result.Payload!;

			HttpUtils.AddPaginationHeaders(req, res, props, pagedResult, page, size);

			await HttpUtils.SendResponse(req, res, props, result.StatusCode, result.Payload!.ToString()!);
		}
	}

	public static async Task SendResultResponse<T>(HttpListenerRequest req, HttpListenerResponse res, Hashtable props, Result<T> result)
	{
		if(result.IsError)
		{
			res.Headers["Cache-Control"] = "no-store";
			
			await HttpUtils.SendResponse(req, res, props, result.StatusCode, result.Error!.ToString()!);
		}
		else
		{
			await HttpUtils.SendResponse(req, res, props, result.StatusCode, result.Payload!.ToString()!);
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
		await SendResponse(req, res, props, (int)HttpStatusCode.OK, content, contentType);
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
		await SendResponse(req, res, props, (int)HttpStatusCode.NotFound, content, contentType);
	}

	public static async Task SendResponse(HttpListenerRequest req, HttpListenerResponse res, Hashtable props, int statusCode, string content)
	{
		await SendResponse(req, res, props, statusCode, content, DetectContentType(content));
	}

	public static async Task SendResponse(HttpListenerRequest req, HttpListenerResponse res, Hashtable props, int statusCode, string content, string contentType)
	{
		byte[] contentBytes = Encoding.UTF8.GetBytes(content);
		res.StatusCode = statusCode;
		res.ContentEncoding = Encoding.UTF8;
		res.ContentType = contentType;
		res.ContentLength64 = contentBytes.LongLength;
		await res.OutputStream.WriteAsync(contentBytes);
		res.Close();
	}
}
