using Order.Core.Entities;

namespace Order.Core.Interfaces
{
    public interface IOrderRepository
    {
        /// <summary>
        /// 查詢訂單: 透過訂單ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<ProductOrder> GetByIdAsync(long id);

        /// <summary>
        /// 新增訂單
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        Task<ProductOrder> AddAsync(ProductOrder order);
    }
}
