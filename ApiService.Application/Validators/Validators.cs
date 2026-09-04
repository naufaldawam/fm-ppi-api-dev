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
    // TIPE VALIDATORS
    // ===================================
    public class CreateTipeRequestValidator : AbstractValidator<CreateTipeRequest>
    {
        public CreateTipeRequestValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        }
    }

    public class UpdateTipeRequestValidator : AbstractValidator<UpdateTipeRequest>
    {
        public UpdateTipeRequestValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        }
    }

    // ===================================
    // BAHAN BAKAR VALIDATORS
    // ===================================
    public class CreateBahanBakarRequestValidator : AbstractValidator<CreateBahanBakarRequest>
    {
        public CreateBahanBakarRequestValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        }
    }

    public class UpdateBahanBakarRequestValidator : AbstractValidator<UpdateBahanBakarRequest>
    {
        public UpdateBahanBakarRequestValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        }
    }

    // ===================================
    // KEPEMILIKAN VALIDATORS
    // ===================================
    public class CreateKepemilikanRequestValidator : AbstractValidator<CreateKepemilikanRequest>
    {
        public CreateKepemilikanRequestValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        }
    }

    public class UpdateKepemilikanRequestValidator : AbstractValidator<UpdateKepemilikanRequest>
    {
        public UpdateKepemilikanRequestValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        }
    }

    // ===================================
    // VENDOR VALIDATORS
    // ===================================
    public class CreateVendorRequestValidator : AbstractValidator<CreateVendorRequest>
    {
        public CreateVendorRequestValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        }
    }

    public class UpdateVendorRequestValidator : AbstractValidator<UpdateVendorRequest>
    {
        public UpdateVendorRequestValidator()
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

    // ===================================
    // KENDARAAN VALIDATORS
    // ===================================
    public class CreateKendaraanRequestValidator : AbstractValidator<CreateKendaraanRequest>
    {
        public CreateKendaraanRequestValidator()
        {
            RuleFor(x => x.NomorPolisi).NotEmpty().MaximumLength(20);
            RuleFor(x => x.TipeId).NotEmpty().WithMessage("Tipe harus dipilih");
            RuleFor(x => x.BahanBakarId).NotEmpty().WithMessage("Bahan bakar harus dipilih");
            RuleFor(x => x.Merek).NotEmpty().MaximumLength(100);
            RuleFor(x => x.VendorId).NotEmpty().WithMessage("Vendor harus dipilih");
            RuleFor(x => x.KepemilikanId).NotEmpty().WithMessage("Kepemilikan harus dipilih");
            RuleFor(x => x.JabatanId).NotEmpty().WithMessage("Alokasi jabatan harus dipilih");
        }
    }

    public class UpdateKendaraanRequestValidator : AbstractValidator<UpdateKendaraanRequest>
    {
        public UpdateKendaraanRequestValidator()
        {
            RuleFor(x => x.NomorPolisi).NotEmpty().MaximumLength(20);
            RuleFor(x => x.TipeId).NotEmpty().WithMessage("Tipe harus dipilih");
            RuleFor(x => x.BahanBakarId).NotEmpty().WithMessage("Bahan bakar harus dipilih");
            RuleFor(x => x.Merek).NotEmpty().MaximumLength(100);
            RuleFor(x => x.VendorId).NotEmpty().WithMessage("Vendor harus dipilih");
            RuleFor(x => x.KepemilikanId).NotEmpty().WithMessage("Kepemilikan harus dipilih");
            RuleFor(x => x.JabatanId).NotEmpty().WithMessage("Alokasi jabatan harus dipilih");
        }
    }

    // ===================================
    // RF.ID VALIDATORS
    // ===================================
    public class CreateRfIdRequestValidator : AbstractValidator<CreateRfIdRequest>
    {
        public CreateRfIdRequestValidator()
        {
            RuleFor(x => x.RfIdCode).NotEmpty().MaximumLength(50);
            RuleFor(x => x.PekerjaId).NotEmpty().WithMessage("Nama pekerja harus dipilih");
            RuleFor(x => x.KendaraanId).NotEmpty().WithMessage("Nopol harus dipilih");
        }
    }

    public class UpdateRfIdRequestValidator : AbstractValidator<UpdateRfIdRequest>
    {
        public UpdateRfIdRequestValidator()
        {
            RuleFor(x => x.RfIdCode).NotEmpty().MaximumLength(50);
            RuleFor(x => x.PekerjaId).NotEmpty().WithMessage("Nama pekerja harus dipilih");
            RuleFor(x => x.KendaraanId).NotEmpty().WithMessage("Nopol harus dipilih");
        }
    }

    // ===================================
    // DRIVER VALIDATORS
    // ===================================
    public class CreateDriverRequestValidator : AbstractValidator<CreateDriverRequest>
    {
        public CreateDriverRequestValidator()
        {
            RuleFor(x => x.NoPekerja).NotEmpty().MaximumLength(50);
            RuleFor(x => x.NamaDriver).NotEmpty().MaximumLength(150);
            RuleFor(x => x.NoHp).NotEmpty().MaximumLength(20);
            RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(150);
            RuleFor(x => x.VendorId).NotEmpty().WithMessage("Vendor harus dipilih");
            RuleFor(x => x.AtasanId).NotEmpty().WithMessage("Atasan harus dipilih");
        }
    }

    public class UpdateDriverRequestValidator : AbstractValidator<UpdateDriverRequest>
    {
        public UpdateDriverRequestValidator()
        {
            RuleFor(x => x.NoPekerja).NotEmpty().MaximumLength(50);
            RuleFor(x => x.NamaDriver).NotEmpty().MaximumLength(150);
            RuleFor(x => x.NoHp).NotEmpty().MaximumLength(20);
            RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(150);
            RuleFor(x => x.VendorId).NotEmpty().WithMessage("Vendor harus dipilih");
            RuleFor(x => x.AtasanId).NotEmpty().WithMessage("Atasan harus dipilih");
        }
    }
}
