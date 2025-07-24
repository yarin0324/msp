using InventoryService.Application.Commands;
using InventoryService.Application.Dtos;
using InventoryService.Domain.Common;
using InventoryService.Domain.Interfaces.Repositories;
using MediatR;

namespace InventoryService.Application.Handlers
{
    /// <summary>
    /// 扣減庫存處理
    /// </summary>
    public class DeductInventoryHandler : IRequestHandler<DeductInventoryCommand, Result<InventoryResponseDto>>
    {
        private readonly IInventoryRepository _inventoryRepository;

        public DeductInventoryHandler(IInventoryRepository inventoryRepository)
        {
            this._inventoryRepository = inventoryRepository;
        }

        public Task<Result<InventoryResponseDto>> Handle(DeductInventoryCommand request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
