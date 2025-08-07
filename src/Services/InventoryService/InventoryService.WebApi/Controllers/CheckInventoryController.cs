using FluentValidation;
using InventoryService.Application.Middleware.Exception;
using InventoryService.WebApi.Dtos;
using InventoryService.WebApi.Mappers;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InventoryService.WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CheckInventoryController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IValidator<CheckInventoryRequestDto> _validator;

        public CheckInventoryController(IMediator mediator)
        {
            this._mediator = mediator;
        }

        [HttpPost(Name = "Check")]
        public async Task<IActionResult> CheckInventory(CheckInventoryRequestDto? request)
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
            var result = await _mediator.Send(InventoryMappers.ToCommand(request));

            // 透過 Use Case
            //var result = await _orderApiService.CreateOrderAsync(creation);

            if (!result.IsSuccess)
            {
                var errorResponse = new ErrorResponse
                {
                    IsSuccess = false,
                    ErrorCode = result.ErrorCode ?? "Bad Request",
                    ErrorMessage = result.Message,
                    Details = result.Message
                };

                return BadRequest(errorResponse);
            }

            return Ok(result);
        }
    }
}