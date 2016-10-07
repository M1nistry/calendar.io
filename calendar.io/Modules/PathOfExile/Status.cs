using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json;

namespace calendar.io.Modules.PathOfExile
{
    public static class Status
    {
        public static List<League> ActiveLeagues
        {
            get
            {
                string json;
                using (var client = new WebClient())
                    json = client.DownloadString("http://api.exiletools.com/status");
                var status = JsonConvert.DeserializeObject<Result>(json);
                var leagueList = new List<League>();
                foreach (var league in status.Leagues)
                {
                    league.Value.ApiName = league.Key;
                    league.Value.URL = LeagueUrl(league.Value.prettyName);
                    leagueList.Add(league.Value);
                }
                return leagueList;
            }
        }

        public static string LeagueUrl(string league)
        {
           
            string json;
            using (var client = new WebClient())
                json = client.DownloadString($"http://api.pathofexile.com/leagues?type=league&id={league}");
            var url = JsonConvert.DeserializeObject<GGGLeague>(json);
            return url.url;
        }

        internal class GGGLeague
        {
            public string url { get; set; }
        }

        public class League
        {
            public string endTime { get; set; }
            public string shopForumID { get; set; }
            public string officialapiName { get; set; }
            public string shopForumURL { get; set; }
            public string startTime { get; set; }
            public string prettyName { get; set; }
            public string itemjsonName { get; set; }
            public string ApiName { get; set; }
            public string URL { get; set; }
        }

        public class Result
        {
            [JsonProperty("Active Leagues")]
            public Dictionary<string, League> Leagues { get; set; }
        }
    }
}
