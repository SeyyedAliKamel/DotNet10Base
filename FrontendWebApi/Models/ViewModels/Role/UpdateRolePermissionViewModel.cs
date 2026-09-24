namespace AdminWebApi.Models.ViewModels.Role
{
    public class UpdateRolePermissionViewModel
    {
      public  List<long> permissionIds { get; set; }
      public long roleId { get; set; }
    }
}
