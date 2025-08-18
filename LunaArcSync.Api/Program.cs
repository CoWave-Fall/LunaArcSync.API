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

// --- 1. ���÷��� (����ע������) ---

// 1.1 ע�����ݿ������� (DbContext)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// 1.2 ���� ASP.NET Core Identity
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    // �����������������
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// 1.3 ���� JWT (JSON Web Token) ��֤
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false; // �ڿ��������п�����Ϊ false����������ӦΪ true
    options.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidAudience = builder.Configuration["JWT:ValidAudience"],
        ValidIssuer = builder.Configuration["JWT:ValidIssuer"],
        // ��Կ����֤ Token ǩ���Ĺؼ�
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Secret"]))
    };
});

// 1.4 ע��������� API ��ط���
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // 1. ���尲ȫ���� (Security Scheme)
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization", // Ҫ��ӵ�����ͷ�е� key
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http, // ������ Http
        Scheme = "Bearer", // ��֤������ Bearer
        BearerFormat = "JWT", // Bearer �ĸ�ʽ�� JWT
        In = Microsoft.OpenApi.Models.ParameterLocation.Header, // Token ��λ��������ͷ
        Description = "������ Bearer Token, ��ʽΪ: Bearer {token}"
    });

    // 2. ���ȫ�ְ�ȫҪ�� (Security Requirement)
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer" // ����� Id ���������� AddSecurityDefinition �еĵ�һ������һ��
                }
            },
            new string[] {}
        }
    });
});
// 1.5 ע���Զ����Ӧ�÷���
// Repository
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();

// ���ķ���
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<IOcrService, TesseractOcrService>();
builder.Services.AddScoped<IImageProcessingService, OpenCvImageProcessingService>();

// ��̨������� (Singleton ��֤ȫ��Ψһ)
builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();

// �йܵĺ�̨��������
builder.Services.AddHostedService<QueuedHostedService>();


// --- 2. ����Ӧ�ó��� ---
var app = builder.Build();


// --- 3. ���� HTTP ������ܵ� (�м��) ---

// 3.1 �ڳ�������ʱ�Զ�Ӧ�����ݿ�Ǩ��
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        // ȷ�����ݿⱻ������Ӧ�����й����Ǩ��
        context.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

// 3.2 ���ÿ��������������������м��
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    // �ڿ��������У���ʾ��ϸ���쳣ҳ��
    app.UseDeveloperExceptionPage();
}

// 3.3 �����м����˳��ǳ���Ҫ����
app.UseHttpsRedirection();

// ����·��
app.UseRouting();

// ������֤�м�� (������ UseAuthorization ֮ǰ)
app.UseAuthentication();

// ������Ȩ�м��
app.UseAuthorization();

// ������ӳ�䵽������
app.MapControllers();


// --- 4. ����Ӧ�ó��� ---
app.Run();