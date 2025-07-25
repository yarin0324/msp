using InventoryService.Application.Dtos;
using InventoryService.Domain.Common;
using MediatR;

namespace InventoryService.Application.Commands
{
    /// <summary>
    /// 作為CQRS Command，表示檢查庫存的意圖
    /// </summary>
    public class CheckInventoryCommand : IRequest<Result<bool>>
    {
        public string ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
