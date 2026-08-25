using InventoryService.Application.Dtos;
using InventoryService.Domain.Common;
using MediatR;

namespace InventoryService.Application.Commands
{
    /// <summary>
    /// 扣減庫存
    /// </summary>
    public class DeductInventoryCommand : IRequest<Result<bool>>
    {
        public long OrderId { get; set; }
        public string ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
