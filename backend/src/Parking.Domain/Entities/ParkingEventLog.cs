namespace Parking.Domain.Entities;

/// <summary>
/// Nhật ký tất cả sự kiện IoT/Staff gửi vào hệ thống.
/// Dùng để: truy vết, chống xử lý trùng (Idempotency), đối chiếu khi có tranh chấp.
/// Mỗi bản ghi là bất biến (append-only) – không cập nhật hay xóa sau khi tạo.
/// </summary>
public class ParkingEventLog
{
    public long Id { get; set; }

    /// <summary>
    /// Khóa idempotency – UUID v4 do phía gửi sinh.
    /// Ràng buộc UNIQUE tại DB: cùng EventId gửi nhiều lần chỉ xử lý 1 lần.
    /// </summary>
    public string EventId { get; set; } = string.Empty;

    /// <summary>
    /// Mã phiên gửi xe liên quan.
    /// Null chỉ khi sự kiện bị từ chối trước khi xác định được phiên.
    /// </summary>
    public string? SessionId { get; set; }

    public int ParkingLotId { get; set; }

    /// <summary>
    /// Mã ô liên quan (nếu có). Format: "A1-012".
    /// Null với sự kiện VehicleEntered vì xe chưa được gán slot.
    /// </summary>
    public string? SlotCode { get; set; }

    /// <summary>Biển số xe, VD: "51F-67890".</summary>
    public string CarPlate { get; set; } = string.Empty;

    /// <summary>
    /// Loại sự kiện:
    /// VehicleEntered – Xe qua cổng vào (Camera + Staff cổng xác nhận).
    /// SlotParked     – Staff khu vực gán xe vào ô cụ thể.
    /// VehicleExited  – Xe ra khỏi bãi (Staff cổng ra xác nhận + đối chiếu ảnh).
    /// SensorTriggered – Cảm biến siêu âm tại ô phát hiện có vật thể (chưa gán xe).
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Nguồn gốc sự kiện, VD: "CameraGate01", "IoTSensor_A1-012", "GateStaff", "ZoneStaff".
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>ID của nhân viên kích hoạt sự kiện (nếu là thao tác thủ công của Staff).</summary>
    public string? TriggeredBy { get; set; }

    /// <summary>
    /// Kết quả xử lý:
    /// accepted  – sự kiện hợp lệ, đã xử lý nghiệp vụ.
    /// rejected  – không hợp lệ (ví dụ: xe chưa check-in mà đã checkout).
    /// duplicate – trùng EventId, bỏ qua (trả lại kết quả cũ).
    /// </summary>
    public string ProcessingStatus { get; set; } = EventProcessingStatus.Accepted;

    /// <summary>Lý do từ chối – có giá trị khi ProcessingStatus = "rejected".</summary>
    public string? RejectReason { get; set; }

    /// <summary>Thời điểm sự kiện xảy ra thực tế (do thiết bị/client cung cấp).</summary>
    public DateTimeOffset EventTimestampUtc { get; set; }

    /// <summary>Thời điểm server nhận và ghi nhật ký.</summary>
    public DateTimeOffset ReceivedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>Hằng số trạng thái xử lý sự kiện.</summary>
public static class EventProcessingStatus
{
    public const string Accepted  = "accepted";
    public const string Rejected  = "rejected";
    public const string Duplicate = "duplicate";
}

/// <summary>Hằng số các loại sự kiện IoT/Staff.</summary>
public static class ParkingEventType
{
    /// <summary>Xe qua cổng vào – Camera chụp ảnh, Gemini phân tích, Staff cổng xác nhận.</summary>
    public const string VehicleEntered    = "VehicleEntered";

    /// <summary>Cảm biến siêu âm tại ô phát hiện có xe – chưa xác định xe nào.</summary>
    public const string SensorTriggered   = "SensorTriggered";

    /// <summary>Staff khu vực gán xe vào ô sau khi cảm biến báo.</summary>
    public const string SlotParked        = "SlotParked";

    /// <summary>Xe ra cổng – Staff đối chiếu ảnh + thu tiền + đóng phiên.</summary>
    public const string VehicleExited     = "VehicleExited";
}
