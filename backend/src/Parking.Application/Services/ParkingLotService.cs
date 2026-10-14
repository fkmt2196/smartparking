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
}
