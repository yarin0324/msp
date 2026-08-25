using OrderService.Application.Commands;
using OrderService.Application.Dtos;
using OrderService.WebApi.DTOs;

namespace OrderService.WebApi.Mappers
{
    public static class OrderMappers
    {
        public static CreateOrderResponseDto ToCreateOrderResponseDto(OrderResponseDto order)
        {
            return new CreateOrderResponseDto
            {
                OrderId = order.OrderId
            };
        }

        public static GetOrderResponseDto ToGetOrderResponseDto(OrderResponseDto order)
        {
            return new GetOrderResponseDto
            {
                OrderId = order.OrderId,
                Amount = order.Amount,
                Currency = order.Currency,
                CustomerId = order.CustomerId,
                Status = order.Status,
                Items = order.Items.Select(i =>  new GetOrderItemResponseDto
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity
                }).ToList()
            };
        }

        public static CreateOrderCommand ToCommand(CreateOrderRequestDto order)
        {
            //TODO : 改AutoMapper
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            return new CreateOrderCommand
            {
                Amount = order.Amount!.Value,
                Currency = order.Currency,
                CustomerId = order.CustomerId,
                Items = order.Items.Select(i => new OrderItemDto
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity
                }).ToList()
            };
        }
    }
}
