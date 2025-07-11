using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderService.Application.Queries;
using OrderService.Domain.Common;
using OrderService.WebApi.DTOs;
using OrderService.WebApi.Facades;
using OrderService.WebApi.Mappers;

namespace OrderService.WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly OrderFacade _orderApiService;
        private readonly IMediator _mediator;

        public OrderController(OrderFacade orderApiService, IMediator mediator)
        {
            this._mediator = mediator;
            this._orderApiService = orderApiService;
        }
        
        [HttpPost(Name = "CreateOrder")]
        public async Task<IActionResult> CreateOrder(CreateOrderRequestDto? creation)
        {
            if(!ModelState.IsValid)
                return BadRequest(Result<ActionResult>.Failure("Invalid model state."));

            var result = await _mediator.Send(OrderMappers.ToCommand(creation));

            //var result = await _orderApiService.CreateOrderAsync(creation);

            if (!result.IsSuccess)
            {
                return BadRequest(new { errors = result.Message });
            }

            return Ok(result);
        }

        [HttpGet(Name = "GetOrder")]
        public async Task<IActionResult> GetOrder(long id)
        {
            //var result = await _orderApiService.GetOrderAsync(id);

            var result = await _mediator.Send(new GetOrderQuery
            {
                OrderId = id
            });

            if (!result.IsSuccess)
            {
                return BadRequest(new { errors = result.Message });
            }

            return Ok(result);
        }
    }
}