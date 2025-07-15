using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HospitalManagementSystem.Repository.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagementSystem.Repository.Data.DataSeeding
{
    public static class DataSeeder
    {
        public static async Task SeedDepartmentsAsync(ApplicationDbContext context)
        {
           
            if (!await context.Departments.AnyAsync())
            {
                var departments = new List<Department>
                {
                    new Department { Name = "Cardiology", Description = "Deals with disorders of the heart and the cardiovascular system." },
                    new Department { Name = "Dermatology", Description = "Specializes in conditions of the skin, hair, and nails." },
                    new Department { Name = "Neurology", Description = "Focuses on disorders of the nervous system." },
                    new Department { Name = "Orthopedics", Description = "Deals with the musculoskeletal system, including bones, joints, ligaments, tendons, and muscles." },
                    new Department { Name = "Pediatrics", Description = "Dedicated to the health and medical care of infants, children, and adolescents." },
                    new Department { Name = "Oncology", Description = "Specializes in the diagnosis and treatment of cancer." },
                    new Department { Name = "Radiology", Description = "Uses medical imaging to diagnose and treat diseases." },
                    new Department { Name = "General Surgery", Description = "Performs surgical procedures for common ailments." },
                    new Department { Name = "Emergency Medicine", Description = "Provides immediate medical care for acute illnesses and injuries." }
                };

                await context.Departments.AddRangeAsync(departments);
                await context.SaveChangesAsync();
            }
        }


        public static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            if (!await roleManager.RoleExistsAsync("Doctor"))
            {
                await roleManager.CreateAsync(new IdentityRole("Doctor"));
            }
            if (!await roleManager.RoleExistsAsync("Patient"))
            {
                await roleManager.CreateAsync(new IdentityRole("Patient"));
            }
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }
        }

        public static async Task SeedAdminUserAsync(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            const string adminEmail = "admin@example.com";
            const string adminPassword = "Admin@123"; // Use a strong password!

            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var adminUser = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);

                if (result.Succeeded)
                {
                    // Ensure the "Admin" role exists
                    if (!await roleManager.RoleExistsAsync("Admin"))
                    {
                        await roleManager.CreateAsync(new IdentityRole("Admin"));
                    }
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
        }
    }
}
