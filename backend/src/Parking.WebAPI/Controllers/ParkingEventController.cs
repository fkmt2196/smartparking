using Microsoft.AspNetCore.Mvc;
using Parking.Application.Common.Interfaces;
using Parking.Application.Events;
using Parking.Application.Events.Dtos;
using Parking.Domain.Entities;

namespace Parking.WebAPI.Controllers;

/// <summary>
/// API tiếp nhận sự kiện từ Camera AI (cổng vào/ra) và IoT cảm biến siêu âm (tại ô đỗ).
///
/// Luồng sử dụng:
///   1. Camera tại cổng vào chụp ảnh → Gemini phân tích → Staff cổng xác nhận
///      → POST /api/events  { eventType: "VehicleEntered", carPlate: "51F-67890", ... }
///
///   2. Cảm biến siêu âm tại ô A1-012 phát hiện xe
///      → POST /api/events  { eventType: "SensorTriggered", slotCode: "A1-012", ... }
///
///   3. Staff khu vực đến xác nhận, bấm gán xe vào ô trên màn hình mobile
///      → POST /api/events  { eventType: "SlotParked", sessionId: "...", slotCode: "A1-012", ... }
///
///   4. Xe ra cổng, Staff đối chiếu ảnh và thu tiền
///      → POST /api/events  { eventType: "VehicleExited", sessionId: "...", ... }
/// </summary>
[ApiController]
[Route("api/events")]
public class ParkingEventController : ControllerBase
{
    private readonly ParkingEventService     _eventService;
    private readonly IParkingSessionRepository  _sessionRepo;
    private readonly IParkingEventLogRepository _eventLogRepo;
    private readonly ILogger<ParkingEventController> _logger;

    public ParkingEventController(
        ParkingEventService          eventService,
        IParkingSessionRepository    sessionRepo,
        IParkingEventLogRepository   eventLogRepo,
        ILogger<ParkingEventController> logger)
    {
        _eventService = eventService;
        _sessionRepo  = sessionRepo;
        _eventLogRepo = eventLogRepo;
        _logger       = logger;
    }

    /// <summary>
    /// Tiếp nhận sự kiện IoT/Staff. Hỗ trợ Idempotency: gửi lại cùng EventId sẽ trả kết quả cũ.
    /// </summary>
    /// <remarks>
    /// Client tạo EventId = UUID v4 mới cho mỗi lần bấm/gửi request.
    /// Nếu mạng timeout và retry → dùng đúng EventId cũ.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(IngestEventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IngestEventResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> IngestEvent(
        [FromBody] IngestEventRequest request,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "Event received: EventId={EventId} Type={EventType} Plate={CarPlate} Lot={LotId} Slot={SlotCode}",
            request.EventId, request.EventType, request.CarPlate, request.ParkingLotId, request.SlotCode);

        var response = await _eventService.IngestAsync(request, ct);

        if (!response.Success)
        {
            _logger.LogWarning(
                "Event rejected: EventId={EventId} Reason={Reason}",
                request.EventId, response.Message);
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Lấy danh sách phiên đang hoạt động (pending_slot + parked) tại một bãi đỗ.
    /// Dùng cho màn hình điều phối của Staff khu vực.
    /// </summary>
    [HttpGet("sessions/active")]
    [ProducesResponseType(typeof(IReadOnlyList<ActiveSessionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveSessions(
        [FromQuery] int parkingLotId,
        CancellationToken ct)
    {
        if (parkingLotId <= 0)
            return BadRequest(new { message = "parkingLotId phải lớn hơn 0." });

        var sessions = await _sessionRepo.GetActiveByLotAsync(parkingLotId, ct);

        var dtos = sessions.Select(s => new ActiveSessionDto
        {
            SessionId     = s.SessionId,
            CarPlate      = s.CarPlate,
            ParkingLotId  = s.ParkingLotId,
            SlotCode      = s.SlotCode,
            Status        = s.Status,
            WarningCode   = s.WarningCode,
            FuelType      = s.FuelType,
            CarColor      = s.CarColor,
            CarModel      = s.CarModel,
            EntryImageUrl = s.EntryImageUrl,
            EnteredAtUtc  = s.EnteredAtUtc,
            ParkedAtUtc   = s.ParkedAtUtc
        }).ToList();

        return Ok(dtos);
    }

    /// <summary>
    /// Lấy nhật ký sự kiện gần đây tại một bãi đỗ (tối đa 100 bản ghi).
    /// Dùng cho màn hình giám sát và debug.
    /// </summary>
    [HttpGet("logs")]
    [ProducesResponseType(typeof(IReadOnlyList<EventLogDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEventLogs(
        [FromQuery] int parkingLotId,
        [FromQuery] int limit = 100,
        CancellationToken ct = default)
    {
        if (parkingLotId <= 0)
            return BadRequest(new { message = "parkingLotId phải lớn hơn 0." });

        limit = Math.Clamp(limit, 1, 200);

        var logs = await _eventLogRepo.GetRecentByLotAsync(parkingLotId, limit, ct);

        var dtos = logs.Select(l => new EventLogDto
        {
            Id                = l.Id,
            EventId           = l.EventId,
            SessionId         = l.SessionId,
            ParkingLotId      = l.ParkingLotId,
            SlotCode          = l.SlotCode,
            CarPlate          = l.CarPlate,
            EventType         = l.EventType,
            Source            = l.Source,
            TriggeredBy       = l.TriggeredBy,
            ProcessingStatus  = l.ProcessingStatus,
            RejectReason      = l.RejectReason,
            EventTimestampUtc = l.EventTimestampUtc,
            ReceivedAtUtc     = l.ReceivedAtUtc
        }).ToList();

        return Ok(dtos);
    }
}
