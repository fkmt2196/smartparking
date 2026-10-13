using Parking.Domain.Entities;

namespace Parking.Application.Common.Interfaces;

public interface IParkingLotRepository
{
    Task<IEnumerable<ParkingLot>> GetAllAsync();
}
