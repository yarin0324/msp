using Basket.Core.Entities;
using Basket.Core.Interfaces;
using Common.Security;
using Microsoft.AspNetCore.Mvc;

namespace Basket.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Route("[controller]")]
    public class BasketController : ControllerBase
    {
        private readonly IBasketRepository _basketRepository;
        private readonly ICurrentUserContext _currentUserContext;
        private readonly ILogger<BasketController> _logger;

        public BasketController(
            IBasketRepository basketRepository, 
            ICurrentUserContext currentUserContext,
            ILogger<BasketController> logger)
        {
            _basketRepository = basketRepository;
            _currentUserContext = currentUserContext;
            _logger = logger;
        }

        [HttpGet("{customerId?}")]
        public async Task<ActionResult<CustomerBasket>> GetBasket(string? customerId = null)
        {
            var targetCustomerId = customerId ?? _currentUserContext.UserId;
            if (string.IsNullOrEmpty(targetCustomerId))
            {
                return BadRequest(new { message = "CustomerId is required or user must be authenticated." });
            }

            var basket = await _basketRepository.GetBasketAsync(targetCustomerId);
            return Ok(basket ?? new CustomerBasket(targetCustomerId));
        }

        [HttpPost]
        public async Task<ActionResult<CustomerBasket>> UpdateBasket([FromBody] CustomerBasket basket)
        {
            if (string.IsNullOrEmpty(basket.CustomerId) && !string.IsNullOrEmpty(_currentUserContext.UserId))
            {
                basket.CustomerId = _currentUserContext.UserId;
            }

            if (string.IsNullOrEmpty(basket.CustomerId))
            {
                return BadRequest(new { message = "CustomerId is required." });
            }

            var updatedBasket = await _basketRepository.UpdateBasketAsync(basket);
            if (updatedBasket == null)
            {
                return StatusCode(500, new { message = "Failed to update basket in Redis." });
            }

            return Ok(updatedBasket);
        }

        [HttpDelete("{customerId?}")]
        public async Task<IActionResult> DeleteBasket(string? customerId = null)
        {
            var targetCustomerId = customerId ?? _currentUserContext.UserId;
            if (string.IsNullOrEmpty(targetCustomerId))
            {
                return BadRequest(new { message = "CustomerId is required." });
            }

            var result = await _basketRepository.DeleteBasketAsync(targetCustomerId);
            return Ok(new { success = result, customerId = targetCustomerId });
        }
    }
}
