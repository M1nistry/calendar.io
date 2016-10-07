using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;

namespace calendar.io.Modules.Reminders
{
    public static class RelativeDateParser
    {
        public const string DateTimeTagPattern = "yyyy-MM-dd HH:mm";
        public const string DateTimeFriendlyPattern = "dd-MMM-yy HH:mm";

        private static object _calendarUtilLock = new object();
        private static dynamic _calendarUtil;
        private static dynamic CalendarUtil
        {
            get
            {
                if (_calendarUtil == null)
                {
                    LoadParseDateTimeLibrary();
                    // Debug.Assert(_calendarUtil != null, "_calendar may not be null after python library loaded");
                }

                return _calendarUtil;
            }
        }

        static RelativeDateParser()
        {
            PreloadAsync();
        }

        public static void PreloadAsync()
        {
            // Loading Python's library takes a while
            ThreadPool.QueueUserWorkItem(delegate { LoadParseDateTimeLibrary(); });
        }

        private static void LoadParseDateTimeLibrary()
        {
            lock (_calendarUtilLock)
            {
                if (_calendarUtil == null)
                {
                    string pythonFolder = Path.Combine(Path.GetDirectoryName(Assembly.GetCallingAssembly().Location), "python");
                    string parseDateTimeFolder = Path.Combine(pythonFolder, "parsedatetime");
                    string parseDateTimeScript = Path.Combine(parseDateTimeFolder, "parsedatetime.py");

                    ScriptEngine engine = Python.CreateEngine();
                    engine.SetSearchPaths(new[]
                                              {
                                                  parseDateTimeFolder,
                                                  Path.Combine(pythonFolder, "lib")
                                              });

                    ScriptSource source = engine.CreateScriptSourceFromFile(parseDateTimeScript);
                    ScriptScope scope = engine.CreateScope();
                    ObjectOperations op = engine.Operations;
                    source.Execute(scope);

                    dynamic Calendar = scope.GetVariable("Calendar");
                    _calendarUtil = Calendar();

                    // Causes further initialization - Without it, first call to parse would take a bit longer
                    CalendarUtil.parse("today");
                }
            }
        }

        public static DateTime Parse(string input)
        {
            DateTime parsedDateTime;
            bool success = TryParse(input, out parsedDateTime);
            if (success == false)
            {
                throw new InvalidDataException("Failed to parse: " + input);
            }

            return parsedDateTime;
        }

        public static bool TryParse(string input, out DateTime parsedDateTime)
        {
            bool tryParseSuccess = DateTime.TryParseExact(input, DateTimeTagPattern, null, DateTimeStyles.None, out parsedDateTime);
            tryParseSuccess = tryParseSuccess || DateTime.TryParseExact(input, DateTimeFriendlyPattern, null, DateTimeStyles.None, out parsedDateTime);

            if (tryParseSuccess == false)
            {
                if (input == "today")
                {
                    parsedDateTime = DateTime.Today;
                    tryParseSuccess = true;
                }
                else
                {
                    var result = CalendarUtil.parse(input);
                    // The [0] tuple looks like that:
                    // [0][0] year, [0][1] month, [0][2] day, [0][3] hour, [0][4] minute, [0][5] seconds, [0][6] day in week, [0][7] day in year, [0][8] isDst
                    // http://docs.python.org/library/time.html
                    // [1] Is a flag that reports on the kind of parsing that was done:
                    // 0 none, 1 date only, 2 time only, 3 both date and time
                    parsedDateTime = new DateTime(result[0][0], result[0][1], result[0][2], result[0][3], result[0][4], result[0][5]);

                    int flag = result[1];

                    if (flag != 0) // If succeeded to parse
                    {
                        tryParseSuccess = true;

                        if (flag == 1)
                        {
                            // If time isn't specified, then discard time information
                            parsedDateTime = parsedDateTime.Date;
                        }
                        else if (flag == 2)
                        {
                            // Since the date was unknown to parser, we'll use some logic:
                            // If the user input hour that passed, he probably means the next day. Otherwise he means today.
                            if (parsedDateTime < DateTime.Now)
                            {
                                parsedDateTime = parsedDateTime.AddDays(1);
                            }
                        }
                    }
                }
            }

            return tryParseSuccess;
        }

        /// <summary>
        /// Parses the a repeating format and returns the next date of their occurance, e.g, yearly would return a year from now.
        /// yearly
        /// every year
        /// every Tuesday
        /// every month
        /// every tuesday
        /// after monday
        /// </summary>
        /// <param name="input">The input.</param>
        public static DateTime ParseRepeating(string input)
        {
            const string validUnits = "year|month|week|day";

            input = input.Replace("every ", "");
            input = input.Replace("after a ", "");
            input = input.Replace("after ", "");

            foreach (var unit in validUnits.Split('|'))
            {
                // Changes "yearly" to "year"
                input = input.Replace(unit + "ly", unit);
            }

            // Replaces "year" to "1 year"
            input = "in a " + input;

            return Parse(input);
        }
    }
}
