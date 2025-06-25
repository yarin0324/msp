using Order.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Order.Core.Interfaces
{
    public interface ICreateOrderUseCase
    {
        Task<ProductOrder> ExecuteAsync(decimal amount);
    }
}
