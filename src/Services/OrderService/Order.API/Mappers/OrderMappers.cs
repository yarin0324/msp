using OrderService.Domain.Entities;
using OrderService.WebApi.DTOs;

namespace OrderService.WebApi.Mappers
{
    public static class OrderMappers
    {
        public static OrderDto ToDto(ProductOrder order)
        {
            //TODO : 改AutoMapper
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            return new OrderDto
            {
                Id = order.Id,
                Amount = order.Amount,
                CreateTime = order.CreateTime
            };
        }
    }
}
