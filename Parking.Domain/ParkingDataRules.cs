namespace Parking.Domain;

public static class ParkingDataRules
{
    // Quy ước tương thích Frontend hiện tại (Song): car = ô tô, moto = xe máy, ev = xe điện.
    // Chưa tách vehicle category và propulsion type; sẽ xem xét khi có yêu cầu nghiệp vụ.
    public static readonly string[] VehicleTypes = ["car", "moto", "ev"];

    public static readonly string[] SlotStatuses = ["available", "occupied", "reserved", "out_of_service"];
}
