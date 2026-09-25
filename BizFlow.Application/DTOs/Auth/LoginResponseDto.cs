using BizFlow.Application.Common.Models;

namespace BizFlow.Application.DTOs.Auth;

public class LoginResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public CurrentUserDto User { get; set; } = null!;
}
