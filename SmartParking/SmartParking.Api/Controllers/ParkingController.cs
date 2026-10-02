using Microsoft.AspNetCore.Mvc;
using SmartParking.Application.UseCases;

namespace SmartParking.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ParkingController : ControllerBase
    {
        private readonly GetSlotStatusUseCase _getSlotStatusUseCase;

        public ParkingController(GetSlotStatusUseCase getSlotStatusUseCase)
        {
            _getSlotStatusUseCase = getSlotStatusUseCase;
        }

        [HttpGet("slot/{id}")]
        public async Task<IActionResult> GetSlotStatus(string id)
        {
            var result = await _getSlotStatusUseCase.ExecuteAsync(id);
            return Ok(result);
        }
    }
}
