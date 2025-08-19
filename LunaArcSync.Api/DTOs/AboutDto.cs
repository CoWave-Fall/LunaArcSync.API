namespace LunaArcSync.Api.DTOs
{
    public class AboutDto
    {
        public string AppName { get; set; } = "LunaArcSync API";
        public string Version { get; set; } = "0.0.1"; // 可以从配置或程序集读取
        public string Description { get; set; } = "A powerful backend for an intelligent document management application.";
        public string Contact { get; set; } = "https://github,com/CoWave-Fall/LunaArcSync.API"; // 联系方式
    }
}