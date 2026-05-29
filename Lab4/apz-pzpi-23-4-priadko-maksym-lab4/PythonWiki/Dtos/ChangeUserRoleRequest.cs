namespace PythonWiki.Dtos;

public class ChangeUserRoleRequest
{
    public int UserId { get; set; }
    public string RoleName { get; set; } = null!;
}
