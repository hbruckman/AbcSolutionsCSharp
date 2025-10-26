using Xunit;
using Abcs.Http;

namespace Abcs.Tests;

public class UnitTest1
{
  [Theory]
  [InlineData("/users/123",  "/users/:id", "123")]
  [InlineData("/users/123/", "/users/:id", "123")]
  public void ParseUrlParams_Matches_Id(string path, string route, string expectedId)
  {
    var map = HttpUtils.ParseUrlParams(path, route);
    Assert.NotNull(map);
    Assert.Equal(expectedId, map!["id"]);
  }

  [Theory]
  [InlineData("/users/", "/users/:id")]
  [InlineData("/users",  "/users/:id")]
  [InlineData("/actors/7", "/users/:id")]
  public void ParseUrlParams_NoMatch_ReturnsNull(string path, string route)
  {
    var map = HttpUtils.ParseUrlParams(path, route);
    Assert.Null(map);
  }
}
