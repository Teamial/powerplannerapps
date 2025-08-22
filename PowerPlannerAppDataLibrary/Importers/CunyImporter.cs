using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json; // Ensure the project already references this; if not, switch to System.Text.Json
using PowerPlannerAppDataLibrary.DataLayer.DataItems; // DataItemClass, DataItemSchedule, etc.

namespace PowerPlannerAppDataLibrary.Importers
{
    // Keep internal so tests can access via InternalsVisibleTo (this repo already includes that):contentReference[oaicite:1]{index=1}.
    internal static class CunyImporter
    {
        // Input shape: what your CUNY exporter outputs.
        internal sealed class CunyClass
        {
            public string Name { get; set; } = "";
            public string ShortName { get; set; } = "";
            public string? Section { get; set; }
            public string? Instructor { get; set; }
            public double? Credits { get; set; }
            public string ColorHex { get; set; } = "#5B8DEF";
            public string TermStart { get; set; } = "";
            public string TermEnd { get; set; } = "";
            public List<Meeting> Meetings { get; set; } = new();
        }

        internal sealed class Meeting
        {
            public List<string> Days { get; set; } = new(); // ["Mon","Wed","Fri"]
            public string Start { get; set; } = "";         // "09:30"
            public string End { get; set; } = "";           // "10:45"
            public string? Location { get; set; }
        }

        public static IEnumerable<DataItemClass> ImportFromCunyJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                yield break;

            var items = JsonConvert.DeserializeObject<List<CunyClass>>(json) ?? new();

            foreach (var c in items)
            {
                var cls = new DataItemClass
                {
                    Name = c.Name,
                    ShortName = c.ShortName,
                    // Power Planner supports credits & GPA weighting; credits are nullable in model (see converters):contentReference[oaicite:2]{index=2}.
                    Credits = c.Credits.HasValue ? (float?)c.Credits.Value : null,
                    // Color model goes through ColorItem; we’ll set hex via helper in a moment.
                    // Start/End dates are per-semester boundaries also used across views/viewmodels in the repo:contentReference[oaicite:3]{index=3}.
                    StartDate = ParseDate(c.TermStart),
                    EndDate = ParseDate(c.TermEnd),
                };

                // Teacher and section are represented via related data items under class (DataItemTeacher / attributes) in the repo:contentReference[oaicite:4]{index=4}.
                if (!string.IsNullOrWhiteSpace(c.Instructor))
                {
                    var teacher = new DataItemTeacherUnderSchedule
                    {
                        Name = c.Instructor
                    };
                    cls.AddChild(teacher);
                }

                // Add class times as schedules under the class (DataItemSchedule exists in the library):contentReference[oaicite:5]{index=5}.
                foreach (var m in c.Meetings ?? Enumerable.Empty<Meeting>())
                {
                    var sched = new DataItemSchedule
                    {
                        Location = m.Location,
                        StartTime = ParseTime(m.Start),
                        EndTime = ParseTime(m.End),
                        DayOfWeeks = DaysToFlags(m.Days) // repo uses day-of-week collections/flags in schedule VM and helpers:contentReference[oaicite:6]{index=6}.
                    };
                    cls.AddChild(sched);
                }

                // Color: the repo has color handling helpers (`ColorItem`, color pickers, etc.):contentReference[oaicite:7]{index=7}.
                if (TryParseHex(c.ColorHex, out var colorItem))
                {
                    cls.Color = colorItem;
                }

                yield return cls;
            }
        }

        private static DateTime ParseDate(string isoDate)
            => DateTime.TryParse(isoDate, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var d) ? d.Date : DateTime.MinValue;

        private static TimeSpan ParseTime(string hhmm)
        {
            if (TimeSpan.TryParseExact(hhmm, @"hh\:mm", CultureInfo.InvariantCulture, out var t))
                return t;
            // Fallback if someone passed "9:30 AM"
            if (DateTime.TryParse(hhmm, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                return dt.TimeOfDay;
            return TimeSpan.Zero;
        }

        private static int DaysToFlags(IEnumerable<string> days)
        {
            // If the model uses flags or collections, adapt here—this is a stub.
            // Map "Mon".."Sun" to a bitmask or list as used by DataItemSchedule in the app.
            int flags = 0;
            foreach (var d in days ?? Enumerable.Empty<string>())
            {
                flags |= d switch
                {
                    "Mon" => 1 << 0,
                    "Tue" => 1 << 1,
                    "Wed" => 1 << 2,
                    "Thu" => 1 << 3,
                    "Fri" => 1 << 4,
                    "Sat" => 1 << 5,
                    "Sun" => 1 << 6,
                    _ => 0
                };
            }
            return flags;
        }

        private static bool TryParseHex(string hex, out ColorItem color)
        {
            color = null!;
            hex = (hex ?? "").Trim();
            if (hex.StartsWith("#")) hex = hex[1..];
            if (hex.Length == 6 && int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
            {
                var r = (byte)((rgb >> 16) & 0xFF);
                var g = (byte)((rgb >> 8) & 0xFF);
                var b = (byte)(rgb & 0xFF);
                color = new ColorItem(r, g, b);
                return true;
            }
            return false;
        }
    }
}
