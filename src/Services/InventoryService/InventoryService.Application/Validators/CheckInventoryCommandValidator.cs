using FluentValidation;
using InventoryService.Application.Commands;

namespace InventoryService.Application.Validators
{
    public class CheckInventoryCommandValidator : AbstractValidator<CheckInventoryCommand>
    {
        public CheckInventoryCommandValidator()
        {
            RuleFor(command => command.ProductId).NotEmpty().WithMessage("Product ID is required.");

            RuleFor(command => command.Quantity).GreaterThanOrEqualTo(1).WithMessage("Quantity must greater than zero.");
        }
    }
}
