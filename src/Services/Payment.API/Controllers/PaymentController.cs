using Common.Contracts;
using Common.Idempotency;
using MassTransit;
using Microsoft.AspNetCore.Mvc;

namespace Payment.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Route("[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(IPublishEndpoint publishEndpoint, ILogger<PaymentController> logger)
        {
            _publishEndpoint = publishEndpoint;
            _logger = logger;
        }

        [HttpPost("pay")]
        [Idempotent(Required = false, ExpiryHours = 24)]
        public async Task<IActionResult> ProcessPayment([FromBody] PaymentRequest request)
        {
            if (request.OrderId <= 0 || request.Amount <= 0)
            {
                return BadRequest(new { message = "Invalid OrderId or Amount." });
            }

            _logger.LogInformation("Processing payment for OrderId: {OrderId}, Amount: {Amount}", request.OrderId, request.Amount);

            // 模擬金流扣款成功
            var paymentId = $"PAY-{Guid.NewGuid():N}";
            var processedTime = DateTime.UtcNow;

            await _publishEndpoint.Publish<IPaymentProcessedEvent>(new PaymentProcessedEvent
            {
                OrderId = request.OrderId,
                PaymentId = paymentId,
                Amount = request.Amount,
                ProcessedTime = processedTime
            });

            _logger.LogInformation("Payment successful for OrderId: {OrderId}, PaymentId: {PaymentId}", request.OrderId, paymentId);

            return Ok(new
            {
                Success = true,
                PaymentId = paymentId,
                OrderId = request.OrderId,
                Amount = request.Amount,
                Status = "Completed",
                ProcessedTime = processedTime
            });
        }
    }

    public class PaymentRequest
    {
        public long OrderId { get; set; }
        public decimal Amount { get; set; }
        public string? PaymentMethod { get; set; } = "CreditCard";
    }

    public class PaymentProcessedEvent : IPaymentProcessedEvent
    {
        public long OrderId { get; set; }
        public string PaymentId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime ProcessedTime { get; set; }
    }
}
