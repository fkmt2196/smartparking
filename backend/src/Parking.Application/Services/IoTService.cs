using System.Collections.Concurrent;
using Parking.Application.Common.Interfaces;
using Parking.Application.IoT;
using Parking.Domain.Entities;

namespace Parking.Application.Services;

public class IoTService
{
    private readonly IParkingLotRepository _repository;
    private static readonly ConcurrentQueue<IoTTelemetryDto> _telemetryHistory = new();
    private const int MaxHistory = 100;

    public IoTService(IParkingLotRepository repository)
    {
        _repository = repository;
    }

    public async Task<IoTEventResultDto> ProcessEventAsync(IoTEventDto dto)
    {
        if (dto.ParkingLotId <= 0)
        {
            return new IoTEventResultDto
            {
                Success = false,
                Message = "ParkingLotId không hợp lệ.",
                ProcessedAt = DateTimeOffset.UtcNow
            };
        }

        var lot = await _repository.GetByIdAsync(dto.ParkingLotId);
        if (lot == null)
        {
            return new IoTEventResultDto
            {
                Success = false,
                Message = $"Không tìm thấy bãi đỗ xe ID={dto.ParkingLotId}.",
                ProcessedAt = DateTimeOffset.UtcNow
            };
        }

        int delta = 0;
        string normalizedType = (dto.EventType ?? "CarParked").Trim();

        if (normalizedType.Equals("CarParked", StringComparison.OrdinalIgnoreCase) ||
            normalizedType.Equals("CheckIn", StringComparison.OrdinalIgnoreCase))
        {
            if (lot.AvailableSlots <= 0)
            {
                return new IoTEventResultDto
                {
                    Success = false,
                    Message = $"Bãi '{lot.Name}' đã hết chỗ trống (0 chỗ)!",
                    DeviceId = dto.DeviceId,
                    ParkingLotId = lot.Id,
                    ParkingLotName = lot.Name,
                    AvailableSlots = 0,
                    EventType = normalizedType,
                    ProcessedAt = DateTimeOffset.UtcNow
                };
            }
            delta = -1;
        }
        else if (normalizedType.Equals("CarDeparted", StringComparison.OrdinalIgnoreCase) ||
                 normalizedType.Equals("CheckOut", StringComparison.OrdinalIgnoreCase))
        {
            delta = 1;
        }
        else if (normalizedType.Equals("Fault", StringComparison.OrdinalIgnoreCase))
        {
            delta = 0;
        }

        if (delta != 0)
        {
            await _repository.UpdateAvailableSlotsAsync(dto.ParkingLotId, delta);
            lot = await _repository.GetByIdAsync(dto.ParkingLotId) ?? lot;
        }

        var telemetry = new IoTTelemetryDto
        {
            DeviceId = string.IsNullOrWhiteSpace(dto.DeviceId) ? $"SENSOR-LOT{lot.Id}-AUTO" : dto.DeviceId,
            ParkingLotId = lot.Id,
            ParkingLotName = lot.Name,
            SlotNumber = dto.SlotNumber ?? "Auto",
            EventType = normalizedType,
            Timestamp = dto.Timestamp ?? DateTimeOffset.UtcNow,
            Status = normalizedType.Equals("Fault", StringComparison.OrdinalIgnoreCase) ? "Fault" : "Normal"
        };

        _telemetryHistory.Enqueue(telemetry);
        while (_telemetryHistory.Count > MaxHistory)
        {
            _telemetryHistory.TryDequeue(out _);
        }

        return new IoTEventResultDto
        {
            Success = true,
            Message = delta < 0
                ? $"Xe vào bãi '{lot.Name}'. Số chỗ còn lại: {lot.AvailableSlots}."
                : delta > 0
                    ? $"Xe rời bãi '{lot.Name}'. Số chỗ còn lại: {lot.AvailableSlots}."
                    : $"Cảm biến '{telemetry.DeviceId}' ghi nhận lỗi (Fault).",
            DeviceId = telemetry.DeviceId,
            ParkingLotId = lot.Id,
            ParkingLotName = lot.Name,
            AvailableSlots = lot.AvailableSlots,
            EventType = normalizedType,
            ProcessedAt = DateTimeOffset.UtcNow
        };
    }

    public Task<IEnumerable<IoTTelemetryDto>> GetTelemetryHistoryAsync()
    {
        return Task.FromResult<IEnumerable<IoTTelemetryDto>>(_telemetryHistory.ToArray().Reverse());
    }

    public async Task<bool> ResetAllAsync()
    {
        _telemetryHistory.Clear();
        return await _repository.ResetToDefaultAsync();
    }
}
