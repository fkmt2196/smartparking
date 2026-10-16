namespace Parking.Application.IoT;

public class IoTEventDto
{
    public string DeviceId { get; set; } = string.Empty;
    public int ParkingLotId { get; set; }
    public string? SlotNumber { get; set; }
    public string EventType { get; set; } = "CarParked"; // CarParked, CarDeparted, Fault
    public DateTimeOffset? Timestamp { get; set; }
}

public class IoTEventResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public int ParkingLotId { get; set; }
    public string ParkingLotName { get; set; } = string.Empty;
    public int AvailableSlots { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTimeOffset ProcessedAt { get; set; }
}

public class IoTTelemetryDto
{
    public string DeviceId { get; set; } = string.Empty;
    public int ParkingLotId { get; set; }
    public string ParkingLotName { get; set; } = string.Empty;
    public string? SlotNumber { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public string Status { get; set; } = "Normal"; // Normal, Fault
}
