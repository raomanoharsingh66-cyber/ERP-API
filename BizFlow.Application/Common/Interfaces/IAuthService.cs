using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Auth;

namespace BizFlow.Application.Common.Interfaces;

public interface IAuthService
{
    Task<ApiResponse<LoginResponseDto>> RegisterBusinessAsync(RegisterBusinessDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<LoginResponseDto>> LoginAsync(LoginRequestDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<LoginResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse> RevokeTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<ApiResponse<CurrentUserDto>> GetCurrentUserProfileAsync(CancellationToken cancellationToken = default);
}
