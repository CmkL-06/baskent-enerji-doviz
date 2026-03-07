using System.Net;
using AnasıTAS_Deniz.Business.Tools;
using Microsoft.AspNetCore.Http;

namespace AnasıTAS_Deniz.Tests;

public class ToolsStringTests
{
    [Fact]
    public void GenerateSlug_RemovesSpecialCharsAndNormalizesDashes()
    {
        var slug = tools_string.GenerateSlug("Merhaba   Dünya!! 2026");

        Assert.Equal("merhaba-dünya-2026", slug);
    }

    [Fact]
    public void DeGenerateSlug_RemovesSeparatorsAndUppercases()
    {
        var title = tools_string.DeGenerateSlug("merhaba-dünya-2026");

        Assert.Equal("MERHABADÜNYA2026", title);
    }

    [Fact]
    public void GetIpAddress_ReturnsEmptyString_WhenIpMissing()
    {
        var context = new DefaultHttpContext();

        Assert.Equal(string.Empty, context.GetIpAddress());
    }

    [Fact]
    public void GetIpAddress_ReturnsIpAddress_WhenPresent()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");

        Assert.Equal("127.0.0.1", context.GetIpAddress());
    }
}