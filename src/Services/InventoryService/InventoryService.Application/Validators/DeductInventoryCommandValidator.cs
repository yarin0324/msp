using FluentValidation;
using InventoryService.Application.Commands;

namespace InventoryService.Application.Validators
{
    public class DeductInventoryCommandValidator : AbstractValidator<CheckInventoryCommand>
    {
        public DeductInventoryCommandValidator()
        {
        }
    }
}
