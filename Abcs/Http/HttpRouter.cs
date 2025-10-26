using System.Collections;
using System.Net;

namespace Abcs.Http;

public class HttpRouter
{
	public const int RESPONSE_NOT_SENT = 777;

	private string basePath;
	private HttpMiddleware[] globalMiddlewares;
	private (string, string, HttpMiddleware[])[] routes;

	public HttpRouter()
	{
		basePath = string.Empty;
		globalMiddlewares = [];
		routes = [];
	}

	public string GetBasePath()
	{
		return basePath;
	}

	public void SetBasePath(string basePath)
	{
		this.basePath = basePath;
	}

	public HttpRouter Use(params HttpMiddleware[] middlewares)
	{
		HttpMiddleware[] tmp = new HttpMiddleware[globalMiddlewares.Length + middlewares.Length];
		globalMiddlewares.CopyTo(tmp, 0);
		middlewares.CopyTo(tmp, globalMiddlewares.Length);
		globalMiddlewares = tmp;

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
		var tmp = new (string, string, HttpMiddleware[])[routes.Length + 1];
		routes.CopyTo(tmp, 0);
		tmp[^1] = (method.ToUpperInvariant(), path, middlewares);
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

	public async Task HandleContextAsync(HttpListenerContext ctx)
	{
		var req = ctx.Request;
		var res = ctx.Response;
		var props = new Hashtable();

		res.StatusCode = RESPONSE_NOT_SENT;

		await HandleAsync(req, res, props, () => Task.CompletedTask);

		if (res.StatusCode == RESPONSE_NOT_SENT)
		{
			await HttpUtils.SendNotFoundResponse(req, res, props);
		}

		Console.WriteLine($"Response status: {res.StatusCode}");
	}

	private async Task HandleAsync(HttpListenerRequest req, HttpListenerResponse res, Hashtable props, Func<Task> next)
	{
		Console.WriteLine(this.GetType().Name);
		Console.WriteLine($"Handling request: {req.HttpMethod} {req.Url!.AbsolutePath}");

		Func<Task> preRouteMatchMiddlewarePipeline = GenerateMiddlewarePipeline(req, res, props, globalMiddlewares);
		await preRouteMatchMiddlewarePipeline();

		await next();
	}

	private async Task RouteMatchingMiddleware(HttpListenerRequest req, HttpListenerResponse res, Hashtable props, Func<Task> next)
	{
		Console.WriteLine($"basePath Match: {(string.IsNullOrEmpty(basePath) || req.Url!.AbsolutePath.StartsWith(basePath))}");

		if (string.IsNullOrEmpty(basePath) || req.Url!.AbsolutePath.StartsWith(basePath))
		{
			foreach (var (method, path, middlewares) in routes)
			{
				Hashtable? parameters;

				Console.WriteLine($"  req: {req.HttpMethod} {req.Url!.AbsolutePath}");
				Console.WriteLine($"route: {method} {basePath + path}");
				Console.WriteLine($"match: {req.HttpMethod == method && (parameters = HttpUtils.ParseUrlParams(req.Url!.AbsolutePath, basePath + path)) != null}");

				if (req.HttpMethod == method && (parameters = HttpUtils.ParseUrlParams(req.Url!.AbsolutePath, basePath + path)) != null)
				{
					props["urlParams"] = parameters;
					Func<Task> postRouteMatchMiddlewarePipeline = GenerateMiddlewarePipeline(req, res, props, middlewares);
					await postRouteMatchMiddlewarePipeline();
					break;
				}
			}
		}

		await next();
	}

	private Func<Task> GenerateMiddlewarePipeline(HttpListenerRequest req, HttpListenerResponse res, Hashtable props, HttpMiddleware[] middlewares)
	{
		int index = -1;
		Func<Task> next = null!;
		next = async () =>
		{
			index++;
			if (index < middlewares.Length)
			{
				await middlewares[index](req, res, props, next);
			}
		};

		return next;
	}
}
