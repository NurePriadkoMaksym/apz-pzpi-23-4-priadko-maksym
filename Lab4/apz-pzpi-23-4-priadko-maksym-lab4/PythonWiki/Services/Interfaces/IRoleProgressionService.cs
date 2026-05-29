namespace PythonWiki.Services.Interfaces
{
    public interface IRoleProgressionService
    {
        Task CheckAndUpgradeRoleAsync(int userId);
    }

}
