using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json.Linq;

namespace calendar.io.Modules.PathOfExile
{
    public static class Player
    {
        public static Exile GetCharacter(string name, string league)
        {
            string json;
            using (var client = new WebClient())
                json = client.DownloadString($"http://api.exiletools.com/ladder?league={league}&charName={name}&short=1");
            //var result = JsonConvert.DeserializeObject(json);

            var result = JObject.Parse(json).First;
            long exp;
            long.TryParse(result.First.Value<string>("experience"), out exp);
            var character = new Exile
            {
                Name = result.First.Value<string>("charName"),
                Class = result.First.Value<string>("class"),
                Dead = result.First.Value<string>("dead") == "1",
                Experience = exp,
                Level = result.First.Value<int>("level"),
                Rank = result.First.Value<int>("rank"),
                Online = result.First.Value<string>("online") == "1"
            };
            
            return character;
        }

        public class Exile
        {
            public bool Dead { get; set; }
            public long Experience { get; set; }
            public int Level { get; set; }
            public string Class { get; set; }
            public string Name { get; set; }
            public int Rank { get; set; }
            public bool Online { get; set; }
        }

        public class Result
        {
            public Dictionary<string, Exile> ResultCharacter { get; set; } 
        }
    }
}
