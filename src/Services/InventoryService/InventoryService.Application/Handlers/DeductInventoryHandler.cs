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
            var isDeducted = await _inventoryRepository.DeductStockAsync(request.ProductId, request.Quantity, DateTime.UtcNow);

            if (!isDeducted)
            {
                var failureReason = $"Insufficient inventory or product not found for product {request.ProductId}. Requested: {request.Quantity}.";

                await _eventPublisher.PublishAsync(new InventoryDeductedFailedEvent
                {
                    OrderId = request.OrderId,
                    ProductId = request.ProductId,
                    RequestedQuantity = request.Quantity,
                    Reason = failureReason
                });

                return Result<bool>.Failure(nameof(HttpStatusCode.BadRequest), failureReason);
            }

            await _eventPublisher.PublishAsync(new InventoryDeductedEvent
            {
                OrderId = request.OrderId,
                ProductId = request.ProductId,
                Quantity = request.Quantity
            });

            return Result<bool>.Success(true);
        }
    }
}
