using SmartParking.Domain.Entities;
using SmartParking.Domain.Interfaces;

namespace SmartParking.Application.UseCases
{
    public class GetSlotStatusUseCase
    {
        private readonly IParkingRepository _repository;

        public GetSlotStatusUseCase(IParkingRepository repository)
        {
            _repository = repository;
        }

        public async Task<ParkingSlot> ExecuteAsync(string slotId)
        {
            return await _repository.GetSlotByIdAsync(slotId);
        }
    }
}
