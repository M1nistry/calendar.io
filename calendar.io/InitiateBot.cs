using System;
using System.IO;
using System.Linq;
using System.Net;
using calendar.io.Modules.PathOfExile;
using calendar.io.Modules.Reminders;
using Discord;
using Discord.Commands;
using Discord.Commands.Permissions.Levels;

namespace calendar.io
{
    public class InitiateBot
    {
        public static DiscordClient Client { get; set; }

        private static Racing _racing;
        private static Reminders _reminders;

        public static InitiateBot _this;

        public InitiateBot()
        {
            Client = new DiscordClient();
            _this = this;
            _racing = new Racing();
            _reminders = new Reminders();

            Client.UsingCommands(x =>
            {
                x.PrefixChar = '.';
                x.HelpMode = HelpMode.Public;
            });

            Commands();

            Client.Ready += ClientOnReady;

            Client.MessageReceived += ClientOnMessageReceived;

            Client.ExecuteAndWait(async () =>
            {
                var keyString = File.ReadAllText(Environment.CurrentDirectory + @"\Key.txt");
                await Client.Connect(keyString, TokenType.Bot);
            });
        }

        public static InitiateBot GetBot()
        {
            return _this;
        }

        private static void ClientOnReady(object sender, EventArgs eventArgs)
        {
            Console.WriteLine($"Status: {Client.Status} - {DateTime.Now.ToString("F")}");
            Console.WriteLine($"Current servers: ");
            foreach (var server in Client.Servers) Console.WriteLine(server.Name);
        }

        private static async void ClientOnMessageReceived(object sender, MessageEventArgs e)
        {

        }

        public async void SendRaceReminder(Racing.RaceEvent raceEvent)
        {
            await Client.Servers
                    .First(x => x.Id == 208852032465928192)
                    .GetChannel(208852308417576961)
                    .SendMessage($"Race reminder: ``{raceEvent.ID}`` - Starts in ``{(raceEvent.StartDt - DateTime.Now.AddSeconds(1)).ToString(@"hh\:mm\:ss")}``");
        }

        public async void SendPersonalReminder(Reminder reminder)
        {
            await Client.Servers
                .First(x => x.Id == 208852032465928192)
                .GetUser(reminder.Owner.Id)
                .SendMessage($"Reminder: {reminder.Message}");
        }

        public void Commands()
        {

            #region Path of Exile Commmands
            Client.GetService<CommandService>().CreateGroup("ladder", cgb =>
            {
                cgb.CreateCommand("race")
                    .Parameter("Class", ParameterType.Optional)
                    .Description("Gets the ladder for the most recent/currently running race")
                    .Do(async e =>
                    {
                        Ladders.Ladder ladder;
                        Racing.RaceEvent selectedEvent;
                        var filterClass = e.GetArg("Class");

                        //checks if there's an active race
                        var activeRace = _racing.ActiveEvent;
                        if (activeRace != null)
                        {
                            ladder = Ladders.GetLadder(activeRace.ID, filterClass);
                            selectedEvent = activeRace;
                        }
                        else
                        {
                            var mostRecent = _racing.MostRecentEvent;
                            ladder = Ladders.GetLadder(mostRecent.ID, filterClass);
                            selectedEvent = mostRecent;
                        }

                        if (ladder == null) await e.Channel.SendMessage($"There was an issue fetching the {(activeRace != null ? @"current" : @"most recent")} ladder");

                        var message = Ladders.BuildLadderMessage(ladder, selectedEvent, null);
                        await e.Channel.SendMessage(message);
                    });

                cgb.CreateCommand("temphc")
                    .Description("Gets the ladder for the current temporary hardcore league")
                    .Do(async e =>
                    {
                        ///TODO: This will get deprecated with the end of ExileTools -- How else do we determine the currect temporary hardcore league titles?
                        var active = Status.ActiveLeagues;
                        //this isn't the best solution to finding the temp hc league, but it works... for now
                        var hcTempLeague= active.FirstOrDefault(x => x.ApiName.EndsWith("hc"));
                        var hcTempLadder = Ladders.GetLadder(hcTempLeague?.prettyName);
                        var message = Ladders.BuildLadderMessage(hcTempLadder, null, hcTempLeague);
                        await e.Channel.SendMessage(message);
                    });

                cgb.CreateCommand("tempsc")
                    .Description("Gets the ladder for the current temporary softcore league")
                    .Do(async e =>
                    {
                        ///TODO: This will get deprecated with the end of ExileTools -- How else do we determine the currect temporary hardcore league titles?
                        var active = Status.ActiveLeagues;
                        var scTempLeague = active.FirstOrDefault(x => x.ApiName != "hardcore" && x.ApiName != "standard" && !x.ApiName.EndsWith("hc"));
                        var scTempLadder = Ladders.GetLadder(scTempLeague?.prettyName);
                        var message = Ladders.BuildLadderMessage(scTempLadder, null, scTempLeague);
                        await e.Channel.SendMessage(message);
                    });

                ///TODO: This will be deprecated when exiletools shuts down. Fetches: Online | Rank | Name | Class | Level | Dead from temporary leagues.
                cgb.CreateCommand("position")
                    .Description("Returns the position on the ladder for the given character and league")
                    .Parameter("Character")
                    .Parameter("League")
                    .Do(async e =>
                    {
                        var character = Player.GetCharacter(e.GetArg("Character"), e.GetArg("League"));
                        await e.Channel.SendMessage($"``` {(character.Online? "+" : "-")} [{character.Rank,-5}] {character.Name, -23} | {character.Class,-12} | Level: {character.Level, -2} | Dead: {character.Dead}```");
                    });
            });

            Client.GetService<CommandService>().CreateGroup("race", cgb =>
            {
                cgb.CreateCommand("upcoming")
                    .Description("Displays the next 3 upcoming races")
                    .Do(async e =>
                    {
                        await e.Channel.SendMessage(
                        $"The next 3 races are (AEST): {Environment.NewLine}" +
                        $"```{_racing.UpcomingEvents[0].ID,-25} | {_racing.UpcomingEvents[0].StartDt.ToString("dddd hh:mm:ss tt")}```<{_racing.UpcomingEvents[0].URL}>{Environment.NewLine}" +
                        $"```{_racing.UpcomingEvents[1].ID,-25} | {_racing.UpcomingEvents[1].StartDt.ToString("dddd hh:mm:ss tt")}```<{_racing.UpcomingEvents[1].URL}>{Environment.NewLine}" +
                        $"```{_racing.UpcomingEvents[2].ID,-25} | {_racing.UpcomingEvents[2].StartDt.ToString("dddd hh:mm:ss tt")}```<{_racing.UpcomingEvents[2].URL}>");
                    });

                cgb.CreateCommand("next")
                    .Description("Displays the next upcoming race")
                    .Do(async e =>
                    {
                        await
                    e.Channel.SendMessage(
                        $"The next race is ``{_racing.UpcomingEvents[0].ID}`` starting in ``{(_racing.UpcomingEvents[0].StartDt - DateTime.Now).ToString(@"hh\:mm\:ss")}`` - <{_racing.UpcomingEvents[0].URL}>");
                    });
            });

            Client.GetService<CommandService>().CreateCommand("wiki")
                .Description("Searches the Path of Exile wiki for the given term")
                .Parameter("Query")
                .Do(async e =>
                {
                    await e.Channel.SendMessage($"{e.User.Mention} <http://pathofexile.gamepedia.com/Special:Search/{e.GetArg("Query")}>");
                });
            #endregion

            Client.GetService<CommandService>().CreateCommand("reminder")
                .Description("Sets a reminder for the bot to PM you with an optional message.")
                .Parameter("ReminderTime")
                .Parameter("Message")
                .Do(async e =>
                {
                    try
                    {
                        ///TODO: INSERT REMINDERS INTO DATABASE INCASE OF BOT CLOSE, LOAD THEM LATER
                        var inputTime = e.GetArg("ReminderTime");
                        var parsed = RelativeDateParser.Parse(inputTime);
                        new Reminder(0, e.GetArg("Message"), parsed, true, e.User, _reminders._scheduler);
                        await e.Channel.SendMessage(e.User.Mention + $" Reminder set for ``{(parsed - DateTime.Now).ToString("dd\\:hh\\:mm\\:ss")}`` from now.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                });
        }
    }
}
