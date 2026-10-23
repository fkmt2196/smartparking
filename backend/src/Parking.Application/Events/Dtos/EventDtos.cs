using System.ComponentModel.DataAnnotations;
using Parking.Domain.Entities;

namespace Parking.Application.Events.Dtos;

// ─────────────────────────────────────────────────────────────────────────────
// INPUT DTOs – dùng nhận request từ client (Camera, IoT Sensor, Staff App)
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Payload chuẩn để gửi sự kiện vào Ingestion API.
/// Client tạo <see cref="EventId"/> bằng UUID v4 cho mỗi request mới.
/// Gửi lại cùng EventId sẽ được nhận diện trùng và bỏ qua – không tạo phiên thứ hai.
/// </summary>
public sealed class IngestEventRequest
{
    /// <summary>
    /// UUID v4 do client sinh – khóa idempotency, ví dụ: "c4a8b792-7b2d-4e9b-9c6a-4f51950d87a1".
    /// Mỗi lần bấm nút/gửi request MỚI phải sinh UUID MỚI.
    /// Nếu mạng lag và gửi lại → dùng đúng UUID cũ để chống duplicate.
    /// </summary>
    [Required]
    public string EventId { get; init; } = string.Empty;

    /// <summary>
    /// Mã phiên gửi xe (UUID v4) – do client sinh khi xe VÀO CỔNG lần đầu.
    /// Tất cả sự kiện trong cùng 1 lượt gửi xe (Entered → SlotParked → Exited) dùng chung 1 SessionId.
    /// Required với SlotParked và VehicleExited; có thể null với VehicleEntered (server sẽ tự tạo).
    /// </summary>
    public string? SessionId { get; init; }

    /// <summary>ID bãi đỗ xe, phải > 0.</summary>
    [Required, Range(1, int.MaxValue, ErrorMessage = "ParkingLotId phải lớn hơn 0.")]
    public int ParkingLotId { get; init; }

    /// <summary>
    /// Mã ô đậu xe – bắt buộc với SlotParked, không cần với VehicleEntered/VehicleExited.
    /// Format: [Khu][Cột]-[Số ô] – ví dụ: "A1-012", "B2-005", "C3-099".
    /// </summary>
    [RegularExpression(@"^[A-Z]\d{1,2}-\d{3}$",
        ErrorMessage = "SlotCode không đúng định dạng. Ví dụ hợp lệ: A1-012, B2-005.")]
    public string? SlotCode { get; init; }

    /// <summary>
    /// Biển số xe theo chuẩn Việt Nam, ví dụ: "51F-67890", "30A-11111".
    /// Bắt buộc với VehicleEntered và VehicleExited; optional với SensorTriggered.
    /// </summary>
    public string? CarPlate { get; init; }

    /// <summary>
    /// Loại sự kiện – dùng hằng số trong <see cref="ParkingEventType"/>:
    /// VehicleEntered | SensorTriggered | SlotParked | VehicleExited.
    /// </summary>
    [Required]
    public string EventType { get; init; } = string.Empty;

    /// <summary>
    /// Nguồn gốc sự kiện, ví dụ: "CameraGate01", "IoTSensor_A1-012", "GateStaff", "ZoneStaff".
    /// </summary>
    [Required]
    public string Source { get; init; } = string.Empty;

    /// <summary>ID nhân viên kích hoạt sự kiện (nếu là thao tác của Staff).</summary>
    public string? TriggeredBy { get; init; }

    /// <summary>Thời điểm sự kiện thực tế xảy ra (UTC). Null → server dùng thời gian nhận.</summary>
    public DateTimeOffset? EventTimestampUtc { get; init; }

    // ── Thông tin AI (chỉ có với VehicleEntered – do Gemini phân tích ảnh cổng vào) ──

    /// <summary>URL ảnh xe do Camera chụp tại cổng vào, đã upload lên Object Storage.</summary>
    public string? EntryImageUrl { get; init; }

    /// <summary>Loại nhiên liệu Gemini nhận diện: "gasoline" | "electric" | "unknown".</summary>
    public string? FuelType { get; init; }

    /// <summary>Màu xe Gemini nhận diện, ví dụ: "white", "black", "silver".</summary>
    public string? CarColor { get; init; }

    /// <summary>Hãng/Model xe Gemini nhận diện, ví dụ: "Toyota Vios", "VinFast VF8".</summary>
    public string? CarModel { get; init; }

    /// <summary>URL ảnh xe tại cổng ra – chỉ dùng với VehicleExited để đối chiếu.</summary>
    public string? ExitImageUrl { get; init; }
}

// ─────────────────────────────────────────────────────────────────────────────
// OUTPUT DTOs – trả về client sau khi xử lý
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Kết quả xử lý sự kiện trả về cho client.</summary>
public sealed class IngestEventResponse
{
    /// <summary>true nếu sự kiện được tiếp nhận (accepted hoặc duplicate).</summary>
    public bool Success { get; init; }

    /// <summary>Mô tả kết quả bằng tiếng Việt, hiển thị lên Staff App.</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>Trạng thái xử lý: "accepted" | "rejected" | "duplicate".</summary>
    public string ProcessingStatus { get; init; } = string.Empty;

    /// <summary>true khi sự kiện đã được xử lý từ trước (cùng EventId).</summary>
    public bool IsDuplicate { get; init; }

    /// <summary>EventId của request – để client tra soát.</summary>
    public string EventId { get; init; } = string.Empty;

    /// <summary>SessionId đang hoạt động sau khi xử lý.</summary>
    public string? SessionId { get; init; }

    /// <summary>Trạng thái phiên sau khi xử lý: "pending_slot" | "parked" | "exited".</summary>
    public string? SessionStatus { get; init; }

    /// <summary>
    /// Cảnh báo nghiệp vụ nếu có, ví dụ: "wrong_vehicle_type" khi xe xăng đậu ô điện.
    /// </summary>
    public string? WarningCode { get; init; }

    /// <summary>Thông báo cảnh báo dạng text để hiển thị trực tiếp.</summary>
    public string? WarningMessage { get; init; }

    /// <summary>Thời điểm server xử lý (UTC).</summary>
    public DateTimeOffset ProcessedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>Thông tin tóm tắt một phiên đang hoạt động – dùng cho màn hình danh sách.</summary>
public sealed class ActiveSessionDto
{
    public string SessionId { get; init; } = string.Empty;
    public string CarPlate { get; init; } = string.Empty;
    public int ParkingLotId { get; init; }
    public string? SlotCode { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? WarningCode { get; init; }
    public string? FuelType { get; init; }
    public string? CarColor { get; init; }
    public string? CarModel { get; init; }
    public string? EntryImageUrl { get; init; }
    public DateTimeOffset EnteredAtUtc { get; init; }
    public DateTimeOffset? ParkedAtUtc { get; init; }
}

/// <summary>Bản ghi sự kiện trong nhật ký – dùng cho màn hình giám sát và debug.</summary>
public sealed class EventLogDto
{
    public long Id { get; init; }
    public string EventId { get; init; } = string.Empty;
    public string? SessionId { get; init; }
    public int ParkingLotId { get; init; }
    public string? SlotCode { get; init; }
    public string CarPlate { get; init; } = string.Empty;
    public string EventType { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public string? TriggeredBy { get; init; }
    public string ProcessingStatus { get; init; } = string.Empty;
    public string? RejectReason { get; init; }
    public DateTimeOffset EventTimestampUtc { get; init; }
    public DateTimeOffset ReceivedAtUtc { get; init; }
}
