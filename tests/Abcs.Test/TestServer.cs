using System;
using System.Collections;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Abcs.Http;

public sealed class TestServer : IAsyncDisposable
{
    private readonly HttpListener _listener = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _loopTask;
    private readonly HttpRouter _router;
    private readonly HttpClient _client = new();

    public string BaseUrl { get; }

    private TestServer(string baseUrl, HttpRouter router)
    {
        BaseUrl = baseUrl;
        _router = router;
        _listener.Prefixes.Add(baseUrl);
        _listener.Start();
        _loopTask = Task.Run(() => AcceptLoopAsync(_cts.Token));
    }

    public static async Task<TestServer> StartAsync(Action<HttpRouter> configure)
    {
        var port = GetFreeTcpPort();
        var baseUrl = $"http://127.0.0.1:{port}/";

        var router = new HttpRouter()
            .Use(HttpUtils.StructuredLogging, HttpUtils.CentralizedErrorHandling)
            .UseDefaultResponse()
            .UseSimpleRouteMatching()
            .UseParametrizedRouteMatching();

        configure(router);
        var server = new TestServer(baseUrl, router);

        await Task.Yield();
        return server;
    }

    public async ValueTask DisposeAsync()
    {
        // IMPORTANT: stop/close the listener first so GetContextAsync unblocks.
        try { _listener.Stop(); } catch { }
        try { _listener.Close(); } catch { }

        // Then signal background tasks to stop and await with a short timeout.
        _cts.Cancel();
        try
        {
            // .NET 6+: WaitAsync timeout; otherwise you can poll Task.WhenAny
            await _loopTask.WaitAsync(TimeSpan.FromSeconds(2));
        }
        catch { /* swallow; we're tearing down a test helper */ }

        _client.Dispose();
        _cts.Dispose();
    }

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            if (!_listener.IsListening) break;

            HttpListenerContext ctx;
            try
            {
                ctx = await _listener.GetContextAsync();
            }
            catch (ObjectDisposedException) { break; }
            catch (HttpListenerException)
            {
                // Happens when listener is stopped/closed; exit the loop.
                break;
            }
            catch
            {
                // Unexpected error: keep the loop alive unless we're shutting down.
                if (!_listener.IsListening || ct.IsCancellationRequested) break;
                continue;
            }

            _ = Task.Run(async () =>
            {
                try { await _router.HandleContextAsync(ctx); }
                catch
                {
                    // Last-resort safety net
                    if (ctx.Response.OutputStream.CanWrite)
                    {
                        var msg = Encoding.UTF8.GetBytes("Unhandled error");
                        ctx.Response.StatusCode = 500;
                        ctx.Response.OutputStream.Write(msg, 0, msg.Length);
                        try { ctx.Response.Close(); } catch { }
                    }
                }
            }, ct);
        }
    }

    private static int GetFreeTcpPort()
    {
        var l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        var port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return port;
    }

    // ---- HTTP helpers -------------------------------------------------------
    public async Task<(HttpStatusCode status, HttpResponseHeaders headers, HttpContentHeaders contentHeaders, string body)>
        GetAsync(string relativePath)
    {
        var resp = await _client.GetAsync(BaseUrl + relativePath.TrimStart('/'));
        var body = await resp.Content.ReadAsStringAsync();
        return (resp.StatusCode, resp.Headers, resp.Content.Headers, body);
    }

    public async Task<(HttpStatusCode status, HttpResponseHeaders headers, HttpContentHeaders contentHeaders, string body)>
        TryGetAsync(string relativePath)
    {
        var resp = await _client.GetAsync(BaseUrl + relativePath.TrimStart('/'));
        var body = await resp.Content.ReadAsStringAsync();
        return (resp.StatusCode, resp.Headers, resp.Content.Headers, body);
    }

    public async Task<(HttpStatusCode status, HttpResponseHeaders headers, HttpContentHeaders contentHeaders, string body)>
        PostJsonAsync(string relativePath, object payload)
    {
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var resp = await _client.PostAsync(BaseUrl + relativePath.TrimStart('/'), content);
        var body = await resp.Content.ReadAsStringAsync();
        return (resp.StatusCode, resp.Headers, resp.Content.Headers, body);
    }
}
