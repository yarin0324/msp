using Order.Adapters.DTOs;
using Order.Core.Entities;

namespace Order.Adapters.Mappers
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
