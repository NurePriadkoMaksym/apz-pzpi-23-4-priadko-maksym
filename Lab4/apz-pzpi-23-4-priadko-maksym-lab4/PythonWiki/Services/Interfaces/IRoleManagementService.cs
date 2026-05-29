using PythonWiki.Dtos;

namespace PythonWiki.Services.Interfaces
{
    public interface IRoleManagementService
    {
        Task ChangeUserRoleAsync(ChangeUserRoleRequest request);
        Task<List<string>> GetAllRolesAsync();
    }
}
