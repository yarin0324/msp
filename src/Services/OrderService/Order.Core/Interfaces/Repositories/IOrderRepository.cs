using OrderService.Domain.Entities;

namespace OrderService.Domain.Interfaces.Repositories
{
    public interface IOrderRepository
    {
        /// <summary>
        /// 查詢訂單: 透過訂單ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<Order?> GetByIdAsync(long id);

        /// <summary>
        /// 新增訂單
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        Task<long> AddAsync(Order order);
    }
}
