using InventoryService.Application.Commands;
using InventoryService.Domain.Common;
using InventoryService.Domain.Interfaces.Repositories;
using MediatR;
using System.Net;

namespace InventoryService.Application.Handlers
{
    /// <summary>
    /// 檢查庫存處理
    /// </summary>
    public class CheckInventoryHandler : IRequestHandler<CheckInventoryCommand, Result<bool>>
    {
        private readonly IInventoryRepository _inventoryRepository;

        public CheckInventoryHandler(IInventoryRepository inventoryRepository)
        {
            this._inventoryRepository = inventoryRepository;
        }

        public async Task<Result<bool>> Handle(CheckInventoryCommand request, CancellationToken cancellationToken)
        {
            var inventory = await _inventoryRepository.GetByProductIdAsync(request.ProductId);

            return inventory == null ? 
                Result<bool>.Failure(nameof(HttpStatusCode.InternalServerError), $"Inventory for product {request.ProductId} not found.") : 
                Result<bool>.Success(inventory.Quantity >= request.Quantity);
        }
    }
}
