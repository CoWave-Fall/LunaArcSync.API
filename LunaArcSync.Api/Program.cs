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
using System;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// --- 1. 配置服务 (依赖注入容器) ---

// 1.1 注册数据库上下文 (DbContext)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// 1.2 配置 ASP.NET Core Identity
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    // 在这里配置密码策略
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// 1.3 配置 JWT (JSON Web Token) 认证
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false; // 在开发环境中可以设为 false，生产环境应为 true
    options.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidAudience = builder.Configuration["JWT:ValidAudience"],
        ValidIssuer = builder.Configuration["JWT:ValidIssuer"],
        // 密钥是验证 Token 签名的关键
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Secret"]))
    };
});

// 1.4 注册控制器和 API 相关服务
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // 1. 定义安全方案 (Security Scheme)
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization", // 要添加到请求头中的 key
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http, // 类型是 Http
        Scheme = "Bearer", // 认证方案是 Bearer
        BearerFormat = "JWT", // Bearer 的格式是 JWT
        In = Microsoft.OpenApi.Models.ParameterLocation.Header, // Token 的位置在请求头
        Description = "请输入 Bearer Token, 格式为: Bearer {token}"
    });

    // 2. 添加全局安全要求 (Security Requirement)
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer" // 这里的 Id 必须与上面 AddSecurityDefinition 中的第一个参数一致
                }
            },
            new string[] {}
        }
    });
});
// 1.5 注册自定义的应用服务
// Repository
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();

// 核心服务
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<IOcrService, TesseractOcrService>();
builder.Services.AddScoped<IImageProcessingService, OpenCvImageProcessingService>();

// 后台任务队列 (Singleton 保证全局唯一)
builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();

// 托管的后台工作服务
builder.Services.AddHostedService<QueuedHostedService>();


// --- 2. 构建应用程序 ---
var app = builder.Build();


// --- 3. 配置 HTTP 请求处理管道 (中间件) ---

// 3.1 在程序启动时自动应用数据库迁移
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        // 确保数据库被创建并应用所有挂起的迁移
        context.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

// 3.2 配置开发环境和生产环境的中间件
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    // 在开发环境中，显示详细的异常页面
    app.UseDeveloperExceptionPage();
}

// 3.3 核心中间件（顺序非常重要！）
app.UseHttpsRedirection();

// 启用路由
app.UseRouting();

// 启用认证中间件 (必须在 UseAuthorization 之前)
app.UseAuthentication();

// 启用授权中间件
app.UseAuthorization();

// 将请求映射到控制器
app.MapControllers();


// --- 4. 运行应用程序 ---
app.Run();