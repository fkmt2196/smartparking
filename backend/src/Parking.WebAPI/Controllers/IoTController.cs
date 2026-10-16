using Microsoft.AspNetCore.Mvc;
using Parking.Application.IoT;
using Parking.Application.Services;

namespace Parking.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IoTController : ControllerBase
{
    private readonly IoTService _ioTService;
    private readonly ParkingLotService _parkingLotService;
    private readonly ILogger<IoTController> _logger;

    public IoTController(
        IoTService ioTService,
        ParkingLotService parkingLotService,
        ILogger<IoTController> logger)
    {
        _ioTService = ioTService;
        _parkingLotService = parkingLotService;
        _logger = logger;
    }

    /// <summary>
    /// Nhận telemetry / sự kiện từ cảm biến IoT hoặc simulator (US-FR-IOT-001)
    /// </summary>
    [HttpPost("events")]
    public async Task<IActionResult> IngestEvent([FromBody] IoTEventDto eventDto)
    {
        _logger.LogInformation("IoT Ingestion: Device={Device}, Lot={Lot}, Type={Type}",
            eventDto.DeviceId, eventDto.ParkingLotId, eventDto.EventType);

        var result = await _ioTService.ProcessEventAsync(eventDto);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Lấy danh sách lịch sử telemetry các sự kiện IoT gần nhất
    /// </summary>
    [HttpGet("telemetry")]
    public async Task<IActionResult> GetTelemetryHistory()
    {
        var history = await _ioTService.GetTelemetryHistoryAsync();
        return Ok(history);
    }

    /// <summary>
    /// Reset dữ liệu 3 bãi đỗ xe và telemetry về trạng thái mặc định
    /// </summary>
    [HttpPost("reset")]
    public async Task<IActionResult> Reset()
    {
        var success = await _ioTService.ResetAllAsync();
        return Ok(new { success, message = "Đã reset bãi đỗ xe và dữ liệu telemetry về mặc định." });
    }
}
