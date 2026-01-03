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
		[FromQuery] InvestmentType? type = null)
	{
		return await investmentManagement.GetInvestmentsAsync(skip, take, fromDate, toDate, quoteId, groupId, type);
	}

	[HttpGet("{id}")]
	public async Task<ApiResponse> GetInvestmentById(int id)
	{
		return await investmentManagement.GetInvestmentByIdAsync(id);
	}

	[HttpPost]
	public async Task<ApiResponse> CreateInvestment([FromBody] InvestmentRequest investment)
	{
		return await investmentManagement.CreateInvestmentAsync(investment);
	}

	[HttpPost("bulk")]
	public async Task<ApiResponse> CreateInvestmentsBulk([FromBody] List<InvestmentRequest> investments)
	{
		return await investmentManagement.CreateInvestmentsAsync(investments);
	}

	[HttpPut("{id}")]
	public async Task<ApiResponse> UpdateInvestment(int id, [FromBody] InvestmentRequest investment)
	{
		return await investmentManagement.UpdateInvestmentAsync(id, investment);
	}

	[HttpDelete("{id}")]
	public async Task<ApiResponse> DeleteInvestment(int id)
	{
		return await investmentManagement.DeleteInvestmentAsync(id);
	}
}