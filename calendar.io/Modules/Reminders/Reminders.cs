using System;
using System.Collections.Generic;
using System.Threading;
using calendar.io.Modules.PathOfExile;
using Discord;
using Quartz;
using Quartz.Impl;

namespace calendar.io.Modules.Reminders
{
    public class Reminders
    {
        public static List<Reminder> UpcomingReminders { get; set; }
        public IScheduler _scheduler;

        public Reminders()
        {
            _scheduler = StdSchedulerFactory.GetDefaultScheduler();
            _scheduler.Start();
        }

        public bool LoadRaceReminders()
        {
            var _racing = new Racing();
            var upcomingEvents = _racing.UpcomingEvents;
            //Load each race into a reminder, reminder time is set in the Reminder class
            foreach (var raceEvent in upcomingEvents)
            {
                new Reminder(raceEvent, _scheduler);
            }
            return true;
        }
    }
}
