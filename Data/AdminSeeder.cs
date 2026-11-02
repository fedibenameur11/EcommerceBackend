using EcommerceAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.Data
{
    /// <summary>Crée au démarrage le compte administrateur statique s'il n'existe pas encore.</summary>
    public static class AdminSeeder
    {
        private const string AdminEmail = "admin@gmail.com";
        private const string AdminPassword = "adminadmin";

        public static async Task SeedAsync(EcommerceContext db)
        {
            var admin = await db.Users.FirstOrDefaultAsync(u => u.Email == AdminEmail);
            if (admin == null)
            {
                admin = new User
                {
                    FirstName = "admin",
                    LastName = "admin",
                    Email = AdminEmail,
                    PhoneNumber = "-",
                    Address = "-"
                };
                admin.Password = new PasswordHasher<User>().HashPassword(admin, AdminPassword);
                db.Users.Add(admin);
            }

            // Garantit que le compte reste administrateur et actif même s'il a été modifié
            admin.Role = UserRole.Admin;
            admin.IsActive = true;
            await db.SaveChangesAsync();
        }
    }
}
