using InventoryService.Application.Dtos;
using InventoryService.Domain.Common;
using MediatR;

namespace InventoryService.Application.Queries
{
    public class GetInventoryQuery : IRequest<Result<InventoryResponseDto>>
    {
        public long OrderId { get; set; }
    }
}
