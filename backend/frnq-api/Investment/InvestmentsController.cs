using System.Threading.Tasks;
using DSaladin.Frnq.Api.Investment;
using Microsoft.AspNetCore.Mvc;

namespace DSaladin.Frnq.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InvestmentsController(InvestmentManagement investmentManagement) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetInvestments()
    {
        return Ok(await investmentManagement.GetInvestmentsAsync());
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