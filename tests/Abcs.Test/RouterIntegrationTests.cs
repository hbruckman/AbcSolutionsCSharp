using System;
using System.Collections;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Xunit;
using Abcs.Http;

public class RouterIntegrationTests
{
    private static void ConfigureRoutes(HttpRouter r)
    {
        // GET /ping -> "pong"
        r.MapGet("/ping", new[]
        {
            (HttpMiddleware)(async (req, res, props, next) =>
            {
                await HttpUtils.SendOkResponse(req, res, props, "pong");
            })
        });

        // GET /users/:id -> { "id": "..." }
        r.MapGet("/users/:id", new[]
        {
            (HttpMiddleware)(async (req, res, props, next) =>
            {
                var id = req.Url!.Segments.Last().TrimEnd('/');
                var node = new JsonObject { ["id"] = id };
                await HttpUtils.SendResponse(req, res, props, (int)HttpStatusCode.OK, node.ToJsonString(), "application/json");
            })
        });

        // GET /items?page={p}&size={s}
        r.MapGet("/items", new[]
        {
            (HttpMiddleware)(async (req, res, props, next) =>
            {
                int page = int.TryParse(req.QueryString["page"], out var p) ? Math.Max(1, p) : 1;
                int size = int.TryParse(req.QueryString["size"], out var s) ? Math.Max(1, s) : 3;

                var total = 10;
                var start = (page - 1) * size;
                var values = Enumerable.Range(start + 1, Math.Max(0, Math.Min(size, total - start))).ToList();

                var paged = new PagedResult<int>(total, values);

                HttpUtils.AddPaginationHeaders(req, res, props, paged, page, size);

                var payload = JsonSerializer.Serialize(values);
                await HttpUtils.SendResponse(req, res, props, (int)HttpStatusCode.OK, payload, "application/json");
            })
        });

        // POST /echo -> echos body as JSON
        r.MapPost("/echo", new[]
				{
						HttpUtils.ReadRequestBodyAsJson,
            (HttpMiddleware)(async (req, res, props, next) =>
            {
                var json = (JsonObject) props["req.json"]!;
                await HttpUtils.SendResponse(req, res, props, (int)HttpStatusCode.OK, json.ToString(), "application/json");
            })
        });
    }

    private static string? GetHeaderValue(HttpResponseHeaders headers, string name)
        => headers.TryGetValues(name, out var values) ? values.FirstOrDefault() : null;

    [Fact]
    public async Task Ping_returns_200_and_XRequestId()
    {
        await using var server = await TestServer.StartAsync(ConfigureRoutes);
        var (status, headers, contentHeaders, body) = await server.GetAsync("/ping");

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Equal("pong", body);

        var reqId = GetHeaderValue(headers, "X-Request-Id");
        Assert.False(string.IsNullOrWhiteSpace(reqId));
    }

    [Fact]
    public async Task Unknown_route_returns_404_by_default()
    {
        await using var server = await TestServer.StartAsync(ConfigureRoutes);
        var (status, _, _, _) = await server.TryGetAsync("/nope");
        Assert.Equal(HttpStatusCode.NotFound, status);
    }

    [Fact]
    public async Task Parameterized_route_selects_correct_handler()
    {
        await using var server = await TestServer.StartAsync(ConfigureRoutes);
        var (status, headers, contentHeaders, body) = await server.GetAsync("/users/42");

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Equal("application/json", contentHeaders.ContentType?.ToString());

        var obj = JsonNode.Parse(body)!.AsObject();
        Assert.Equal("42", (string?)obj["id"]);
    }

    [Fact]
    public async Task Pagination_headers_are_emitted_and_consistent()
    {
        await using var server = await TestServer.StartAsync(ConfigureRoutes);
        var (status, headers, contentHeaders, body) = await server.GetAsync("/items?page=2&size=3");

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Equal("application/json", contentHeaders.ContentType?.ToString());
        Assert.Equal("10", GetHeaderValue(headers, "X-Total-Count"));
        Assert.Equal("2",  GetHeaderValue(headers, "X-Page"));
        Assert.Equal("3",  GetHeaderValue(headers, "X-Page-Size"));
        Assert.Equal("4",  GetHeaderValue(headers, "X-Total-Pages")); // ceil(10/3)=4

        var arr = JsonNode.Parse(body)!.AsArray();
        Assert.True(arr.Count > 0);
        Assert.Equal(4, (int)arr[0]!); // page 2, size 3 -> 4,5,6
    }

    [Fact]
    public async Task Echo_json_roundtrip_works()
    {
        await using var server = await TestServer.StartAsync(ConfigureRoutes);
        var payload = new { msg = "hello", n = 123 };
        var (status, headers, contentHeaders, body) = await server.PostJsonAsync("/echo", payload);

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Equal("application/json", contentHeaders.ContentType?.ToString());

        var obj = JsonNode.Parse(body)!.AsObject();
        Assert.Equal("hello", (string?)obj["msg"]);
        Assert.Equal(123, (int?)obj["n"]);
    }
}
