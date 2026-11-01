using System.Net;
using dotBento.Infrastructure.Utilities;

namespace dotBento.Infrastructure.Tests.Utilities;

public sealed class PublicAddressValidatorTests
{
    [Theory]
    [InlineData("8.8.8.8")]
    [InlineData("1.1.1.1")]
    [InlineData("140.82.121.4")]
    [InlineData("2606:4700:4700::1111")]
    public void PublicAddressesAreAllowed(string value)
    {
        Assert.True(PublicAddressValidator.IsPublic(IPAddress.Parse(value)));
    }

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("10.0.0.1")]
    [InlineData("172.16.0.1")]
    [InlineData("192.168.1.1")]
    [InlineData("169.254.169.254")]
    [InlineData("100.64.0.1")]
    [InlineData("0.0.0.0")]
    [InlineData("224.0.0.1")]
    [InlineData("::1")]
    [InlineData("fe80::1")]
    [InlineData("fc00::1")]
    [InlineData("::ffff:10.0.0.1")]
    [InlineData("2001:db8::1")]
    public void NonPublicAddressesAreRejected(string value)
    {
        Assert.False(PublicAddressValidator.IsPublic(IPAddress.Parse(value)));
    }

    [Fact]
    public void AllPublic_RequiresAtLeastOneAddressAndRejectsMixedAnswers()
    {
        Assert.False(PublicAddressValidator.AllPublic([]));
        Assert.True(PublicAddressValidator.AllPublic([IPAddress.Parse("8.8.8.8")]));
        Assert.False(PublicAddressValidator.AllPublic([IPAddress.Parse("8.8.8.8"), IPAddress.Parse("10.0.0.1")]));
    }
}
