using Xunit;
using Abcs.Http;

public class JsonUtilsOptionsTests
{
    [Fact]
    public void DefaultOptions_Are_CamelCase_And_CaseInsensitive()
    {
        Assert.True(JsonUtils.DefaultOptions.PropertyNameCaseInsensitive);
        Assert.Equal(System.Text.Json.JsonNamingPolicy.CamelCase, JsonUtils.DefaultOptions.PropertyNamingPolicy);
    }
}
