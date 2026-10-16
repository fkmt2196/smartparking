using Parking.Application.Common.Interfaces;
using Parking.Domain.Entities;

namespace Parking.Application.Services;

public class ParkingLotService
{
    private readonly IParkingLotRepository _repository;

    public ParkingLotService(IParkingLotRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<ParkingLot>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<ParkingLot?> GetByIdAsync(int id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<bool> UpdateAvailableSlotsAsync(int id, int delta)
    {
        return await _repository.UpdateAvailableSlotsAsync(id, delta);
    }

    public async Task<bool> ResetToDefaultAsync()
    {
        return await _repository.ResetToDefaultAsync();
    }
}
