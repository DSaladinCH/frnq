using DSaladin.Frnq.Api.Testing.Infrastructure;
using Xunit;
using Allure.Xunit.Attributes;
using System.Net;
using DSaladin.Frnq.Api.Testing.Api;
using DSaladin.Frnq.Api.Position;
using DSaladin.Frnq.Api.Auth;
using DSaladin.Frnq.Api.Result;

namespace DSaladin.Frnq.Api.Testing.Tests;

[AllureSuite("Position")]
public class Position : TestBase
{
    [Fact]
    public async Task GetPositions_WhenAuthenticated_ReturnsPositions()
    {
        using AuthenticationScope<UserModel> authScope = await Authenticate();
		ApiResponse<PositionsResponse> response = await ApiInterface.Positions.GetPositions();

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Value);
        // Ensure seeder data is present
        Assert.NotEmpty(response.Value.Snapshots);
    }
}
