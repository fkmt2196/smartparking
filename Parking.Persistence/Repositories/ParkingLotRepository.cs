using System.Text.Json;
using Parking.Application.Common.Interfaces;
using Parking.Domain.Entities;

namespace Parking.Persistence.Repositories;

public class ParkingLotRepository : IParkingLotRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private readonly string _jsonFilePath;

    public ParkingLotRepository(string jsonFilePath)
    {
        _jsonFilePath = jsonFilePath;
    }

    public async Task<IEnumerable<ParkingLot>> GetAllAsync()
    {
        if (!File.Exists(_jsonFilePath))
        {
            throw new InvalidOperationException("Không tìm thấy file dữ liệu bãi đỗ xe.");
        }

        string json;
        try
        {
            json = await File.ReadAllTextAsync(_jsonFilePath);
        }
        catch (IOException ex)
        {
            throw new InvalidOperationException("Không thể đọc file dữ liệu bãi đỗ xe.", ex);
        }

        List<ParkingLot>? parkingLots;
        try
        {
            parkingLots = JsonSerializer.Deserialize<List<ParkingLot>>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("File dữ liệu bãi đỗ xe không đúng định dạng JSON.", ex);
        }

        if (parkingLots is null || parkingLots.Count == 0)
        {
            throw new InvalidOperationException("File dữ liệu bãi đỗ xe không chứa danh sách hợp lệ.");
        }

        ValidateParkingLots(parkingLots);
        return parkingLots;
    }

    private static void ValidateParkingLots(List<ParkingLot> parkingLots)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var lot in parkingLots)
        {
            if (string.IsNullOrWhiteSpace(lot.Name))
            {
                throw new InvalidOperationException($"Bãi đỗ xe Id={lot.Id} có tên rỗng.");
            }

            if (!names.Add(lot.Name.Trim()))
            {
                throw new InvalidOperationException($"Tên bãi đỗ xe bị trùng: {lot.Name}.");
            }

            if (lot.AvailableSlots < 0)
            {
                throw new InvalidOperationException($"Bãi '{lot.Name}' có số chỗ trống âm.");
            }

            if (lot.HourlyRate <= 0)
            {
                throw new InvalidOperationException($"Bãi '{lot.Name}' có giá theo giờ không hợp lệ.");
            }
        }
    }
}
