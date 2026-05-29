using PythonWiki.Dtos;

namespace PythonWiki.Services.Interfaces;

public interface IUserService
{
    Task<UserProfileResponse> GetMyProfileAsync(int userId);
    Task<UserProfileResponse> GetUserProfileByIdAsync(int userId);
    Task DeleteUserAsync(int userId);

}
