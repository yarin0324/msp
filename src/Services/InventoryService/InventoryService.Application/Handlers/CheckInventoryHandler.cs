using FluentValidation;
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
        private readonly IValidator<CheckInventoryCommand> _validator;

        public CheckInventoryHandler(IInventoryRepository inventoryRepository, IValidator<CheckInventoryCommand> validator)
        {
            this._inventoryRepository = inventoryRepository;
            this._validator = validator;
        }

        public async Task<Result<bool>> Handle(CheckInventoryCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request);

            if (validationResult.IsValid is false)
            {
                var errorMessages = validationResult.Errors.Select(e => e.ErrorMessage);
                return Result<bool>.Failure(nameof(HttpStatusCode.InternalServerError), $"{string.Join(",", errorMessages)}");
            }

            var inventory = await _inventoryRepository.GetByProductIdAsync(request.ProductId);

            return inventory == null ? 
                Result<bool>.Failure(nameof(HttpStatusCode.InternalServerError), $"Inventory for product {request.ProductId} not found.") : 
                Result<bool>.Success(inventory.Quantity >= request.Quantity);
        }
    }
}
