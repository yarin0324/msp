using FluentResults;
using Microsoft.AspNetCore.Mvc;
using Order.Adapters.DTOs;
using Order.Adapters.Interfaces;
using Order.Adapters.Services;

namespace Order.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderAdapterService _orderAdapterService;

        public OrderController(IOrderAdapterService orderAdapterService)
        {
            this._orderAdapterService = orderAdapterService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder(OrderCreationDto? creation)
        {
            if(!ModelState.IsValid)
                return BadRequest(Result.Fail("Invalid model state.").Errors);

            var result = await _orderAdapterService.CreateOrderAsync(creation);

            if (!result.IsSuccess)
            {
                // 將錯誤訊息轉成陣列，以符合API回應格式
                return BadRequest(new { errors = result.Errors.Select(e => e.Message) });
            }

            return CreatedAtAction(nameof(CreateOrder), new { result.Value.Id }, result.Value);
        }

        public async Task<IActionResult> GetOrder(long id)
        {
            var result = await _orderAdapterService.GetOrderAsync(id);

            if (!result.IsSuccess)
            {
                return BadRequest(new { errors = result.Errors.Select(e => e.Message) });
            }

            return Ok(result);
        }
    }
}