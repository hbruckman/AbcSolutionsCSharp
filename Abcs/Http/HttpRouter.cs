using System.Collections;
using System.Net;

namespace Abcs.Http;

public class HttpRouter
{
	public const int RESPONSE_NOT_SENT = 777;
	private string basePath;
	private HttpMiddleware[] middlewares;
	private (string, string, HttpMiddleware[])[] routes;

	public HttpRouter(string basePath = "")
	{
		this.basePath = basePath;
		middlewares = Array.Empty<HttpMiddleware>();
		routes = Array.Empty<(string, string, HttpMiddleware[])>();
	}

	public string GetBasePath()
	{
		return basePath;
	}

	public void SetBasePath(string basePath)
	{
		this.basePath = basePath;
	}

	public HttpRouter Use(params HttpMiddleware[] middleware)
	{
		HttpMiddleware[] tmp = new HttpMiddleware[middlewares.Length + middleware.Length];
		middlewares.CopyTo(tmp, 0);
		middleware.CopyTo(tmp, middlewares.Length);
		middlewares = tmp;
		return this;
	}

	public HttpRouter UseRouteMatching()
	{
		return Use(RouteMatchingMiddleware);
	}

	public HttpRouter UseRouter(string path, HttpRouter router)
	{
		router.SetBasePath(basePath + path);
		return Use(router.HandleAsync);
	}

	public HttpRouter Map(string method, string path, params HttpMiddleware[] middlewares)
	{
		(string, string, HttpMiddleware[])[] tmp = new (string, string, HttpMiddleware[])[routes.Length + 1];
		routes.CopyTo(tmp, 0);
		tmp[^1] = (method, path, middlewares);
		routes = tmp;
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

	public async Task HandleAsync(HttpListenerRequest req, HttpListenerResponse res, Hashtable props, Func<Task> _next)
	{
		res.StatusCode = RESPONSE_NOT_SENT;

		Func<Task> middlewaresStart = GenerateMiddlewarePipeline(req, res, props, middlewares);
		await middlewaresStart();

		Console.WriteLine($"Response status: {res.StatusCode}");

		if (res.StatusCode == RESPONSE_NOT_SENT)
		{
			await HttpUtils.SendNotFoundResponse(req, res, props);
		}
	}

	private async Task RouteMatchingMiddleware(HttpListenerRequest req, HttpListenerResponse res, Hashtable props, Func<Task> _)
	{
		foreach (var (method, path, middlewares) in routes)
		{
			Hashtable? parameters;

			Console.WriteLine($"  req: {req.HttpMethod} {req.Url!.AbsolutePath}");
			Console.WriteLine($"route: {method} {basePath + path}");
			Console.WriteLine($"match: {(req.HttpMethod == method && (parameters = HttpUtils.ParseUrlParams(req.Url!.AbsolutePath, basePath + path)) != null)}");

			if (req.HttpMethod == method && (parameters = HttpUtils.ParseUrlParams(req.Url!.AbsolutePath, basePath + path)) != null)
			{
				props["urlParams"] = parameters;
				Func<Task> next = GenerateMiddlewarePipeline(req, res, props, middlewares);
				await next();
			}
		}
	}

	private Func<Task> GenerateMiddlewarePipeline(HttpListenerRequest req, HttpListenerResponse res, Hashtable props, HttpMiddleware[] middlewares)
	{
		int index = -1;
		Func<Task> next = null!;
		next = async () =>
		{
			index++;
			if (index < middlewares.Length && res.StatusCode == RESPONSE_NOT_SENT)
			{
				await middlewares[index](req, res, props, next);
			}
		};

		return next;
	}
}
