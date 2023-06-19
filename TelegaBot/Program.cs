using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using OpenAI_API;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using File = System.IO.File;

namespace TelegaBot;

class Program
{
    static async Task Main(string[] args)
    {
        var botClient = new TelegramBotClient(AppConfig.BotToken);
        var ccc = AppConfig.OpenAIToken;
        using var cts = new CancellationTokenSource();
        var receiverOptions = new ReceiverOptions()
        {
            AllowedUpdates = { }
        };

        botClient.StartReceiving(
            HandleUpdate,
            HandleError,
            receiverOptions,
            cancellationToken: cts.Token
        );

        var me = await botClient.GetMeAsync(cancellationToken: cts.Token);
        Console.WriteLine($"Start listening for @{me.Username}");
        while (true)
        {
            var folderPath = @"D:\AI\OpenAI\Pictures\Matched";
            var files = Directory.GetFiles(folderPath, "*.jpg");
            if (files.Length <= 0)
            {
                await Task.Delay(5000, cts.Token);
                continue;
            }

            foreach (var file in files)
            {
                var photo = File.OpenRead(file);
                await botClient.SendPhotoAsync(-1001423926098, photo, cancellationToken: cts.Token);
                photo.Close();
                File.Delete(file);
            }

            try
            {
                var openAi =
                    new OpenAIAPI(new APIAuthentication(AppConfig.OpenAIToken));
                var conversation = openAi.Chat.CreateConversation();
                var messageLength = new Random().Next(20, 35);
                conversation.AppendUserInput(
                    $"Напиши короткое развлекательное сообщение с шутками и приколами для телеграмм о том что привезли еду. Длина сообщения примерно {messageLength} слов");
                var responseOpenAi = await conversation.GetResponseFromChatbotAsync();
                await botClient.SendTextMessageAsync(-1001423926098, responseOpenAi, cancellationToken: cts.Token);
            }
            catch (HttpRequestException ex)
            {
                if (ex.Message.Contains("insufficient_quota"))
                {
                    Console.WriteLine("Error: Insufficient quota. Please check your plan and billing details.");
                }
                else
                {
                    Console.WriteLine("Error: An HTTP request exception occurred: " + ex.Message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: An exception occurred: " + ex.Message);
            }
        }
    }

    private static Task HandleUpdate(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private static Task HandleMessage(ITelegramBotClient botClient, Message message)
    {
        return Task.CompletedTask;
    }

    private static string _lastErrorMessage = string.Empty;
    private static DateTime _lastErrorTimestamp = DateTime.MinValue;

    private static Task HandleError(ITelegramBotClient client, Exception exception, CancellationToken ct)
    {
        var errorMessage = exception switch
        {
            Telegram.Bot.Exceptions.RequestException
                {
                    InnerException: System.Net.Http.HttpRequestException httpException
                } =>
                $"Telegram HTTP request error: {httpException.Message}",
            Telegram.Bot.Exceptions.ApiRequestException apiRequestException =>
                $"Telegram API error: {apiRequestException.ErrorCode} - {apiRequestException.Message}",
            System.Net.Sockets.SocketException socketException =>
                $"Network error: {socketException.Message}",
            System.Net.Http.HttpRequestException httpException =>
                $"HTTP request error: {httpException.Message}",
            System.Threading.Tasks.TaskCanceledException taskCanceledException =>
                $"The request was canceled: {taskCanceledException.Message}",
            _ => exception.ToString()
        };

        if (errorMessage != _lastErrorMessage || (DateTime.Now - _lastErrorTimestamp).TotalSeconds >= 1)
        {
            _lastErrorMessage = errorMessage;
            _lastErrorTimestamp = DateTime.Now;

            Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ms") + " " + errorMessage);
        }

        return Task.CompletedTask;
    }
}