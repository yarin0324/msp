using FluentValidation;
using OrderService.Application.Commands;

namespace OrderService.Application.Validators
{
    public class CreateOrderCommandTestValidator : AbstractValidator<CreateOrderCommandTest>
    {
        public CreateOrderCommandTestValidator()
        {
            RuleFor(command => command.CustomerId).NotEmpty().WithMessage("Customer ID is required.");

            RuleFor(command => command.Items).NotEmpty().WithMessage("Order must contain at least on item.");

            RuleForEach(command => command.Items).ChildRules(item =>
            {
                item.RuleFor(i => i.ProductId).NotEmpty().WithMessage("Item product ID is required.");
                item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("Item quantity must be greater than zero.");
            });
        }
    }
}
