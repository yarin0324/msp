using MediatR;
using OrderService.Application.Dtos;
using OrderService.Domain.Common;

namespace OrderService.Application.Commands
{
    /// <summary>
    /// 作為CQRS Command，表示創建訂單的意圖
    /// </summary>
    public class CreateOrderCommandTest
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string CustomerId { get; set; }
        public DateTime CreateTime { get; set; }
        public List<OrderItemDto> Items { get; set; } = new List<OrderItemDto>();
    }
}
