using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DNUContact.Data;
using DNUContact.Models;
using DNUContact.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container - Dùng SQLite
var connectionString = "Data Source=dnucontact.db";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
  options.UseSqlite(connectionString));

// Thêm Identity services
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => {
  // Password settings
  options.Password.RequireDigit = true;
  options.Password.RequireLowercase = true;
  options.Password.RequireNonAlphanumeric = false;
  options.Password.RequireUppercase = true;
  options.Password.RequiredLength = 8;
  options.Password.RequiredUniqueChars = 1;

  // Lockout settings
  options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
  options.Lockout.MaxFailedAccessAttempts = 5;
  options.Lockout.AllowedForNewUsers = true;

  // User settings
  options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
  options.User.RequireUniqueEmail = true;

  // Email confirmation
  options.SignIn.RequireConfirmedEmail = false; // Tạm thời tắt để test
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Register custom services
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IActivityLogService, ActivityLogService>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
  app.UseDeveloperExceptionPage();
}
else
{
  app.UseExceptionHandler("/Home/Error");
  app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "account",
    pattern: "Account/{action=Login}/{id?}",
    defaults: new { controller = "Account" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// XÓA VÀ TẠO LẠI DATABASE ĐỂ SỬA LỖI SCHEMA
using (var scope = app.Services.CreateScope())
{
  var services = scope.ServiceProvider;
  try
  {
      var context = services.GetRequiredService<ApplicationDbContext>();
      var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
      var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
      
      // XÓA DATABASE CŨ VÀ TẠO MỚI
      await context.Database.EnsureDeletedAsync();
      await context.Database.EnsureCreatedAsync();
      Console.WriteLine("Database recreated successfully!");
      
      // Seed roles
      string[] roles = { "Admin", "CBGV", "SinhVien" };
      foreach (var role in roles)
      {
          if (!await roleManager.RoleExistsAsync(role))
          {
              await roleManager.CreateAsync(new IdentityRole(role));
              Console.WriteLine($"{role} role created!");
          }
      }
          
      // Seed Admin user
      if (await userManager.FindByEmailAsync("admin@dnu.edu.vn") == null)
      {
          var adminUser = new ApplicationUser
          {
              UserName = "admin@dnu.edu.vn",
              Email = "admin@dnu.edu.vn",
              FullName = "Nguyễn Văn Admin",
              EmailConfirmed = true,
              IsActive = true,
              IsEmailVerified = true,
              CreatedAt = DateTime.Now,
              PhotoUrl = "/images/default-avatar.png"
          };
          
          var result = await userManager.CreateAsync(adminUser, "Admin@123");
          if (result.Succeeded)
          {
              await userManager.AddToRoleAsync(adminUser, "Admin");
              Console.WriteLine("Admin user created successfully!");
          }
      }

      // Seed CBGV user
      if (await userManager.FindByEmailAsync("gv01@dnu.edu.vn") == null)
      {
          var cbgvUser = new ApplicationUser
          {
              UserName = "gv01@dnu.edu.vn",
              Email = "gv01@dnu.edu.vn",
              FullName = "Trần Thị Lan",
              EmailConfirmed = true,
              IsActive = true,
              IsEmailVerified = true,
              CreatedAt = DateTime.Now,
              PhotoUrl = "/images/default-avatar.png"
          };
          
          var result = await userManager.CreateAsync(cbgvUser, "Cbgv@123");
          if (result.Succeeded)
          {
              await userManager.AddToRoleAsync(cbgvUser, "CBGV");
              
              // Create Staff record
              var staff = new Staff
              {
                  StaffCode = "GV001",
                  FullName = "Trần Thị Lan",
                  Position = "Giảng viên",
                  Phone = "0123456789",
                  Email = "gv01@dnu.edu.vn",
                  AcademicDegree = "Thạc sĩ",
                  UnitId = 1,
                  UserId = cbgvUser.Id,
                  IsActive = true,
                  CreatedAt = DateTime.Now
              };
              context.Staff.Add(staff);
              
              Console.WriteLine("CBGV user created successfully!");
          }
      }

      // Seed Student user
      if (await userManager.FindByEmailAsync("sv01@e.dnu.edu.vn") == null)
      {
          var studentUser = new ApplicationUser
          {
              UserName = "sv01@e.dnu.edu.vn",
              Email = "sv01@e.dnu.edu.vn",
              FullName = "Lê Văn Nam",
              EmailConfirmed = true,
              IsActive = true,
              IsEmailVerified = true,
              CreatedAt = DateTime.Now,
              PhotoUrl = "/images/default-avatar.png"
          };
          
          var result = await userManager.CreateAsync(studentUser, "Student@123");
          if (result.Succeeded)
          {
              await userManager.AddToRoleAsync(studentUser, "SinhVien");
              
              // Create Student record
              var student = new Student
              {
                  StudentCode = "SV2024001",
                  FullName = "Lê Văn Nam",
                  Phone = "0987654321",
                  Email = "sv01@e.dnu.edu.vn",
                  Address = "Hà Nội",
                  ClassName = "CNTT2024A",
                  EnrollmentYear = 2024,
                  UserId = studentUser.Id,
                  IsActive = true,
                  CreatedAt = DateTime.Now
              };
              context.Students.Add(student);
              
              Console.WriteLine("Student user created successfully!");
          }
      }

      // Seed sample units
      if (!context.Units.Any())
      {
          var units = new List<Unit>
          {
              new Unit
              {
                  UnitCode = "DNU",
                  Name = "Đại học Đại Nam",
                  Address = "Hà Nội",
                  Phone = "024-12345678",
                  Email = "info@dnu.edu.vn",
                  UnitType = "Trường",
                  IsActive = true,
                  CreatedAt = DateTime.Now
              },
              new Unit
              {
                  UnitCode = "CNTT",
                  Name = "Khoa Công nghệ thông tin",
                  Address = "Hà Nội",
                  Phone = "024-12345679",
                  Email = "cntt@dnu.edu.vn",
                  UnitType = "Khoa",
                  ParentUnitId = 1,
                  IsActive = true,
                  CreatedAt = DateTime.Now
              }
          };
          
          context.Units.AddRange(units);
      }
      
      await context.SaveChangesAsync();
      Console.WriteLine("Database seeding completed!");
  }
  catch (Exception ex)
  {
      var logger = services.GetRequiredService<ILogger<Program>>();
      logger.LogError(ex, "An error occurred seeding the DB.");
      Console.WriteLine($"Database error: {ex.Message}");
  }
}

app.MapGet("/", context =>
{
    context.Response.Redirect("/Account/Login");
    return Task.CompletedTask;
});

Console.WriteLine("Starting web server...");
app.Run();
