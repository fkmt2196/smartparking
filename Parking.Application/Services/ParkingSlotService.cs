using Parking.Application.Common.Interfaces;
using Parking.Domain.Entities;

namespace Parking.Application.Services;

public class ParkingSlotService
{
    private readonly IParkingSlotRepository _repository;

    public ParkingSlotService(IParkingSlotRepository repository)
    {
        _repository = repository;
    }

    public async Task<ParkingSlot?> GetSlotByIdAsync(int id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<ParkingSlot>> GetAllSlotsAsync()
    {
        return await _repository.GetAllAsync();
    }
}
