using InventoryService.Application.Commands;
using InventoryService.Application.Events;
using InventoryService.Domain.Common;
using InventoryService.Domain.Interfaces.Events;
using InventoryService.Domain.Interfaces.Repositories;
using MediatR;
using System.Net;

namespace InventoryService.Application.Handlers
{
    /// <summary>
    /// 扣減庫存處理
    /// </summary>
    public class DeductInventoryHandler : IRequestHandler<DeductInventoryCommand, Result<bool>>
    {
        private readonly IInventoryRepository _inventoryRepository;
        private readonly IEventPublisher _eventPublisher;

        public DeductInventoryHandler(IInventoryRepository inventoryRepository, IEventPublisher eventPublisher)
        {
            this._inventoryRepository = inventoryRepository;
            this._eventPublisher = eventPublisher;
        }

        public async Task<Result<bool>> Handle(DeductInventoryCommand request, CancellationToken cancellationToken)
        {
            var inventory = await _inventoryRepository.GetByProductIdAsync(request.ProductId);

            if (inventory == null)
            {
                return Result<bool>.Failure(nameof(HttpStatusCode.InternalServerError), $"Inventory for product {request.ProductId} not found.");
            }

            if (inventory.Quantity < request.Quantity)
            {
                await _eventPublisher.PublishAsync(new InventoryDeductedFailedEvent
                {
                    OrderId = request.OrderId,
                    Reason = $"Insufficient inventory for product {request.ProductId}. Available: {inventory.Quantity}, Requested: {request.Quantity}."
                });

                return Result<bool>.Failure(nameof(HttpStatusCode.InternalServerError), $"Insufficient inventory for product {request.ProductId}. Available: {inventory.Quantity}, Requested: {request.Quantity}.");
            }

            inventory.Quantity -= request.Quantity;
            inventory.UpdateTime = DateTime.Now;

            await _inventoryRepository.UpdateAsync(inventory);

            await _eventPublisher.PublishAsync(new InventoryDeductedEvent
            {
                OrderId = request.OrderId,
                ProductId = request.ProductId,
                Quantity = request.Quantity,
            });

            return Result<bool>.Success(true);
        }
    }
}
