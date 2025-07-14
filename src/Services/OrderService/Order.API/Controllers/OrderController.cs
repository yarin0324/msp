using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderService.Application.Commands;
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
        private readonly IValidator<CreateOrderRequestDto> _validator;

        public OrderController(OrderFacade orderApiService, IMediator mediator, IValidator<CreateOrderRequestDto> validator)
        {
            this._mediator = mediator;
            this._orderApiService = orderApiService;
            this._validator = validator;
        }
        
        [HttpPost(Name = "CreateOrder")]
        public async Task<IActionResult> CreateOrder(CreateOrderRequestDto? creation)
        {
            //if (!ModelState.IsValid)
            //    return BadRequest(Result<ActionResult>.Failure("Invalid model state."));

            // 手動驗證範例
            //var validationResult = await _validator.ValidateAsync(creation);

            //if (validationResult.IsValid is false)
            //{
            //    var errorMessages = validationResult.Errors.Select(e => e.ErrorMessage);
            //    var resultMessage = string.Join(",", errorMessages);
            //    return BadRequest(Result<ActionResult>.Failure(resultMessage));
            //}

            // 透過 Mediator
            var result = await _mediator.Send(OrderMappers.ToCommand(creation));

            // 透過 Use Case
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
            // 透過 Mediator
            var result = await _mediator.Send(new GetOrderQuery
            {
                OrderId = id
            });

            // 透過 Use Case
            //var result = await _orderApiService.GetOrderAsync(id);

            if (!result.IsSuccess)
            {
                return BadRequest(new { errors = result.Message });
            }

            return Ok(result);
        }
    }
}