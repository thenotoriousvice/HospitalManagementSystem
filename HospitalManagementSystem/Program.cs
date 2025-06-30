using BillingAndPayments.Repository.Repositories;
using HospitalManagementSystem.BusinessLogic.Implementation; // For PatientService (assuming it's in Implementation)
using HospitalManagementSystem.BusinessLogic.Interfaces;
using HospitalManagementSystem.BusinessLogic.Services;
using HospitalManagementSystem.Repository.Data;
using HospitalManagementSystem.Repository.Data.DataSeeding; // For DataSeeder
using HospitalManagementSystem.Repository.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// --- Database Context Configuration ---
// Configure the database connection for SQL Server with retry logic
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                       throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString,
        sqlServerOptionsAction: sqlOptions =>
        {
            // Enable retry logic for transient failures (e.g., temporary database unavailability)
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null); // Use default error numbers
        }));

// --- ASP.NET Core Identity Configuration ---
// Configure Identity for IdentityUser and IdentityRole, enabling role management
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    // Sign-in options
    options.SignIn.RequireConfirmedAccount = false; // Set to true for production for email confirmation

    // Password options (combined from both original files, prioritizing stricter requirements)
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1; // From the first Program.cs
})
    .AddEntityFrameworkStores<ApplicationDbContext>() // This tells Identity to use your ApplicationDbContext for its data
    .AddDefaultTokenProviders(); // Required for password resets, email confirmations etc.

// Configure the Application Cookie options for login/logout paths
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login"; // Adjust to your actual login path, e.g., /Patient/Login or /Doctor/Login
    options.LogoutPath = "/Account/Logout"; // Adjust to your actual logout path
    options.AccessDeniedPath = "/Home/AccessDenied"; // Set this to your Access Denied action/page
});

builder.Services.Configure<CookiePolicyOptions>(options =>

{

    options.CheckConsentNeeded = context => false; // Optional: true if you want consent banner

    options.MinimumSameSitePolicy = SameSiteMode.Lax; // Change to None if needed

    options.Secure = CookieSecurePolicy.Always; // Enforce HTTPS

});

// 3. Configure Session properly

builder.Services.AddSession(options =>

{

    options.Cookie.Name = ".DoctorMgmt.Session";

    options.IdleTimeout = TimeSpan.FromMinutes(30); // adjust as needed

    options.Cookie.HttpOnly = true;

    options.Cookie.IsEssential = true;

    options.Cookie.SameSite = SameSiteMode.Lax; // Set to None if needed with HTTPS

    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

});

// --- Business Logic Services Registration ---
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<IDoctorAccountService, DoctorAccountService>();
builder.Services.AddScoped<IPatientService, HospitalManagementSystem.BusinessLogic.Implementation.PatientService>();


builder.Services.AddScoped<IBillRepository, BillRepository>();
builder.Services.AddScoped<IBillService, BillService>();
builder.Services.AddHttpContextAccessor();


var app = builder.Build();

// --- Application Startup: Database Migration and Data Seeding ---
// This block ensures that the database is migrated and seeded with initial data
// (Departments, Roles, Admin User) when the application starts.
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
        await context.Database.MigrateAsync(); // Apply any pending database migrations
        logger.LogInformation("Database migrations applied successfully.");

        logger.LogInformation("Seeding initial data (Departments, Roles, Admin User)...");
        // Seed departments
        await DataSeeder.SeedDepartmentsAsync(context);
        // Seed roles (Admin, Doctor, Patient)
        await DataSeeder.SeedRolesAsync(roleManager);
        // Seed the admin user
        await DataSeeder.SeedAdminUserAsync(userManager, roleManager);
        logger.LogInformation("Initial data seeding completed.");
    }
    catch (Exception ex)
    {
        // Log any errors that occur during migration or seeding
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating or seeding the database.");
        // Depending on your deployment strategy, you might want to re-throw the exception
        // or ensure the application doesn't proceed if critical setup fails.
        // For development, it's often fine to continue, but in production, this might be a critical error.
    }
}


// --- HTTP Request Pipeline Configuration ---
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error"); // Global error handling for non-development environments
    app.UseHsts(); // Enforces secure connections over HTTPS
}

app.UseHttpsRedirection(); // Redirects HTTP requests to HTTPS
app.UseStaticFiles(); // Serves static files (CSS, JS, images)

app.UseRouting(); // Enables endpoint routing

app.UseAuthentication(); // IMPORTANT: Must be before UseAuthorization. Handles user login/logout.
app.UseAuthorization(); // Authorizes users based on policies/roles.

// Default routing for MVC controllers
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(

    name: "doctor_login",

    pattern: "{controller=Doctor}/{action=Login}/{id?}");


app.Run(); // Starts the application
