using InventoryService.Application.Dtos;
using InventoryService.Domain.Common;
using MediatR;

namespace InventoryService.Application.Commands
{
    /// <summary>
    /// 作為CQRS Command，表示創建訂單的意圖
    /// </summary>
    public class CreateInventoryCommand : IRequest<Result<InventoryResponseDto>>
    {
        
    }
}
