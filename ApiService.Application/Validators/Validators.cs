using FluentValidation;
using ApiService.Application.DTOs;

namespace ApiService.Application.Validators
{
    public class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
    {
        public CreateProductRequestValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
            RuleFor(x => x.Price).GreaterThanOrEqualTo(0).WithMessage("Price cannot be negative");
            RuleFor(x => x.Stock).GreaterThanOrEqualTo(0).WithMessage("Stock cannot be negative");
            RuleFor(x => x.Description).MaximumLength(1000).When(x => x.Description != null);
            RuleFor(x => x.Category).MaximumLength(100).When(x => x.Category != null);
        }
    }

    public class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
    {
        public UpdateProductRequestValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
            RuleFor(x => x.Price).GreaterThanOrEqualTo(0).WithMessage("Price cannot be negative");
            RuleFor(x => x.Stock).GreaterThanOrEqualTo(0).WithMessage("Stock cannot be negative");
            RuleFor(x => x.Description).MaximumLength(1000).When(x => x.Description != null);
            RuleFor(x => x.Category).MaximumLength(100).When(x => x.Category != null);
        }
    }

        // ===================================
    // JABATAN VALIDATORS
    // ===================================
    public class CreateJabatanRequestValidator : AbstractValidator<CreateJabatanRequest>
    {
        public CreateJabatanRequestValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        }
    }

    public class UpdateJabatanRequestValidator : AbstractValidator<UpdateJabatanRequest>
    {
        public UpdateJabatanRequestValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        }
    }

    // ===================================
    // PEKERJA VALIDATORS
    // ===================================
    public class CreatePekerjaRequestValidator : AbstractValidator<CreatePekerjaRequest>
    {
        public CreatePekerjaRequestValidator()
        {
            RuleFor(x => x.NoPekerja).NotEmpty().MaximumLength(50);
            RuleFor(x => x.NopekHome).NotEmpty().MaximumLength(50);
            RuleFor(x => x.NopekHost).NotEmpty().MaximumLength(50);
            RuleFor(x => x.NamaPekerja).NotEmpty().MaximumLength(255);
            RuleFor(x => x.JabatanId).NotEmpty().WithMessage("Jabatan harus dipilih");
        }
    }

    public class UpdatePekerjaRequestValidator : AbstractValidator<UpdatePekerjaRequest>
    {
        public UpdatePekerjaRequestValidator()
        {
            RuleFor(x => x.NoPekerja).NotEmpty().MaximumLength(50);
            RuleFor(x => x.NopekHome).NotEmpty().MaximumLength(50);
            RuleFor(x => x.NopekHost).NotEmpty().MaximumLength(50);
            RuleFor(x => x.NamaPekerja).NotEmpty().MaximumLength(255);
            RuleFor(x => x.JabatanId).NotEmpty().WithMessage("Jabatan harus dipilih");
        }
    }
}
