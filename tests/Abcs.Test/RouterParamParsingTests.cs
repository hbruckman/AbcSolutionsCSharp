using System;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using Xunit;
using Abcs.Http;

public class RouterParamParsingTests
{
    // Small helper to make assertions on NameValueCollection easier
    private static (bool Has, string? Val) Get(NameValueCollection? nvc, string key)
        => (nvc?.AllKeys?.Contains(key) == true, nvc?[key]);

    [Theory]
    [InlineData("/users/42", "/users/:id", "id", "42")]
    [InlineData("/api/v1/courses/CS101", "/api/v1/courses/:code", "code", "CS101")]
    public void Single_param_is_extracted_correctly(string uPath, string rPath, string pName, string expected)
    {
        // Act
        var nvc = HttpRouter.ParseUrlParams(uPath, rPath);

        // Assert
        var (has, val) = Get(nvc, pName);
        Assert.NotNull(nvc);
        Assert.True(has);
        Assert.Equal(expected, val);
        // No extra params expected
        Assert.Single(nvc!);
    }

    [Fact]
    public void Multiple_params_across_segments_are_extracted()
    {
        var nvc = HttpRouter.ParseUrlParams("/orgs/abc/users/42/profile", "/orgs/:org/users/:id/profile");
        Assert.NotNull(nvc);
        Assert.Equal("abc", nvc!["org"]);
        Assert.Equal("42", nvc["id"]);
        Assert.Equal(2, nvc.Count);
    }

    [Theory]
    [InlineData("/users/42/", "/users/:id")]         // trailing slash on url
    [InlineData("/users/42", "/users/:id/")]         // trailing slash on route
    [InlineData("/users/42/", "/users/:id/")]        // both trailing
    [InlineData("users/42", "users/:id")]            // no leading slashes
    public void Leading_and_trailing_slashes_are_ignored(string uPath, string rPath)
    {
        var nvc = HttpRouter.ParseUrlParams(uPath, rPath);
        Assert.NotNull(nvc);
        Assert.Equal("42", nvc!["id"]);
    }

    [Fact]
    public void Literal_segment_mismatch_returns_null()
    {
        // "members" != "users" should fail even though parameter position matches
        var nvc = HttpRouter.ParseUrlParams("/members/42", "/users/:id");
        Assert.Null(nvc);
    }

    [Theory]
    [InlineData("/users", "/users/:id")]          // missing segment
    [InlineData("/users/42/extra", "/users/:id")] // extra segment
    public void Different_segment_counts_return_null(string uPath, string rPath)
    {
        var nvc = HttpRouter.ParseUrlParams(uPath, rPath);
        Assert.Null(nvc);
    }

    [Fact]
    public void Url_decoding_applies_plus_as_space_and_percent_sequences()
    {
        // "+", "%20", and percent-encoded unicode should decode
        var nvc = HttpRouter.ParseUrlParams("/search/John+Doe/notes/ol%C3%A9", "/search/:name/notes/:word");
        Assert.NotNull(nvc);
        Assert.Equal("John Doe", nvc!["name"]); // "+" -> space under HttpUtility.UrlDecode
        Assert.Equal("olé", nvc["word"]);       // %C3%A9 -> é
    }

    [Fact]
    public void Parameter_value_that_matches_literal_is_still_captured()
    {
        // Even if the value equals the literal, the route shape governs extraction
        var nvc = HttpRouter.ParseUrlParams("/users/users", "/users/:id");
        Assert.NotNull(nvc);
        Assert.Equal("users", nvc!["id"]);
    }

    [Fact]
    public void Empty_route_template_results_in_empty_param_set_when_paths_match()
    {
        // Edge case: both reduce to empty when trimmed—treat as match with no params
        var nvc = HttpRouter.ParseUrlParams("/", "/");
        Assert.NotNull(nvc);
        Assert.Empty(nvc!);
    }

    [Fact]
    public void Parameter_values_are_unescaped_per_segment_not_across_slashes()
    {
        // "%2F" decodes to "/", but since we split path first, this remains within segment
        var nvc = HttpRouter.ParseUrlParams("/files/a%2Fb", "/files/:name");
        Assert.NotNull(nvc);
        Assert.Equal("a/b", nvc!["name"]);
    }
}
