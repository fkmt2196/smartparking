using System.Text.Json.Serialization;

namespace Parking.Domain.Entities;

// Entity biểu diễn thông tin một bãi đỗ xe.
// SAMPLE DATA / API contract tạm thời – chờ Thức cung cấp dữ liệu chính thức
// và Song xác nhận tên trường.
public class ParkingLot
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int AvailableSlots { get; set; }
    public decimal HourlyRate { get; set; }

    // Các field dưới đây dùng cho PostgreSQL, chưa trả về API JSON.
    [JsonIgnore]
    public string Code { get; set; } = string.Empty;

    [JsonIgnore]
    public string Address { get; set; } = string.Empty;

    [JsonIgnore]
    public decimal? Latitude { get; set; }

    [JsonIgnore]
    public decimal? Longitude { get; set; }

    [JsonIgnore]
    public bool IsActive { get; set; } = true;

    [JsonIgnore]
    public DateTimeOffset CreatedAtUtc { get; set; }

    [JsonIgnore]
    public DateTimeOffset UpdatedAtUtc { get; set; }

    [JsonIgnore]
    public ICollection<ParkingFloor> Floors { get; set; } = new List<ParkingFloor>();
}
