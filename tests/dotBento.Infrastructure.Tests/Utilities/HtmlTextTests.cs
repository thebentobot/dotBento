using dotBento.Infrastructure.Utilities;

namespace dotBento.Infrastructure.Tests.Utilities;

public sealed class HtmlTextTests
{
    [Fact]
    public void Encode_EscapesHtmlMetacharactersAndPreservesEmoji()
    {
        Assert.Equal("&lt;script&gt;alert(&#x27;x&#x27;)&lt;/script&gt;",
            HtmlText.Encode("<script>alert('x')</script>"));
        Assert.Equal("🍱 &amp; friends", HtmlText.Encode("🍱 & friends"));
    }

    [Fact]
    public void Attr_EscapesAttributeBreakouts()
    {
        Assert.Equal("&#x27; onload=&#x27;x", HtmlText.Attr("' onload='x"));
        Assert.Equal("&quot; x=&quot;y", HtmlText.Attr("\" x=\"y"));
    }

    [Fact]
    public void CssUrl_PercentEncodesCssBreakers()
    {
        Assert.Equal("https://example.com/a%27b%29;%3C/style%3E",
            HtmlText.CssUrl("https://example.com/a'b);</style>"));
    }

    [Fact]
    public void CssUrl_LeavesSafeUrlsUntouched()
    {
        Assert.Equal("https://cdn.example/background.png?size=large",
            HtmlText.CssUrl("https://cdn.example/background.png?size=large"));
    }
}
