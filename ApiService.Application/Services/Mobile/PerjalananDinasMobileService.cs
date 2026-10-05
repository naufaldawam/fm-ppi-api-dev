using ApiService.Application.DTOs;
using ApiService.Application.DTOs.Mobile;

namespace ApiService.Application.Services.Mobile
{
    public interface IPerjalananDinasMobileService
    {
        Task<ApiResponse<PagedResponse<PerjalananDinasDto>>> GetAllAsync(PerjalananDinasMobileFilterRequest filter);
        Task<ApiResponse<PerjalananDinasDto>> GetByIdAsync(string id);
        Task<ApiResponse<PerjalananDinasDto>> CreateAsync(CreatePerjalananDinasMobileRequest request, string userId);
        Task<ApiResponse<PerjalananDinasDto>> UpdateAsync(string id, UpdatePerjalananDinasMobileRequest request, string userId);
        Task<ApiResponse<bool>> DeleteAsync(string id, string userId);
    }
    public class PerjalananDinasMobileService : IPerjalananDinasMobileService
    {
        private readonly IPerjalananDinasService _service;

        public PerjalananDinasMobileService(IPerjalananDinasService service)
        {
            _service = service;
        }

        public async Task<ApiResponse<PagedResponse<PerjalananDinasDto>>> GetAllAsync(PerjalananDinasMobileFilterRequest filter)
        {
            return await _service.GetAllAsync(new PerjalananDinasFilterRequest
            {
                Search = filter.Search,
                PeriodeId = filter.PeriodeId,
                IsActive = filter.IsActive,
                Page = filter.Page,
                PageSize = filter.PageSize
            });
        }

        public async Task<ApiResponse<PerjalananDinasDto>> GetByIdAsync(string id)
        {
            return await _service.GetByIdAsync(id);
        }

        public async Task<ApiResponse<PerjalananDinasDto>> CreateAsync(CreatePerjalananDinasMobileRequest request, string userId)
        {
            return await _service.CreateAsync(new CreatePerjalananDinasRequest
            {
                PekerjaId = request.PekerjaId,
                BulanTahun = request.BulanTahun,
                PeriodeId = request.PeriodeId,
                TotalBiayaDinas = request.TotalBiayaDinas
            }, userId);
        }

        public async Task<ApiResponse<PerjalananDinasDto>> UpdateAsync(string id, UpdatePerjalananDinasMobileRequest request, string userId)
        {
            return await _service.UpdateAsync(id, new UpdatePerjalananDinasRequest
            {
                PekerjaId = request.PekerjaId,
                BulanTahun = request.BulanTahun,
                PeriodeId = request.PeriodeId,
                TotalBiayaDinas = request.TotalBiayaDinas,
                IsActive = request.IsActive
            }, userId);
        }

        public async Task<ApiResponse<bool>> DeleteAsync(string id, string userId)
        {
            return await _service.DeleteAsync(id, userId);
        }
    }
}
