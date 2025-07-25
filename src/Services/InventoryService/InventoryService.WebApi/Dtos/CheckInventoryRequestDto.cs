namespace InventoryService.WebApi.Dtos
{
    public class CheckInventoryRequestDto
    {
        public string ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
