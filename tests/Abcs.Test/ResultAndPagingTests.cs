using System.Collections.Generic;
using Xunit;
using Abcs.Http;

public class ResultAndPagingTests
{
    [Fact]
    public void Result_Ok_Holds_Payload_And_Status()
    {
        var r = new Result<string>("ok", 201);
        Assert.False(r.IsError);
        Assert.Equal("ok", r.Payload);
        Assert.Null(r.Error);
        Assert.Equal(201, r.StatusCode);
    }

    [Fact]
    public void Result_Error_Holds_Exception_And_Status()
    {
        var ex = new System.InvalidOperationException("boom");
        var r = new Result<string>(ex, 503);
        Assert.True(r.IsError);
        Assert.Same(ex, r.Error);
        Assert.Null(r.Payload);
        Assert.Equal(503, r.StatusCode);
    }

    [Fact]
    public void PagedResult_Carries_TotalCount_And_Values()
    {
        var p = new PagedResult<int>(totalCount: 5, values: new List<int> { 1, 2, 3 });
        Assert.Equal(5, p.TotalCount);
        Assert.Equal(new[] { 1, 2, 3 }, p.Values);
    }
}
