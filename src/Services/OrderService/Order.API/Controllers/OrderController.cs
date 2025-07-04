using Microsoft.AspNetCore.Mvc;
using OrderService.Domain.Common;
using OrderService.WebApi.DTOs;
using OrderService.WebApi.Services;

namespace OrderService.WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly OrderServiceFacade _orderServiceFacade;

        public OrderController(OrderServiceFacade orderServiceFacade)
        {
            this._orderServiceFacade = orderServiceFacade;
        }
        
        [HttpPost(Name = "CreateOrder")]
        public async Task<IActionResult> CreateOrder(OrderCreationDto? creation)
        {
            if(!ModelState.IsValid)
                return BadRequest(Result<ActionResult>.Failure("Invalid model state."));

            var result = await _orderServiceFacade.CreateOrderAsync(creation);

            if (!result.IsSuccess)
            {
                return BadRequest(new { errors = result.Message });
            }

            return Ok(result);
        }

        [HttpGet(Name = "GetOrder")]
        public async Task<IActionResult> GetOrder(long id)
        {
            var result = await _orderServiceFacade.GetOrderAsync(id);

            if (!result.IsSuccess)
            {
                return BadRequest(new { errors = result.Message });
            }

            return Ok(result);
        }
    }
}