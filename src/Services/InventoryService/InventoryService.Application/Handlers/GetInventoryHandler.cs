using InventoryService.Application.Dtos;
using InventoryService.Application.Queries;
using InventoryService.Domain.Common;
using InventoryService.Domain.Interfaces.Repositories;
using MediatR;

namespace InventoryService.Application.Handlers
{
    public class GetInventoryHandler : IRequestHandler<GetInventoryQuery, Result<InventoryResponseDto>>
    {
        private readonly IInventoryRepository _inventoryRepository;

        public GetInventoryHandler(IInventoryRepository inventoryRepository)
        {
            this._inventoryRepository = inventoryRepository;
        }

        public Task<Result<InventoryResponseDto>> Handle(GetInventoryQuery request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
