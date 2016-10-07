using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Newtonsoft.Json;

namespace calendar.io.Modules.PathOfExile
{
    public static class Ladders
    {
        public static Ladder GetLadder(string league, string filterClass = "")
        {
            var json = "";
            if (!String.IsNullOrEmpty(league))
                using (var client = new WebClient()) json = client.DownloadString($"http://api.pathofexile.com/ladders/{league}?limit=200");
            try
            {
                var result = JsonConvert.DeserializeObject<Ladder>(json);
                if (filterClass != String.Empty)
                {
                    var entries = result.Participants.Where(x => String.Equals(x.Character.Class, filterClass, StringComparison.CurrentCultureIgnoreCase)).ToList();
                    var filterResult = new Ladder
                    {
                        Participants = entries,
                        Total = entries.Count
                    };
                    return filterResult;
                }
                return result;
            }
            catch (JsonReaderException e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }

        public static string BuildLadderMessage(Ladders.Ladder ladder, Racing.RaceEvent raceEvent = null, Status.League league = null)
        {
            var message = "";
            if (raceEvent != null)
            {
                var activeRace = raceEvent.StartDt <= DateTime.Now && raceEvent.EndDt >= DateTime.Now;
                message = $"{raceEvent.ID} | <{raceEvent.URL}> | {(activeRace ? $"Ends in {(DateTime.Now - raceEvent.EndDt).ToString("hh\\:mm\\:ss")}" : "Finished")} {Environment.NewLine}```";
            }
            else if (league != null)
            {
                message = $"{league.prettyName} | <{league.URL}> {Environment.NewLine}```";
            }
            const int maxResults = 10;
            var postedResults = 0;
            foreach (var entry in ladder?.Participants.TakeWhile(entry => postedResults != maxResults))
            {
                message += $"{Environment.NewLine}{(entry.Online ? "+" : "-")} [{entry.Rank,-2}] {entry.Account.Name,-16} | {entry.Character.Name,-23} | {entry.Character.Class,-12} | {entry.Character.Experience.ToString("N0"),-10} xp";
                postedResults++;
            }

            return message + "```";
        }

        public class Ladder
        {
            [JsonProperty("total")]
            public int Total { get; set; }

            [JsonProperty("entries")]
            public List<Entry> Participants { get; set; }

            public class Character
            {
                [JsonProperty("name")]
                public string Name { get; set; }

                [JsonProperty("level")]
                public int Level { get; set; }

                [JsonProperty("class")]
                public string Class { get; set; }

                [JsonProperty("experience")]
                public long Experience { get; set; }
            }

            public class Challenges
            {
                [JsonProperty("total")]
                public int Total { get; set; }
            }

            public class Twitch
            {
                [JsonProperty("name")]
                public string Name { get; set; }
            }

            public class Account
            {
                [JsonProperty("name")]
                public string Name { get; set; }

                [JsonProperty("challenges")]
                public Challenges Challenges { get; set; }

                [JsonProperty("twitch")]
                public Twitch Twitch { get; set; }
            }

            public class Entry
            {
                [JsonProperty("online")]
                public bool Online { get; set; }

                [JsonProperty("rank")]
                public int Rank { get; set; }

                [JsonProperty("dead")]
                public bool Dead { get; set; }

                [JsonProperty("character")]
                public Character Character { get; set; }

                [JsonProperty("account")]
                public Account Account { get; set; }
            }
        }
    }
}
