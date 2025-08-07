using FluentValidation;
using InventoryService.Application.Commands;
using InventoryService.WebApi.Dtos;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InventoryService.WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DeductInventoryController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IValidator<CheckInventoryRequestDto> _validator;

        public DeductInventoryController(IMediator mediator)
        {
            this._mediator = mediator;
        }

        [HttpPost(Name = "Deduct")]
        public async Task<IActionResult> DeductInventory(DeductInventoryRequestDto request)
        {
            // 透過 Mediator
            var result = await _mediator.Send(new DeductInventoryCommand
            {
                //OrderId = request.OrderId,
                ProductId = request.ProductId,
                Quantity = request.Quantity
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