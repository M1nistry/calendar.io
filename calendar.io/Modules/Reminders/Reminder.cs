using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using calendar.io.Modules.PathOfExile;
using Discord;
using Quartz;

namespace calendar.io.Modules.Reminders
{
    /// <summary>
    /// Set reminders using syntax: !reminder (time (eg. 1h, 15:30)) (message ("hello world!")) (PM ("y/n") *optional)
    ///                             !reminder 1h "Hello World!" y
    /// </summary>
    public class Reminder
    {
        public Reminder(int id, string message, DateTime reminderTime, bool PM, User owner, IScheduler scheduler)
        {
            Id = id;
            Message = message;
            ReminderTime = reminderTime;
            PrivateMessage = PM;
            Owner = owner;

            var job = JobBuilder.Create<GenericReminder>()
                    .WithIdentity(id.ToString(), "UserReminders")
                    .WithDescription(message)
                    .Build();
            scheduler.Context.Put(id.ToString(), this);

            var trigger = TriggerBuilder.Create()
                .WithIdentity(id.ToString(), "UserReminders")
                .StartAt(reminderTime)
                .WithSimpleSchedule(x => x
                    .WithRepeatCount(0))
                .Build();

            scheduler.ScheduleJob(job, trigger);
        }

        public Reminder(Racing.RaceEvent raceEvent, IScheduler scheduler)
        {
            Message = raceEvent.ID;
            PrivateMessage = false;
            var job = JobBuilder.Create<RaceReminder>()
                    .WithIdentity(raceEvent.ID, "Races")
                    .WithDescription(raceEvent.ID)
                    .Build();
            scheduler.Context.Put(raceEvent.ID, raceEvent);

            ///TODO: We can set variables for when to have alerts for a race (ie 30m, 10m, 3m etc)
            var trigger = TriggerBuilder.Create()
                .WithIdentity(raceEvent.ID, "Races")
                .StartAt(DateTime.Now.AddSeconds(15))
                .WithSimpleSchedule(x => x
                    .WithRepeatCount(1)
                    .WithIntervalInMinutes(15))
                .Build();

            scheduler.ScheduleJob(job, trigger);
        }

        public class RaceReminder : IJob
        {
            public void Execute(IJobExecutionContext context)
            {
                var schedulerContext = context.Scheduler.Context;
                var RaceReminder = (Racing.RaceEvent) schedulerContext.Get(context.JobDetail.Key.Name);
                InitiateBot._this.SendRaceReminder(RaceReminder);
                Console.ReadLine();
            }
        }

        public class GenericReminder : IJob
        {
            public void Execute(IJobExecutionContext context)
            {
                var schedulerContext = context.Scheduler.Context;
                var myObj = (Reminder)schedulerContext.Get(context.JobDetail.Key.Name);
                InitiateBot._this.SendPersonalReminder(myObj);
                Console.ReadLine();
            }
        }


        public int Id { get; set; }
        public string Message { get; set; }
        public DateTime ReminderTime { get; set; }
        public bool PrivateMessage { get; set; }
        public User Owner { get; set; }
    }
}
