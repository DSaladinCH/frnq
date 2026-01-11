using System.Threading.Tasks;
using DSaladin.Frnq.Api.Investment;
using DSaladin.Frnq.Api.Quote;
using DSaladin.Frnq.Api.Result;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DSaladin.Frnq.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InvestmentsController(InvestmentManagement investmentManagement) : ControllerBase
{
	[HttpGet]
	public async Task<ApiResponse> GetInvestments(
		[FromQuery] int skip = 0,
		[FromQuery] int take = 25,
		[FromQuery] DateTime? fromDate = null,
		[FromQuery] DateTime? toDate = null,
		[FromQuery] int? quoteId = null,
		[FromQuery] int? groupId = null,
		[FromQuery] InvestmentType? type = null,
		CancellationToken cancellationToken = default)
	{
		return await investmentManagement.GetInvestmentsAsync(skip, take, fromDate, toDate, quoteId, groupId, type, cancellationToken);
	}

	[HttpGet("{id}")]
	public async Task<ApiResponse> GetInvestmentById(int id, CancellationToken cancellationToken)
	{
		return await investmentManagement.GetInvestmentByIdAsync(id, cancellationToken);
	}

	[HttpPost]
	public async Task<ApiResponse> CreateInvestment([FromBody] InvestmentDto investment, CancellationToken cancellationToken)
	{
		return await investmentManagement.CreateInvestmentAsync(investment, cancellationToken);
	}

	[HttpPost("bulk")]
	public async Task<ApiResponse> CreateInvestmentsBulk([FromBody] List<InvestmentDto> investments, CancellationToken cancellationToken)
	{
		return await investmentManagement.CreateInvestmentsAsync(investments, cancellationToken);
	}

	[HttpPut("{id}")]
	public async Task<ApiResponse> UpdateInvestment(int id, [FromBody] InvestmentDto investment, CancellationToken cancellationToken)
	{
		return await investmentManagement.UpdateInvestmentAsync(id, investment, cancellationToken);
	}

	[HttpDelete("{id}")]
	public async Task<ApiResponse> DeleteInvestment(int id, CancellationToken cancellationToken)
	{
		return await investmentManagement.DeleteInvestmentAsync(id, cancellationToken);
	}
}