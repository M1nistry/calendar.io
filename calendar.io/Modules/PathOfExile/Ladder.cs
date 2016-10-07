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
            if (!string.IsNullOrEmpty(league))
                using (var client = new WebClient()) json = client.DownloadString($"http://api.pathofexile.com/ladders/{league}?limit=200");
            try
            {
                var result = JsonConvert.DeserializeObject<Ladder>(json);
                if (filterClass != string.Empty)
                {
                    var entries = result.Participants.Where(x => string.Equals(x.Character.Class, filterClass, StringComparison.CurrentCultureIgnoreCase)).ToList();
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
