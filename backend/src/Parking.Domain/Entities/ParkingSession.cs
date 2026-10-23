namespace Parking.Domain.Entities;

/// <summary>
/// Phiên gửi xe – mỗi lượt xe vào/ra tạo ra 1 phiên.
/// Phiên được mở khi xe qua cổng vào (VehicleEntered) và đóng khi xe ra khỏi bãi (VehicleExited).
/// </summary>
public class ParkingSession
{
    public long Id { get; set; }

    /// <summary>Mã phiên duy nhất – UUID v4 do client sinh khi xe vào cổng.</summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>Biển số xe theo chuẩn Việt Nam, VD: "51F-67890".</summary>
    public string CarPlate { get; set; } = string.Empty;

    public int ParkingLotId { get; set; }

    /// <summary>
    /// Mã ô đậu xe theo định dạng [Khu][Cột]-[Số ô], VD: "A1-012", "B2-005".
    /// Null khi xe mới vào cổng, chưa được Staff gán slot.
    /// </summary>
    public string? SlotCode { get; set; }

    /// <summary>
    /// Trạng thái phiên hiện tại:
    /// pending_slot  – xe đã qua cổng vào, chưa được gán slot (chờ IoT cảm biến + Staff gán).
    /// parked        – Staff đã gán slot, xe đang đậu.
    /// exited        – xe đã ra khỏi bãi, phiên đóng.
    /// </summary>
    public string Status { get; set; } = ParkingSessionStatus.PendingSlot;

    /// <summary>
    /// Cảnh báo nghiệp vụ: null = không có vấn đề,
    /// "wrong_vehicle_type" = xe xăng đậu ô điện hoặc ngược lại.
    /// </summary>
    public string? WarningCode { get; set; }

    /// <summary>URL ảnh xe do Camera Gemini chụp lúc vào cổng (lưu trên Object Storage).</summary>
    public string? EntryImageUrl { get; set; }

    /// <summary>URL ảnh xe do Camera Gemini chụp lúc ra cổng – dùng để đối chiếu.</summary>
    public string? ExitImageUrl { get; set; }

    /// <summary>Loại nhiên liệu do Gemini AI nhận diện: "gasoline" | "electric" | "unknown".</summary>
    public string? FuelType { get; set; }

    /// <summary>Màu xe do Gemini AI nhận diện, VD: "white", "black".</summary>
    public string? CarColor { get; set; }

    /// <summary>Hãng/Model xe do Gemini AI nhận diện, VD: "Toyota Vios", "VinFast VF8".</summary>
    public string? CarModel { get; set; }

    /// <summary>Staff cổng vào thực hiện check-in.</summary>
    public string? GateStaffId { get; set; }

    /// <summary>Staff khu vực thực hiện gán slot.</summary>
    public string? ZoneStaffId { get; set; }

    public DateTimeOffset EnteredAtUtc { get; set; }
    public DateTimeOffset? ParkedAtUtc { get; set; }
    public DateTimeOffset? ExitedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>Hằng số trạng thái phiên gửi xe – dùng thay magic strings.</summary>
public static class ParkingSessionStatus
{
    public const string PendingSlot = "pending_slot";
    public const string Parked      = "parked";
    public const string Exited      = "exited";
}

/// <summary>Hằng số cảnh báo nghiệp vụ trên phiên.</summary>
public static class ParkingSessionWarning
{
    public const string WrongVehicleType = "wrong_vehicle_type";
}
