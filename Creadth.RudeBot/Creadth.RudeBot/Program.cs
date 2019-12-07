using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;

namespace Creadth.RudeBot
{
    class Program
    {
        private const string MailsFile = "mails.txt";
        private const string UsedMailsFile = "usedmails.txt";
        private const string AdminAcc = "ADMIN ACCOUNT HERE";
        private const string Role = "Первопроходец";
        private const string ServerName = "YOUR SERVER NAME";
        private const string Token = "BOT TOKEN";
        private DiscordSocketClient _client;
        private List<string> _emailList;
        private List<string> _usedList;
        private SocketGuild _guild;
        private SocketRole _role;
        private SocketGuildUser _admin;

        public static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            //reading mails db lul
            _emailList = await ReadFileToList(MailsFile);
            //reading used mails db lul
            _usedList = await ReadFileToList(UsedMailsFile);
            _client = new DiscordSocketClient();
            _client.Log += Log;
            _client.MessageReceived += ClientOnMessageReceived;
            await _client.LoginAsync(TokenType.Bot, Token);
            await _client.StartAsync();
            _client.Ready += () =>
            {
                _guild = _client.Guilds.FirstOrDefault(x => x.Name == ServerName);
                Debug.Assert(_guild != null, nameof(_guild) + " != null");
                _role = _guild.Roles.FirstOrDefault(x => x.Name == Role);
                _admin = _guild.Users.FirstOrDefault(x => x.Username == AdminAcc);
                return Task.CompletedTask;
            };
            await Task.Delay(-1);
        }

        private async Task<List<string>> ReadFileToList(string fileName)
        {
            var list = new List<string>();
            using (var file = File.Open(fileName, FileMode.OpenOrCreate))
            using (var stream = new StreamReader(file))
            {
                while (!stream.EndOfStream)
                {
                    list.Add(await stream.ReadLineAsync());
                }
            }

            return list;
        }

        private async Task InvalidateEmail(string fileName, string email)
        {
            using (var file = File.Open(fileName, FileMode.Append))
            using (var stream = new StreamWriter(file))
            {
                stream.WriteLine(email);
                _usedList.Add(email);
            }
        }

        private async Task ClientOnMessageReceived(SocketMessage message)
        {
            if (!(message.Channel is IPrivateChannel) || message.Author.IsBot) return;
            Console.WriteLine(message.Content);
            var m = (RestUserMessage) await message.Channel.GetMessageAsync(message.Id);
            if (_emailList.Contains(message.Content))
            {
                if (_usedList.Contains(message.Content))
                {
                    //scam
                    await m.AddReactionAsync(new Emoji("❌"));
                    //write admin
                    await _admin.SendMessageAsync($"{m.Author.Username} tried to use already used {message.Content} email");
                    return;
                }
                var user = _guild.GetUser(message.Author.Id);
                if (user.Roles.Any(x => x.Name == Role))
                {
                    await m.AddReactionAsync(new Emoji("❌"));
                    //no duplicate usages
                    return;
                }
                await m.AddReactionAsync(new Emoji("✔️"));
                await user.AddRoleAsync(_role);
                //invalidate
                await InvalidateEmail(UsedMailsFile, message.Content);
            }
            else
            {
                //just wrong email
                await m.AddReactionAsync(new Emoji("❌"));
            }
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
