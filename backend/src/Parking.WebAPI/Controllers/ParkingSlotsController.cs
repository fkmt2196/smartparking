using Microsoft.AspNetCore.Mvc;
using Parking.Application.Services;

namespace Parking.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ParkingSlotsController : ControllerBase
{
    private readonly ParkingSlotService _parkingSlotService;

    public ParkingSlotsController(ParkingSlotService parkingSlotService)
    {
        _parkingSlotService = parkingSlotService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var slots = await _parkingSlotService.GetAllSlotsAsync();
        return Ok(slots);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var slot = await _parkingSlotService.GetSlotByIdAsync(id);
        if (slot == null)
        {
            return NotFound(new { Message = $"Không tìm thấy vị trí đỗ xe ID = {id}" });
        }
        return Ok(slot);
    }
}
