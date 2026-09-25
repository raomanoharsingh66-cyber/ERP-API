using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Users;

namespace BizFlow.Application.Common.Interfaces;

public interface IUserService
{
    Task<ApiResponse<PagedResult<UserDto>>> GetUsersAsync(int pageNumber = 1, int pageSize = 10, string? search = null, CancellationToken cancellationToken = default);
    Task<ApiResponse<UserDto>> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse<UserDto>> CreateUserAsync(CreateUserDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<UserDto>> UpdateUserAsync(Guid id, UpdateUserDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse> DeleteUserAsync(Guid id, CancellationToken cancellationToken = default);
}
