using System.Threading.Tasks;
using DSaladin.Frnq.Api.Investment;
using DSaladin.Frnq.Api.Quote;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DSaladin.Frnq.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InvestmentsController(InvestmentManagement investmentManagement) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetInvestments(
        [FromQuery] int skip = 0, 
        [FromQuery] int take = 25,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int? quoteId = null,
        [FromQuery] int? groupId = null,
        [FromQuery] InvestmentType? type = null)
    {
        var result = await investmentManagement.GetInvestmentsAsync(skip, take, fromDate, toDate, quoteId, groupId, type);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetInvestmentById(int id)
    {
        var investment = await investmentManagement.GetInvestmentByIdAsync(id);

        if (investment is null)
            return NotFound();

        return Ok(investment);
    }

    [HttpPost]
    public async Task<IActionResult> CreateInvestment([FromBody] InvestmentRequest investment)
    {
        await investmentManagement.CreateInvestmentAsync(investment);
        return Created();
    }

    [HttpPost("bulk")]
    public async Task<IActionResult> CreateInvestmentsBulk([FromBody] List<InvestmentRequest> investments)
    {
        var createdInvestments = await investmentManagement.CreateInvestmentsAsync(investments);
        return Ok(createdInvestments);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateInvestment(int id, [FromBody] InvestmentRequest investment)
    {
        InvestmentModel updatedInvestment = await investmentManagement.UpdateInvestmentAsync(id, investment);
        return Ok(updatedInvestment);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteInvestment(int id)
    {
        try
        {
            await investmentManagement.DeleteInvestmentAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}