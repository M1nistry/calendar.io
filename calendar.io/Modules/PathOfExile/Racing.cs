using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using Newtonsoft.Json;

namespace calendar.io.Modules.PathOfExile
{
    public class Racing
    {
        private readonly List<RaceEvent> _raceEvents;

        public Racing()
        {
            _raceEvents = Events;
        }

        public List<RaceEvent> UpcomingEvents => _raceEvents.Where(e => e.StartDt >= DateTime.Now).ToList();

        public RaceEvent ActiveEvent => _raceEvents.FirstOrDefault(e => e.StartDt <= DateTime.Now && e.EndDt >= DateTime.Now);

        public RaceEvent MostRecentEvent => _raceEvents.Last(x => x.EndDt <= DateTime.Now);

        public static List<RaceEvent> Events
        {
            get
            {
                string json;
                using (var client = new WebClient()) json = client.DownloadString("http://api.pathofexile.com/leagues?type=season&season=Medallion&compact=1");
                return JsonConvert.DeserializeObject<RaceEvent[]>(json).Where(x => !string.IsNullOrEmpty(x.End) && !string.IsNullOrEmpty(x.Start)).ToList();
            }
        }

        public class RaceEvent
        {
            [JsonProperty("id")]
            public string ID { get; set; }

            [JsonProperty("url")]
            public string URL { get; set; }

            [JsonProperty("startAt")]
            public string Start { get; set; }

            public DateTime StartDt => DateTime.ParseExact(Start, "yyyy-MM-ddTHH:mm:ssZ", CultureInfo.CurrentCulture, DateTimeStyles.AssumeUniversal);

            [JsonProperty("endAt")]
            public string End { get; set; }

            public DateTime EndDt => DateTime.ParseExact(End, "yyyy-MM-ddTHH:mm:ssZ", CultureInfo.CurrentCulture, DateTimeStyles.AssumeUniversal);

        }
    }
}
