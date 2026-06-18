namespace GestoraWebApi.Services.Auth.DTOs
{
    public class UserResponseDTO
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public IList<string> Roles { get; set; } = new List<string>();
    }

    public class UpdateUserDTO
    {
        public string? UserName { get; set; }
        public string? Email { get; set; }
    }

    public class AdminResetPasswordDTO
    {
        public string NewPassword { get; set; } = string.Empty;
    }
}