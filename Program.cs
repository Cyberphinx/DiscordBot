using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace MrHyde
{
    class Program
    {
        public static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();
        
        private DiscordSocketClient _client;
        private CommandService _commands;
        private IServiceProvider _services;
        public async Task MainAsync()
        {
            _client = new DiscordSocketClient();
            _commands = new CommandService();
            _services = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_commands)
                .BuildServiceProvider();

            _client.Log += Log;
            _client.Ready += ReadyAsync;

            string token = File.ReadAllText("token.txt");

            await InstallCommandAsync();
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            await Task.Delay(-1);
        }

        // logging data
        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        //the ready event
        private Task ReadyAsync()
        {
            Console.WriteLine($"Connected as : {_client.CurrentUser}");
            return Task.CompletedTask;
        }

        // command service method
        private async Task InstallCommandAsync()
        {
            _client.MessageReceived += HandleCommandAsync;
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }

        // command handler method
        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            var now = DateTime.Now;
            var date = new DateTime(2021,10,30,12,00,00);

            var message = messageParam as SocketUserMessage;
            if (message == null) return;
            
            var context = new SocketCommandContext(_client, message);

            int argPos = 0;

            if (!(message.HasStringPrefix("!", ref argPos) ||
                message.HasMentionPrefix(_client.CurrentUser, ref argPos)) ||
                message.Author.IsBot)
                return;

            var result = await _commands.ExecuteAsync(context, argPos, _services);

            if (!result.IsSuccess)
            {
                Console.WriteLine(now + " : " + result.ErrorReason);
                await message.Channel.SendMessageAsync(now + " : " + result.ErrorReason);
            }

        }

    }
}
