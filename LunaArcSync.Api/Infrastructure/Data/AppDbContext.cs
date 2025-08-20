using Microsoft.EntityFrameworkCore;
using LunaArcSync.Api.Core.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace LunaArcSync.Api.Infrastructure.Data
{
    // --- 修改这里的继承关系 ---
    public class AppDbContext : IdentityDbContext<AppUser> // 指定用户实体类型
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Page> Pages { get; set; }
        public DbSet<Core.Entities.Version> Versions { get; set; }
        public DbSet<Job> Jobs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 必须调用 base.OnModelCreating(modelBuilder)，因为它会配置所有 Identity 相关的实体
            base.OnModelCreating(modelBuilder);

            // --- 添加新的实体关系配置 ---
            modelBuilder.Entity<AppUser>()
                .HasMany(u => u.Pages) // 一个用户有多个文档
                .WithOne(d => d.User)      // 一个文档有一个用户
                .HasForeignKey(d => d.UserId); // 外键是 UserId
        }
    }
}