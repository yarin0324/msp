using MediatR;
using OrderService.Application.Dtos;
using OrderService.Domain.Common;

namespace OrderService.Application.Queries
{
    public class GetOrderQuery : IRequest<Result<OrderResponseDto>>
    {
        public long OrderId { get; set; }
    }
}
