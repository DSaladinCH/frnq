using DSaladin.Frnq.Api.Testing.Infrastructure;
using Xunit;
using Allure.Xunit.Attributes;
using System.Net;

namespace DSaladin.Frnq.Api.Testing.Tests;

[AllureSuite("Position")]
public class Position(CustomWebApplicationFactory<Program> factory) : BaseTest(factory)
{
    [Fact]
    public async Task GetPositions_WhenAuthenticated_ReturnsPositions()
    {
        await AuthenticateAsync();
        var response = await Api.Positions.GetPositions();

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Content);
        // Ensure seeder data is present
        Assert.NotEmpty(response.Content.Snapshots);
    }
}
