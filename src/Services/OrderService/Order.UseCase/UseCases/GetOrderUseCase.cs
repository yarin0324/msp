using OrderService.Domain.Common;
using OrderService.Domain.Entities;
using OrderService.Domain.Repositories;
using OrderService.Domain.UseCase;

namespace OrderService.Application.UseCases
{
    public class GetOrderUseCase : IGetOrderUseCase
    {
        private readonly IOrderRepository _orderRepository;

        public GetOrderUseCase(IOrderRepository orderRepository)
        {
            this._orderRepository = orderRepository;
        }

        public async Task<Result<ProductOrder>> ExecuteAsync(long id)
        {
            var order = await _orderRepository.GetByIdAsync(id);

            if (order == null)
            {
                throw new Exception($"Order with ID {id} does not exist.");
            }

            return Result<ProductOrder>.Success(order);
        }
    }
}
