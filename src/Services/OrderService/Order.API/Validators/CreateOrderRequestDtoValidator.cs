using FluentValidation;
using OrderService.WebApi.DTOs;

namespace OrderService.WebApi.Validators
{
    public class CreateOrderRequestDtoValidator : AbstractValidator<CreateOrderRequestDto>
    {
        public CreateOrderRequestDtoValidator()
        {
            RuleFor(dto => dto.CustomerId).NotEmpty().WithMessage("Customer ID is required.");

            RuleFor(dto => dto.Items).NotEmpty().WithMessage("Order must contain at least on item.");

            RuleForEach(dto => dto.Items).ChildRules(item =>
            {
                item.RuleFor(i => i.ProductId).NotEmpty().WithMessage("Item product ID is required.");
                item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("Item quantity must be greater than zero.");
            });
        }
    }
}
