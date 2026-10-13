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
}
