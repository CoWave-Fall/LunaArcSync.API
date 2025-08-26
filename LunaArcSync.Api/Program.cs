using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using LunaArcSync.Api.BackgroundTasks;
using LunaArcSync.Api.Core.Entities;
using LunaArcSync.Api.Core.Interfaces;
using LunaArcSync.Api.Infrastructure.Data;
using LunaArcSync.Api.Infrastructure.FileStorage;
using LunaArcSync.Api.Infrastructure.Services;
using LunaArcSync.Api.DTOs;
using LunaArcSync.Api.Core.Constants;
using System;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// --- 1. 服务注册 (DI) ---

// +++  CORS  +++
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          policy.AllowAnyOrigin()
                                .AllowAnyMethod()
                                .AllowAnyHeader();
                      });
});

// 1.1 注册数据库 (DbContext)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// 1.2 注册 ASP.NET Core Identity
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = false; // Ensure cookie expires immediately on logout
    options.ExpireTimeSpan = TimeSpan.FromMinutes(1); // Short lifespan for session cookies
});




// 1.4 注册 API 控制器和 Razor Pages
builder.Services.AddControllers();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminPolicy", policy => policy.RequireRole(UserRoles.Admin));
});
builder.Services.AddRazorPages(options =>
{
    // Allow anonymous access to public pages
    options.Conventions.AllowAnonymousToFolder("/Public");

    // Authorize admin pages with AdminPolicy
    options.Conventions.AuthorizePage("/Index", "AdminPolicy"); // Dashboard
    options.Conventions.AuthorizeFolder("/Users", "AdminPolicy");
    options.Conventions.AuthorizeFolder("/Documents", "AdminPolicy");
    options.Conventions.AuthorizeFolder("/Settings", "AdminPolicy");

    // Allow anonymous access to login, logout, and access denied pages
    options.Conventions.AllowAnonymousToPage("/Account/Login");
    options.Conventions.AllowAnonymousToPage("/Account/Logout");
    options.Conventions.AllowAnonymousToPage("/Account/AccessDenied");
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.OperationFilter<SwaggerFileOperationFilter>();

    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "请输入 Bearer Token, 格式为: Bearer {token}"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// 1.5 注册自定义应用服务
// Repository
builder.Services.AddScoped<IPageRepository, PageRepository>();
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>(); 

// Services
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<IOcrService, TesseractOcrService>();
builder.Services.AddScoped<IImageProcessingService, OpenCvImageProcessingService>();

// App Status & Cache
builder.Services.AddSingleton<IApplicationStatusService, ApplicationStatusService>();
builder.Services.AddMemoryCache();

// Background Tasks
builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
builder.Services.AddHostedService<QueuedHostedService>();
builder.Services.AddHostedService<CacheWarmingService>();


// --- 2. 构建应用 ---
var app = builder.Build();


// --- 3. 配置 HTTP 请求管道 (中间件) ---

// 3.1 启动时自动应用数据库迁移
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        context.Database.Migrate();
        await SeedData(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

// 3.2 开发环境中间件
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

// 3.3 标准中间件
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseStatusCodePagesWithReExecute("/NotFound");
app.UseRouting();
app.UseCors(MyAllowSpecificOrigins);
app.UseAuthentication();
app.UseAuthorization();

// 3.4 映射终结点
app.MapControllers();
app.MapRazorPages();


// --- 4. 运行应用 ---
app.Run();

async Task SeedData(IServiceProvider serviceProvider)
{
    var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();
    var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

    // Seed Roles
    string[] roleNames = { UserRoles.Admin, UserRoles.User };
    foreach (var roleName in roleNames)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
            logger.LogInformation("Role '{RoleName}' created.", roleName);
        }
    }

    // Seed Default Admin User
    var defaultAdminEmail = "admin@example.com";
    var defaultAdminPassword = "admin233";

    var adminUser = await userManager.FindByEmailAsync(defaultAdminEmail);
    if (adminUser == null)
    {
        adminUser = new AppUser { UserName = defaultAdminEmail, Email = defaultAdminEmail };
        var result = await userManager.CreateAsync(adminUser, defaultAdminPassword);
        if (result.Succeeded)
        {
            logger.LogInformation("Default admin user '{Email}' created.", defaultAdminEmail);
            if (!await userManager.IsInRoleAsync(adminUser, UserRoles.Admin))
            {
                await userManager.AddToRoleAsync(adminUser, UserRoles.Admin);
                logger.LogInformation("Default admin user '{Email}' assigned to '{Role}' role.", defaultAdminEmail, UserRoles.Admin);
            }
        }
        else
        {
            logger.LogError("Failed to create default admin user '{Email}': {Errors}", defaultAdminEmail, string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }
    else
    {
        logger.LogInformation("Default admin user '{Email}' already exists.", defaultAdminEmail);
        if (!await userManager.IsInRoleAsync(adminUser, UserRoles.Admin))
        {
            await userManager.AddToRoleAsync(adminUser, UserRoles.Admin);
            logger.LogInformation("Existing admin user '{Email}' assigned to '{Role}' role.", defaultAdminEmail, UserRoles.Admin);
        }
    }
}
