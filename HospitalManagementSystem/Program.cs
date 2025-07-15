using BillingAndPayments.Repository.Repositories;
using HospitalManagementSystem.BusinessLogic.Implementation; 
using HospitalManagementSystem.BusinessLogic.Interfaces;
using HospitalManagementSystem.BusinessLogic.Services;
using HospitalManagementSystem.Repository.Data;
using HospitalManagementSystem.Repository.Data.DataSeeding; 
using HospitalManagementSystem.Repository.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                       throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString,
        sqlServerOptionsAction: sqlOptions =>
        {
            
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null); 
        }));


builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    
    options.SignIn.RequireConfirmedAccount = false; 

    
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1; 
})
    .AddEntityFrameworkStores<ApplicationDbContext>() 
    .AddDefaultTokenProviders(); 

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login"; 
    options.LogoutPath = "/Account/Logout"; 
    options.AccessDeniedPath = "/Home/AccessDenied"; 
});

builder.Services.Configure<CookiePolicyOptions>(options =>

{

    options.CheckConsentNeeded = context => false; 

    options.MinimumSameSitePolicy = SameSiteMode.Lax; 

    options.Secure = CookieSecurePolicy.Always;

});

// configure Sessions

builder.Services.AddSession(options =>

{

    options.Cookie.Name = ".DoctorMgmt.Session";

    options.IdleTimeout = TimeSpan.FromMinutes(30); // adjust as needed

    options.Cookie.HttpOnly = true;

    options.Cookie.IsEssential = true;

    options.Cookie.SameSite = SameSiteMode.Lax; 

    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

});


builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<IDoctorAccountService, DoctorAccountService>();
builder.Services.AddScoped<IPatientService, HospitalManagementSystem.BusinessLogic.Implementation.PatientService>();


builder.Services.AddScoped<IBillRepository, BillRepository>();
builder.Services.AddScoped<IBillService, BillService>();
builder.Services.AddHttpContextAccessor();


var app = builder.Build();



using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var logger = services.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("Applying pending database migrations...");
        await context.Database.MigrateAsync(); 
        logger.LogInformation("Database migrations applied successfully.");

        logger.LogInformation("Seeding initial data (Departments, Roles, Admin User)...");
       
        await DataSeeder.SeedDepartmentsAsync(context);
       
        await DataSeeder.SeedRolesAsync(roleManager);
      
        await DataSeeder.SeedAdminUserAsync(userManager, roleManager);
        logger.LogInformation("Initial data seeding completed.");
    }
    catch (Exception ex)
    {
        
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating or seeding the database.");
       
    }
}


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error"); 
}

app.UseHttpsRedirection(); 
app.UseStaticFiles();
app.UseRouting(); 

app.UseAuthentication(); 
app.UseAuthorization(); 

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(

    name: "doctor_login",

    pattern: "{controller=Doctor}/{action=Login}/{id?}");


app.Run(); // starts the application
