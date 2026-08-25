using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Contracts
{
    public interface IInventoryDeductedEvent
    {
        long OrderId { get; set; }
        string ProductId { get; set; }
        int Quantity { get; set; }
    }
}
