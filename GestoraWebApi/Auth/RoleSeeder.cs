using Microsoft.AspNetCore.Identity;

namespace GestoraWebApi.Auth
{
    public static class RoleSeeder
    {
        public static async Task SeedAsync(RoleManager<IdentityRole> roleManager)
        {
            string[] roles = [Roles.Admin, Roles.Staff, Roles.Cliente];

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }
}
