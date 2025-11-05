using System.Collections;
using System.Collections.Specialized;
using System.Net;
using System.Web;

namespace Abcs.Http;

public class HttpRouter
{
	public const int RESPONSE_NOT_SENT = 777;

	private static ulong requestId = 0;
	private string basePath;
	private List<HttpMiddleware> middlewares;
	private List<(string, string, HttpMiddleware[])> routes;

	public HttpRouter()
	{
		basePath = string.Empty;
		middlewares = [];
		routes = [];
	}

	public HttpRouter Use(params HttpMiddleware[] middlewares)
	{
		this.middlewares.AddRange(middlewares);

		return this;
	}

	public HttpRouter UseDefaultResponse()
	{
		return Use(DefaultResponse);
	}

	public HttpRouter UseRouteMatching()
	{
		return Use(RouteMatchingMiddleware);
	}

	public HttpRouter UseRouter(string path, HttpRouter router)
	{
		basePath += path;

		return Use(router.HandleAsync);
	}

	public HttpRouter Map(string method, string path, params HttpMiddleware[] middlewares)
	{
		routes.Add((method.ToUpperInvariant(), path, middlewares));

		return this;
	}

	public HttpRouter MapGet(string path, params HttpMiddleware[] middlewares)
	{
		return Map("GET", path, middlewares);
	}

	public HttpRouter MapPost(string path, params HttpMiddleware[] middlewares)
	{
		return Map("POST", path, middlewares);
	}

	public HttpRouter MapPut(string path, params HttpMiddleware[] middlewares)
	{
		return Map("PUT", path, middlewares);
	}

	public HttpRouter MapDelete(string path, params HttpMiddleware[] middlewares)
	{
		return Map("DELETE", path, middlewares);
	}

	public async Task HandleContextAsync(HttpListenerContext ctx)
	{
		var req = ctx.Request;
		var res = ctx.Response;
		var props = new Hashtable();

		res.StatusCode = RESPONSE_NOT_SENT;
		props["req.id"] = ++requestId;

		await HandleAsync(req, res, props, () => Task.CompletedTask);
	}

	private async Task HandleAsync(HttpListenerRequest req, HttpListenerResponse res, Hashtable props, Func<Task> next)
	{
		Func<Task> preRouteMatchMiddlewarePipeline = GenerateMiddlewarePipeline(req, res, props, middlewares);

		await preRouteMatchMiddlewarePipeline();

		await next();
	}

	private async Task DefaultResponse(HttpListenerRequest req, HttpListenerResponse res, Hashtable props, Func<Task> next)
	{
		await next();

		if(res.StatusCode == RESPONSE_NOT_SENT)
		{
			res.StatusCode = (int) HttpStatusCode.NotFound;
			res.Close();
		}
	}

	private async Task RouteMatchingMiddleware(HttpListenerRequest req, HttpListenerResponse res, Hashtable props, Func<Task> next)
	{
		if (string.IsNullOrEmpty(basePath) || req.Url!.AbsolutePath.StartsWith(basePath))
		{
			foreach (var (method, path, middlewares) in routes)
			{
				NameValueCollection? parameters;

				if (req.HttpMethod == method && (parameters = ParseUrlParams(req.Url!.AbsolutePath, basePath + path)) != null)
				{
					props["urlParams"] = parameters;
					Func<Task> postRouteMatchMiddlewarePipeline = GenerateMiddlewarePipeline(req, res, props, middlewares.ToList());
					await postRouteMatchMiddlewarePipeline();
					break;
				}
			}
		}

		await next();
	}

	private static NameValueCollection? ParseUrlParams(string uPath, string rPath)
	{
		string[] uParts = uPath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
		string[] rParts = rPath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);

		if(uParts.Length != rParts.Length) { return null; }

		var parameters = new NameValueCollection();

		for(int i = 0; i < rParts.Length; i++)
		{
			string uPart = uParts[i];
			string rPart = rParts[i];

			if(rPart.StartsWith(":"))
			{
				string paramName = rPart.Substring(1);
				parameters[paramName] = HttpUtility.UrlDecode(uPart); // Decodes "+" -> " "
				//parameters[paramName] = WebUtility.UrlDecode(rPart); // Official but does not
			}
			else if(uPart != rPart)
			{
				return null;
			}
		}

		return parameters;
	}

	private Func<Task> GenerateMiddlewarePipeline(HttpListenerRequest req, HttpListenerResponse res, Hashtable props, List<HttpMiddleware> middlewares)
	{
		int index = -1;
		Func<Task> next = () => Task.CompletedTask;
		next = async () =>
		{
			index++;
			if (index < middlewares.Count && res.StatusCode == RESPONSE_NOT_SENT)
			{
				await middlewares[index](req, res, props, next);
			}
		};

		return next;
	}
}
