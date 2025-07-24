using InventoryService.Application.Commands;
using InventoryService.Application.Dtos;
using InventoryService.Domain.Common;
using InventoryService.Domain.Interfaces.Events;
using InventoryService.Domain.Interfaces.Repositories;
using MediatR;

namespace InventoryService.Application.Handlers
{
    /// <summary>
    /// 檢查庫存處理
    /// </summary>
    public class CheckInventoryHandler : IRequestHandler<CreateInventoryCommand, Result<InventoryResponseDto>>
    {
        private readonly IInventoryRepository _inventoryRepository;
        private readonly IEventPublisher _eventPublisher;

        public CheckInventoryHandler(IInventoryRepository inventoryRepository, IEventPublisher eventPublisher)
        {
            this._inventoryRepository = inventoryRepository;
            this._eventPublisher = eventPublisher;
        }


        public Task<Result<InventoryResponseDto>> Handle(CreateInventoryCommand request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
