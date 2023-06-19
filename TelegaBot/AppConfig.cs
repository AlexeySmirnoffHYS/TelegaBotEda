using Microsoft.Extensions.Configuration;

namespace TelegaBot;

public static class AppConfig
{
    private static IConfiguration _configuration;

    static AppConfig()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddUserSecrets<Program>();
        _configuration = builder.Build();
    }

    public static string BotToken => _configuration["BotToken"];
    //public static string BotToken => _configuration.GetSection("BotToken")["TestBot"];
    public static string OpenAIToken => _configuration["OpenAIToken"];
}