using lab09.Exceptions;
using lab09.Model;
using lab09.Services;
using Microsoft.AspNetCore.Mvc;

namespace lab09.Controllers;
[Route("api/[controller]")]
[ApiController]
public class WarehouseController : Controller
{
    private readonly IDbService _dbService;

    public WarehouseController(IDbService dbService)
    {
        _dbService = dbService;
    }

    [HttpPut]
    public async Task<IActionResult> Put([FromBody] ProductWarehouseDto productWarehouseDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _dbService.CreateProductWarehouse(productWarehouseDto);
            return CreatedAtAction(nameof(Put), result);
        }
        catch (NotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
    
}