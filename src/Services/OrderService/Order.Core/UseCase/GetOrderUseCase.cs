using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Order.Core.Entities;
using Order.Core.Interfaces;

namespace Order.Core.UseCase
{
    public class GetOrderUseCase : IGetOrderUseCase
    {
        private readonly IOrderRepository _orderRepository;

        public GetOrderUseCase(IOrderRepository orderRepository)
        {
            this._orderRepository = orderRepository;
        }

        public async Task<ProductOrder> ExecuteAsync(long id)
        {
            var order = await _orderRepository.GetByIdAsync(id);

            if (order == null)
            {
                throw new Exception($"Order with ID {id} does not exist.");
            }

            return order;
        }
    }
}
