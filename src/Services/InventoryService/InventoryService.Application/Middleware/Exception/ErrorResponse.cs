namespace InventoryService.Application.Middleware.Exception
{
    public class ErrorResponse
    {
        public bool IsSuccess { get; set; }
        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public string Details { get; set; }
    }
}
