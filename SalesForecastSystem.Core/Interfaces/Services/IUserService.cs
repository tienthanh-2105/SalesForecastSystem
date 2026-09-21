using SalesForecastSystem.Core.Common;
using SalesForecastSystem.Core.DTOs.Users;

namespace SalesForecastSystem.Core.Interfaces.Services;

public interface IUserService
{
    Task<PagedResponse<UserResponse>> GetPagedAsync(
        UserQueryRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<UserResponse>> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<UserResponse>> CreateAsync(
        UserCreateRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<UserResponse>> UpdateAsync(
        int id,
        int currentUserId,
        UserUpdateRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<UserResponse>> SetStatusAsync(
        int id,
        int currentUserId,
        UserStatusRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<bool>> ResetPasswordAsync(
        int id,
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default);
}
