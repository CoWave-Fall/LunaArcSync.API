using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
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

// --- 1. ÷ (ע) ---

// +++  CORS  +++
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          // ڿУκԴκηκͷ
                          policy.AllowAnyOrigin()
                                .AllowAnyMethod()
                                .AllowAnyHeader();
                      });
});

// 1.1 עݿ (DbContext)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// 1.2  ASP.NET Core Identity
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    // 
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// 1.3  JWT (JSON Web Token) ֤
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment(); // Set to true in production
    options.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidAudience = builder.Configuration["JWT:ValidAudience"],
        ValidIssuer = builder.Configuration["JWT:ValidIssuer"],
        // Կ֤ Token ǩĹؼ
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Secret"]))
    };
});

// 1.4 ע API ط
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // 1. 尲ȫ (Security Scheme)
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization", // Ҫӵͷе key
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http, //  Http
        Scheme = "Bearer", // ֤ Bearer
        BearerFormat = "JWT", // Bearer ĸʽ JWT
        In = Microsoft.OpenApi.Models.ParameterLocation.Header, // Token λͷ
        Description = " Bearer Token, ʽΪ: Bearer {token}"
    });

    // 2. ȫְȫҪ (Security Requirement)
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer" //  Id  AddSecurityDefinition еĵһһ
                }
            },
            new string[] {}
        }
    });
});
// 1.5 עԶӦ÷
// Repository
builder.Services.AddScoped<IPageRepository, PageRepository>();
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>(); 

// ķ
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<IOcrService, TesseractOcrService>();
builder.Services.AddScoped<IImageProcessingService, OpenCvImageProcessingService>();

// ̨ (Singleton ֤ȫΨһ)
builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();

// йܵĺ̨
builder.Services.AddHostedService<QueuedHostedService>();


// --- 2. Ӧó ---
var app = builder.Build();


// --- 3.  HTTP ܵ (м) ---

// 3.1 ڳʱԶӦݿǨ
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        // ȷݿⱻӦйǨ
        context.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

// 3.2 ÿм
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    // ڿУʾϸ쳣ҳ
    app.UseDeveloperExceptionPage();
}

// 3.3 м˳ǳҪ
app.UseHttpsRedirection();

// ·
app.UseRouting();

// +++  CORS м +++
app.UseCors(MyAllowSpecificOrigins);

// ֤м ( UseAuthorization ֮ǰ)
app.UseAuthentication();

// Ȩм
app.UseAuthorization();

// ӳ䵽
app.MapControllers();


// --- 4. Ӧó ---
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
    var defaultAdminPassword = "admin"; // Consider using a stronger default password in production

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
        // Ensure admin user has the Admin role if they somehow don't
        if (!await userManager.IsInRoleAsync(adminUser, UserRoles.Admin))
        {
            await userManager.AddToRoleAsync(adminUser, UserRoles.Admin);
            logger.LogInformation("Existing admin user '{Email}' assigned to '{Role}' role.", defaultAdminEmail, UserRoles.Admin);
        }
    }
}
