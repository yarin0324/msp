namespace InventoryService.WebApi.Dtos
{
    public class DeductInventoryRequestDto
    {
        public long OrderId { get; set; }
        public string ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
