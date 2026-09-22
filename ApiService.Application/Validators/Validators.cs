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
    // JENIS BBM VALIDATORS
    // ===================================
    public class CreateJenisBbmRequestValidator : AbstractValidator<CreateJenisBbmRequest>
    {
        public CreateJenisBbmRequestValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        }
    }

    public class UpdateJenisBbmRequestValidator : AbstractValidator<UpdateJenisBbmRequest>
    {
        public UpdateJenisBbmRequestValidator()
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
            // RuleFor(x => x.AtasanId).NotEmpty().WithMessage("Atasan harus dipilih");
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
            // RuleFor(x => x.AtasanId).NotEmpty().WithMessage("Atasan harus dipilih");
        }
    }

    // ===================================
    // PERIODE VALIDATORS
    // ===================================
    public class CreatePeriodeRequestValidator : AbstractValidator<CreatePeriodeRequest>
    {
        public CreatePeriodeRequestValidator()
        {
            RuleFor(x => x.NamaPeriode).NotEmpty().MaximumLength(100);
            RuleFor(x => x.TanggalAwal).NotEmpty().WithMessage("Tanggal awal wajib diisi");
            RuleFor(x => x.TanggalAkhir)
                .NotEmpty().WithMessage("Tanggal akhir wajib diisi")
                .GreaterThanOrEqualTo(x => x.TanggalAwal)
                .WithMessage("Tanggal akhir tidak boleh sebelum tanggal awal");
        }
    }

    public class UpdatePeriodeRequestValidator : AbstractValidator<UpdatePeriodeRequest>
    {
        public UpdatePeriodeRequestValidator()
        {
            RuleFor(x => x.NamaPeriode).NotEmpty().MaximumLength(100);
            RuleFor(x => x.TanggalAwal).NotEmpty().WithMessage("Tanggal awal wajib diisi");
            RuleFor(x => x.TanggalAkhir)
                .NotEmpty().WithMessage("Tanggal akhir wajib diisi")
                .GreaterThanOrEqualTo(x => x.TanggalAwal)
                .WithMessage("Tanggal akhir tidak boleh sebelum tanggal awal");
        }
    }

    // ===================================
    // MEMBER PARKIR VALIDATORS
    // ===================================
    public class CreateMemberParkirRequestValidator : AbstractValidator<CreateMemberParkirRequest>
    {
        public CreateMemberParkirRequestValidator()
        {
            RuleFor(x => x.PekerjaId).NotEmpty().WithMessage("Pekerja harus dipilih");
            RuleFor(x => x.PeriodeId).NotEmpty().WithMessage("Periode harus dipilih");
            RuleFor(x => x.TanggalPenagihan).NotEmpty().WithMessage("Tanggal penagihan wajib diisi");
            RuleFor(x => x.JumlahBiaya).GreaterThan(0).WithMessage("Jumlah biaya harus lebih dari 0");
        }
    }

    public class UpdateMemberParkirRequestValidator : AbstractValidator<UpdateMemberParkirRequest>
    {
        public UpdateMemberParkirRequestValidator()
        {
            RuleFor(x => x.PekerjaId).NotEmpty().WithMessage("Pekerja harus dipilih");
            RuleFor(x => x.PeriodeId).NotEmpty().WithMessage("Periode harus dipilih");
            RuleFor(x => x.TanggalPenagihan).NotEmpty().WithMessage("Tanggal penagihan wajib diisi");
            RuleFor(x => x.JumlahBiaya).GreaterThan(0).WithMessage("Jumlah biaya harus lebih dari 0");
        }
    }

    public class CreateOperasionalUpahRequestValidator : AbstractValidator<CreateOperasionalUpahRequest>
    {
        public CreateOperasionalUpahRequestValidator()
        {
            RuleFor(x => x.PekerjaId).NotEmpty().WithMessage("Pekerja harus dipilih");
            RuleFor(x => x.PeriodeId).NotEmpty().WithMessage("Periode harus dipilih");
            RuleFor(x => x.TotalLembur).GreaterThanOrEqualTo(0).WithMessage("Total Lembur tidak boleh negatif");
            RuleFor(x => x.TotalEMoneyMember).GreaterThanOrEqualTo(0).WithMessage("Total E-Money Member tidak boleh negatif");
            RuleFor(x => x.DanaOps).GreaterThanOrEqualTo(0).WithMessage("Dana Ops tidak boleh negatif");
            RuleFor(x => x.TotalParkir).GreaterThanOrEqualTo(0).WithMessage("Total Parkir tidak boleh negatif");
            RuleFor(x => x.TotalSewaKendaraan).GreaterThanOrEqualTo(0).WithMessage("Total Sewa Kendaraan tidak boleh negatif");
            RuleFor(x => x.TotalUpahDriver).GreaterThanOrEqualTo(0).WithMessage("Total Upah Driver tidak boleh negatif");
        }
    }

    public class UpdateOperasionalUpahRequestValidator : AbstractValidator<UpdateOperasionalUpahRequest>
    {
        public UpdateOperasionalUpahRequestValidator()
        {
            RuleFor(x => x.PekerjaId).NotEmpty().WithMessage("Pekerja harus dipilih");
            RuleFor(x => x.PeriodeId).NotEmpty().WithMessage("Periode harus dipilih");
            RuleFor(x => x.TotalLembur).GreaterThanOrEqualTo(0).WithMessage("Total Lembur tidak boleh negatif");
            RuleFor(x => x.TotalEMoneyMember).GreaterThanOrEqualTo(0).WithMessage("Total E-Money Member tidak boleh negatif");
            RuleFor(x => x.DanaOps).GreaterThanOrEqualTo(0).WithMessage("Dana Ops tidak boleh negatif");
            RuleFor(x => x.TotalParkir).GreaterThanOrEqualTo(0).WithMessage("Total Parkir tidak boleh negatif");
            RuleFor(x => x.TotalSewaKendaraan).GreaterThanOrEqualTo(0).WithMessage("Total Sewa Kendaraan tidak boleh negatif");
            RuleFor(x => x.TotalUpahDriver).GreaterThanOrEqualTo(0).WithMessage("Total Upah Driver tidak boleh negatif");
        }
    }

    public class CreateBbmSubmissionRequestValidator : AbstractValidator<CreateBbmSubmissionRequest>
    {
        public CreateBbmSubmissionRequestValidator()
        {
            RuleFor(x => x.DriverId).NotEmpty().WithMessage("Driver harus dipilih");
            RuleFor(x => x.PeriodeId).NotEmpty().WithMessage("Periode harus dipilih");
            RuleFor(x => x.TanggalPenggunaan).NotEmpty().WithMessage("Tanggal penggunaan wajib diisi");
            RuleFor(x => x.KendaraanId).NotEmpty().WithMessage("Nomor plat kendaraan harus dipilih");
            RuleFor(x => x.JumlahPenggunaanBbm).GreaterThan(0).WithMessage("Jumlah penggunaan BBM harus lebih dari 0");
            RuleFor(x => x.NilaiOdometer).GreaterThanOrEqualTo(0).WithMessage("Nilai odometer tidak boleh negatif");
            RuleFor(x => x.NilaiNota).GreaterThan(0).WithMessage("Nilai nota harus lebih dari 0");
            RuleFor(x => x.CatatanTambahan).MaximumLength(500).When(x => x.CatatanTambahan != null);
        }
    }

    public class RejectBbmSubmissionRequestValidator : AbstractValidator<RejectBbmSubmissionRequest>
    {
        public RejectBbmSubmissionRequestValidator()
        {
            RuleFor(x => x.Reason).NotEmpty().WithMessage("Alasan penolakan wajib diisi").MaximumLength(500);
        }
    }

    public class CreateTagihanKwhRequestValidator : AbstractValidator<CreateTagihanKwhRequest>
    {
        public CreateTagihanKwhRequestValidator()
        {
            RuleFor(x => x.TanggalPenagihan).NotEmpty().WithMessage("Tanggal penagihan wajib diisi");
            RuleFor(x => x.JumlahBiaya).GreaterThan(0).WithMessage("Jumlah biaya harus lebih dari 0");
            RuleFor(x => x.JumlahKwh).GreaterThan(0).WithMessage("Jumlah KWH harus lebih dari 0");
        }
    }

    public class UpdateTagihanKwhRequestValidator : AbstractValidator<UpdateTagihanKwhRequest>
    {
        public UpdateTagihanKwhRequestValidator()
        {
            RuleFor(x => x.TanggalPenagihan).NotEmpty().WithMessage("Tanggal penagihan wajib diisi");
            RuleFor(x => x.JumlahBiaya).GreaterThan(0).WithMessage("Jumlah biaya harus lebih dari 0");
            RuleFor(x => x.JumlahKwh).GreaterThan(0).WithMessage("Jumlah KWH harus lebih dari 0");
        }
    }

    // ===================================
    // PERJALANAN DINAS VALIDATORS
    // ===================================
    public class CreatePerjalananDinasRequestValidator : AbstractValidator<CreatePerjalananDinasRequest>
    {
        public CreatePerjalananDinasRequestValidator()
        {
            RuleFor(x => x.PekerjaId).NotEmpty().WithMessage("Pekerja harus dipilih");
            RuleFor(x => x.PeriodeId).NotEmpty().WithMessage("Periode harus dipilih");
            RuleFor(x => x.BulanTahun).NotEmpty().WithMessage("Bulan tahun wajib diisi");
            RuleFor(x => x.TotalBiayaDinas).GreaterThan(0).WithMessage("Total biaya dinas harus lebih dari 0");
        }
    }

    public class UpdatePerjalananDinasRequestValidator : AbstractValidator<UpdatePerjalananDinasRequest>
    {
        public UpdatePerjalananDinasRequestValidator()
        {
            RuleFor(x => x.PekerjaId).NotEmpty().WithMessage("Pekerja harus dipilih");
            RuleFor(x => x.PeriodeId).NotEmpty().WithMessage("Periode harus dipilih");
            RuleFor(x => x.BulanTahun).NotEmpty().WithMessage("Bulan tahun wajib diisi");
            RuleFor(x => x.TotalBiayaDinas).GreaterThan(0).WithMessage("Total biaya dinas harus lebih dari 0");
        }
    }

    public class CreateSimCardRequestValidator : AbstractValidator<CreateSimCardRequest>
    {
        public CreateSimCardRequestValidator()
        {
            RuleFor(x => x.PekerjaId).NotEmpty().WithMessage("Pekerja harus dipilih");
            RuleFor(x => x.PeriodeId).NotEmpty().WithMessage("Periode harus dipilih");
            RuleFor(x => x.BiayaSimCard).GreaterThan(0).WithMessage("Biaya SIM card harus lebih dari 0");
        }
    }

public class UpdateSimCardRequestValidator : AbstractValidator<UpdateSimCardRequest>
    {
        public UpdateSimCardRequestValidator()
        {
            RuleFor(x => x.PekerjaId).NotEmpty().WithMessage("Pekerja harus dipilih");
            RuleFor(x => x.PeriodeId).NotEmpty().WithMessage("Periode harus dipilih");
            RuleFor(x => x.BiayaSimCard).GreaterThan(0).WithMessage("Biaya SIM card harus lebih dari 0");
        }
    }

    // ===================================
    // BIAYA KESEHATAN VALIDATORS
    // ===================================
    public class CreateBiayaKesehatanRequestValidator : AbstractValidator<CreateBiayaKesehatanRequest>
    {
        public CreateBiayaKesehatanRequestValidator()
        {
            RuleFor(x => x.PekerjaId).NotEmpty().WithMessage("Pekerja harus dipilih");
            RuleFor(x => x.PeriodeId).NotEmpty().WithMessage("Periode harus dipilih");
            RuleFor(x => x.BulanTahun).NotEmpty().WithMessage("Bulan tahun wajib diisi");
            RuleFor(x => x.TotalBiaya).GreaterThan(0).WithMessage("Total biaya harus lebih dari 0");
        }
    }

    public class UpdateBiayaKesehatanRequestValidator : AbstractValidator<UpdateBiayaKesehatanRequest>
    {
        public UpdateBiayaKesehatanRequestValidator()
        {
            RuleFor(x => x.PekerjaId).NotEmpty().WithMessage("Pekerja harus dipilih");
            RuleFor(x => x.PeriodeId).NotEmpty().WithMessage("Periode harus dipilih");
            RuleFor(x => x.BulanTahun).NotEmpty().WithMessage("Bulan tahun wajib diisi");
            RuleFor(x => x.TotalBiaya).GreaterThan(0).WithMessage("Total biaya harus lebih dari 0");
        }
    }
}