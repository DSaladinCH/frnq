using DSaladin.Frnq.Api.Testing.Infrastructure;
using Xunit;
using Allure.Xunit.Attributes;
using System.Net;
using DSaladin.Frnq.Api.Testing.Api;
using DSaladin.Frnq.Api.Position;
using DSaladin.Frnq.Api.Auth;

namespace DSaladin.Frnq.Api.Testing.Tests;

[AllureSuite("Position")]
public class Position : TestBase
{
    [Fact]
    public async Task GetPositions_WhenAuthenticated_ReturnsPositions()
    {
        using AuthenticationScope<UserModel> authScope = await Authenticate();
		TestResponse<PositionsResponse> response = await ApiInterface.Positions.GetPositions();

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Content);
        // Ensure seeder data is present
        Assert.NotEmpty(response.Content.Snapshots);
    }
}
