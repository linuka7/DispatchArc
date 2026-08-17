using DispatchArc.Api.Configuration;
using Microsoft.Extensions.Configuration;

namespace DispatchArc.IntegrationTests.Configuration;

public sealed class ReverseProxyConfigurationTests
{
    [Fact]
    public void DisabledReverseProxyRequiresNoTrustedAddresses()
    {
        var configuration =
            CreateConfiguration(
                enabled: false);

        var result =
            ReverseProxyConfigurationValidator
                .ValidateAndGet(
                    configuration);

        Assert.False(
            result.Enabled);

        Assert.Empty(
            result.KnownProxies);
    }

    [Fact]
    public void EnabledReverseProxyRequiresTrustedAddress()
    {
        var configuration =
            CreateConfiguration(
                enabled: true);

        var exception =
            Assert.Throws<InvalidOperationException>(
                () =>
                    ReverseProxyConfigurationValidator
                        .ValidateAndGet(
                            configuration));

        Assert.Contains(
            "no trusted proxy addresses",
            exception.Message);
    }

    [Fact]
    public void InvalidTrustedProxyAddressIsRejected()
    {
        var configuration =
            CreateConfiguration(
                enabled: true,
                "not-an-ip");

        var exception =
            Assert.Throws<InvalidOperationException>(
                () =>
                    ReverseProxyConfigurationValidator
                        .ValidateAndGet(
                            configuration));

        Assert.Contains(
            "not a valid IP address",
            exception.Message);
    }

    [Fact]
    public void ValidTrustedProxyAddressesAreAccepted()
    {
        var configuration =
            CreateConfiguration(
                enabled: true,
                "127.0.0.1",
                "10.10.0.25");

        var result =
            ReverseProxyConfigurationValidator
                .ValidateAndGet(
                    configuration);

        Assert.True(
            result.Enabled);

        Assert.Equal(
            2,
            result.KnownProxies.Count);

        Assert.Equal(
            "127.0.0.1",
            result.KnownProxies[0]
                .ToString());

        Assert.Equal(
            "10.10.0.25",
            result.KnownProxies[1]
                .ToString());
    }

    private static IConfiguration CreateConfiguration(
        bool enabled,
        params string[] knownProxies)
    {
        var values =
            new Dictionary<string, string?>
            {
                ["ReverseProxy:Enabled"] =
                    enabled.ToString()
            };

        for (var index = 0;
             index < knownProxies.Length;
             index++)
        {
            values[
                $"ReverseProxy:KnownProxies:{index}"] =
                    knownProxies[index];
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}