using Parking.Application.Common.Interfaces;
using Parking.Application.Events.Dtos;
using Parking.Domain.Entities;

namespace Parking.Application.Events;

/// <summary>
/// Service xử lý trung tâm cho luồng sự kiện IoT/Staff.
///
/// Luồng nghiệp vụ Sprint 3:
///   Camera chụp ảnh cổng vào → Gemini phân tích → Staff cổng bấm xác nhận
///     → [VehicleEntered] → Tạo phiên pending_slot
///   IoT cảm biến siêu âm báo có xe tại ô A1-012
///     → [SensorTriggered] → Ghi log, chưa gán phiên
///   Staff khu vực đến xác nhận xe, bấm gán vào phiên
///     → [SlotParked] → Gán slot, chuyển phiên sang parked
///   Xe ra cổng, Staff đối chiếu ảnh, thu tiền
///     → [VehicleExited] → Đóng phiên, giải phóng slot (do Phụng thực hiện qua API riêng)
/// </summary>
public class ParkingEventService
{
    private readonly IParkingEventLogRepository _eventLogRepo;
    private readonly IParkingSessionRepository  _sessionRepo;
    private readonly IParkingSlotRepository     _slotRepo;

    public ParkingEventService(
        IParkingEventLogRepository eventLogRepo,
        IParkingSessionRepository  sessionRepo,
        IParkingSlotRepository     slotRepo)
    {
        _eventLogRepo = eventLogRepo;
        _sessionRepo  = sessionRepo;
        _slotRepo     = slotRepo;
    }

    public async Task<IngestEventResponse> IngestAsync(
        IngestEventRequest request, CancellationToken ct = default)
    {
        // ── 1. IDEMPOTENCY CHECK ────────────────────────────────────────────
        // Nếu EventId đã tồn tại → trả về kết quả cũ, không xử lý lại.
        var existingLog = await _eventLogRepo.FindByEventIdAsync(request.EventId, ct);
        if (existingLog is not null)
        {
            return new IngestEventResponse
            {
                Success           = true,
                IsDuplicate       = true,
                ProcessingStatus  = EventProcessingStatus.Duplicate,
                EventId           = request.EventId,
                SessionId         = existingLog.SessionId,
                Message           = "Sự kiện đã được xử lý trước đó (idempotent – bỏ qua)."
            };
        }

        // ── 2. DISPATCH THEO LOẠI SỰ KIỆN ──────────────────────────────────
        return request.EventType switch
        {
            ParkingEventType.VehicleEntered   => await HandleVehicleEnteredAsync(request, ct),
            ParkingEventType.SensorTriggered  => await HandleSensorTriggeredAsync(request, ct),
            ParkingEventType.SlotParked       => await HandleSlotParkedAsync(request, ct),
            ParkingEventType.VehicleExited    => await HandleVehicleExitedAsync(request, ct),
            _ => await RejectAndLogAsync(request,
                     $"EventType '{request.EventType}' không được hỗ trợ.", ct)
        };
    }

    // ───────────────────────────────────────────────────────────────────────
    // HANDLER: VehicleEntered – Xe qua cổng vào
    // ───────────────────────────────────────────────────────────────────────
    private async Task<IngestEventResponse> HandleVehicleEnteredAsync(
        IngestEventRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.CarPlate))
            return await RejectAndLogAsync(request, "CarPlate bắt buộc với VehicleEntered.", ct);

        // Ngăn xe có biển số đang đỗ vào check-in lại
        var activeSession = await _sessionRepo.FindActiveByCarPlateAsync(request.CarPlate, ct);
        if (activeSession is not null)
        {
            return await RejectAndLogAsync(request,
                $"Biển số '{request.CarPlate}' đang có phiên mở (SessionId: {activeSession.SessionId}). " +
                "Xe phải ra khỏi bãi trước khi check-in lại.", ct);
        }

        // Tạo phiên mới
        var sessionId = string.IsNullOrWhiteSpace(request.SessionId)
            ? Guid.NewGuid().ToString()
            : request.SessionId;

        var session = new ParkingSession
        {
            SessionId     = sessionId,
            CarPlate      = request.CarPlate.Trim().ToUpperInvariant(),
            ParkingLotId  = request.ParkingLotId,
            Status        = ParkingSessionStatus.PendingSlot,
            EntryImageUrl = request.EntryImageUrl,
            FuelType      = request.FuelType,
            CarColor      = request.CarColor,
            CarModel      = request.CarModel,
            GateStaffId   = request.TriggeredBy,
            EnteredAtUtc  = request.EventTimestampUtc ?? DateTimeOffset.UtcNow
        };

        await _sessionRepo.AddAsync(session, ct);
        await WriteEventLogAsync(request, sessionId, EventProcessingStatus.Accepted, null, ct);

        return new IngestEventResponse
        {
            Success          = true,
            ProcessingStatus = EventProcessingStatus.Accepted,
            EventId          = request.EventId,
            SessionId        = sessionId,
            SessionStatus    = ParkingSessionStatus.PendingSlot,
            Message          = $"Xe '{request.CarPlate}' đã vào cổng. Phiên {sessionId} đang chờ gán slot."
        };
    }

    // ───────────────────────────────────────────────────────────────────────
    // HANDLER: SensorTriggered – IoT cảm biến siêu âm phát hiện xe tại ô
    // ───────────────────────────────────────────────────────────────────────
    private async Task<IngestEventResponse> HandleSensorTriggeredAsync(
        IngestEventRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.SlotCode))
            return await RejectAndLogAsync(request, "SlotCode bắt buộc với SensorTriggered.", ct);

        // Chỉ ghi log, không gán phiên – Staff khu vực sẽ gán thủ công qua SlotParked
        await WriteEventLogAsync(request, request.SessionId, EventProcessingStatus.Accepted, null, ct);

        return new IngestEventResponse
        {
            Success          = true,
            ProcessingStatus = EventProcessingStatus.Accepted,
            EventId          = request.EventId,
            SessionId        = request.SessionId,
            Message          = $"Cảm biến ô '{request.SlotCode}' phát hiện có xe. Chờ Staff khu vực xác nhận."
        };
    }

    // ───────────────────────────────────────────────────────────────────────
    // HANDLER: SlotParked – Staff khu vực gán xe vào ô
    // ───────────────────────────────────────────────────────────────────────
    private async Task<IngestEventResponse> HandleSlotParkedAsync(
        IngestEventRequest request, CancellationToken ct)
    {
        // Validate bắt buộc
        if (string.IsNullOrWhiteSpace(request.SessionId))
            return await RejectAndLogAsync(request, "SessionId bắt buộc với SlotParked.", ct);
        if (string.IsNullOrWhiteSpace(request.SlotCode))
            return await RejectAndLogAsync(request, "SlotCode bắt buộc với SlotParked.", ct);

        // Kiểm tra phiên tồn tại
        var session = await _sessionRepo.FindBySessionIdAsync(request.SessionId, ct);
        if (session is null)
            return await RejectAndLogAsync(request,
                $"Không tìm thấy phiên '{request.SessionId}'.", ct);

        if (session.Status != ParkingSessionStatus.PendingSlot)
            return await RejectAndLogAsync(request,
                $"Phiên '{request.SessionId}' đang ở trạng thái '{session.Status}' – không thể gán slot.", ct);

        // Kiểm tra tương thích loại xe vs loại ô (cảnh báo, không chặn)
        string? warningCode    = null;
        string? warningMessage = null;

        var slot = await _slotRepo.GetByCodeAsync(request.ParkingLotId, request.SlotCode, ct);
        if (slot is not null && !string.IsNullOrWhiteSpace(session.FuelType))
        {
            bool isEvSlot = slot.VehicleType.Equals("ev", StringComparison.OrdinalIgnoreCase);
            bool isEvCar  = session.FuelType.Equals("electric", StringComparison.OrdinalIgnoreCase);

            if (isEvSlot != isEvCar)
            {
                warningCode    = ParkingSessionWarning.WrongVehicleType;
                warningMessage = isEvSlot
                    ? $"Ô '{request.SlotCode}' dành cho xe điện nhưng xe '{session.CarPlate}' là xe xăng."
                    : $"Ô '{request.SlotCode}' dành cho xe xăng nhưng xe '{session.CarPlate}' là xe điện.";
            }
        }

        // Cập nhật phiên → parked
        session.SlotCode    = request.SlotCode;
        session.Status      = ParkingSessionStatus.Parked;
        session.WarningCode = warningCode;
        session.ZoneStaffId = request.TriggeredBy;
        session.ParkedAtUtc = request.EventTimestampUtc ?? DateTimeOffset.UtcNow;

        await _sessionRepo.UpdateAsync(session, ct);
        await WriteEventLogAsync(request, request.SessionId, EventProcessingStatus.Accepted, null, ct);

        return new IngestEventResponse
        {
            Success          = true,
            ProcessingStatus = EventProcessingStatus.Accepted,
            EventId          = request.EventId,
            SessionId        = request.SessionId,
            SessionStatus    = ParkingSessionStatus.Parked,
            WarningCode      = warningCode,
            WarningMessage   = warningMessage,
            Message          = warningMessage is not null
                ? $"⚠️ Xe '{session.CarPlate}' đã đậu tại ô '{request.SlotCode}' – có cảnh báo."
                : $"Xe '{session.CarPlate}' đã đậu tại ô '{request.SlotCode}'."
        };
    }

    // ───────────────────────────────────────────────────────────────────────
    // HANDLER: VehicleExited – Xe ra cổng
    // ───────────────────────────────────────────────────────────────────────
    private async Task<IngestEventResponse> HandleVehicleExitedAsync(
        IngestEventRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.SessionId))
            return await RejectAndLogAsync(request, "SessionId bắt buộc với VehicleExited.", ct);

        var session = await _sessionRepo.FindBySessionIdAsync(request.SessionId, ct);
        if (session is null)
            return await RejectAndLogAsync(request,
                $"Không tìm thấy phiên '{request.SessionId}'.", ct);

        if (session.Status == ParkingSessionStatus.Exited)
            return await RejectAndLogAsync(request,
                $"Phiên '{request.SessionId}' đã đóng trước đó – xe không thể ra lần hai.", ct);

        // Đóng phiên – việc thu tiền và giải phóng slot do API của Phụng đảm nhận
        session.Status       = ParkingSessionStatus.Exited;
        session.ExitImageUrl = request.ExitImageUrl;
        session.ExitedAtUtc  = request.EventTimestampUtc ?? DateTimeOffset.UtcNow;

        await _sessionRepo.UpdateAsync(session, ct);
        await WriteEventLogAsync(request, request.SessionId, EventProcessingStatus.Accepted, null, ct);

        return new IngestEventResponse
        {
            Success          = true,
            ProcessingStatus = EventProcessingStatus.Accepted,
            EventId          = request.EventId,
            SessionId        = request.SessionId,
            SessionStatus    = ParkingSessionStatus.Exited,
            Message          = $"Xe '{session.CarPlate}' đã ra khỏi bãi. Phiên '{request.SessionId}' đã đóng."
        };
    }

    // ───────────────────────────────────────────────────────────────────────
    // HELPERS
    // ───────────────────────────────────────────────────────────────────────
    private async Task<IngestEventResponse> RejectAndLogAsync(
        IngestEventRequest request, string reason, CancellationToken ct)
    {
        await WriteEventLogAsync(request, request.SessionId, EventProcessingStatus.Rejected, reason, ct);

        return new IngestEventResponse
        {
            Success          = false,
            ProcessingStatus = EventProcessingStatus.Rejected,
            EventId          = request.EventId,
            SessionId        = request.SessionId,
            Message          = reason
        };
    }

    private Task WriteEventLogAsync(
        IngestEventRequest request,
        string?            sessionId,
        string             status,
        string?            rejectReason,
        CancellationToken  ct)
    {
        var log = new ParkingEventLog
        {
            EventId            = request.EventId,
            SessionId          = sessionId,
            ParkingLotId       = request.ParkingLotId,
            SlotCode           = request.SlotCode,
            CarPlate           = request.CarPlate ?? string.Empty,
            EventType          = request.EventType,
            Source             = request.Source,
            TriggeredBy        = request.TriggeredBy,
            ProcessingStatus   = status,
            RejectReason       = rejectReason,
            EventTimestampUtc  = request.EventTimestampUtc ?? DateTimeOffset.UtcNow,
            ReceivedAtUtc      = DateTimeOffset.UtcNow
        };

        return _eventLogRepo.AddAsync(log, ct);
    }
}
