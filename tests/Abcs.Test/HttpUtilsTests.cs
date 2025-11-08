using System.Collections.Specialized;
using Xunit;
using Abcs.Http;

public class HttpUtilsTests
{
    [Fact]
    public void ParseUrl_Splits_All_Parts()
    {
        var parts = HttpUtils.ParseUrl("https://john:abc123@site.com:8080/api/v1/users/3?q=0&active=true#bio");

        Assert.Equal("https", parts["scheme"]);
        Assert.Equal("john", parts["user"]);
        Assert.Equal("abc123", parts["pass"]);
        Assert.Equal("site.com", parts["host"]);
        Assert.Equal("8080", parts["port"]);
        Assert.Equal("/api/v1/users/3", parts["path"]);
        Assert.Equal("q=0&active=true", parts["query"]);
        Assert.Equal("bio", parts["fragment"]);
    }

    [Fact]
    public void ParseQueryString_Joins_Duplicates_With_Comma_By_Default()
    {
        var q = HttpUtils.ParseQueryString("?a=1&a=2&b=3");
        Assert.Equal("1,2", q["a"]);
        Assert.Equal("3", q["b"]);
    }

    [Fact]
    public void ParseFormData_Custom_Duplicate_Separator()
    {
        var f = HttpUtils.ParseFormData("a=1&a=2&x=9", duplicateSeparator: "|");
        Assert.Equal("1|2", f["a"]);
        Assert.Equal("9", f["x"]);
    }

    [Theory]
    [InlineData("  { \"x\":1 }", "application/json")]
    [InlineData("<html><body>x</body></html>", "text/html")]
    [InlineData("<root/>", "application/xml")]
    [InlineData("hello", "text/plain")]
    public void DetectContentType_Matches_Heuristics(string input, string expected)
    {
        Assert.Equal(expected, HttpUtils.DetectContentType(input));
    }
}
