using InventoryService.Application.Commands;
using InventoryService.WebApi.Dtos;

namespace InventoryService.WebApi.Mappers
{
    public static class InventoryMappers
    {
        public static CheckInventoryCommand ToCommand(CheckInventoryRequestDto inventory)
        {
            //TODO : 改AutoMapper
            if (inventory == null)
                throw new ArgumentNullException(nameof(inventory));

            return new CheckInventoryCommand
            {
                ProductId = inventory.ProductId,
                Quantity = inventory.Quantity
            };
        }
    }
}
