// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using EveLens.Common.Serialization.Settings;

namespace EveLens.Common.Scheduling
{
    /// <summary>
    /// Represents a recurring schedule entry
    /// </summary>
    public sealed class RecurringScheduleEntry : ScheduleEntry
    {
        public const int SecondsPerDay = 60 * 60 * 24;

        /// <summary>
        /// Default constructor
        /// </summary>
        public RecurringScheduleEntry()
        {
            OverflowResolution = MonthlyOverflowResolution.ClipBack;
            DayOfMonth = 1;
            DayOfWeek = DayOfWeek.Monday;
            WeeksPeriod = 1;
            Frequency = RecurringFrequency.Daily;
            EndDate = DateTime.MaxValue;
        }

        /// <summary>
        /// Deserialization constructor
        /// </summary>
        internal RecurringScheduleEntry(SerializableRecurringScheduleEntry serial)
        {
            StartDate = serial.StartDate;
            EndDate = serial.EndDate;
            Title = serial.Title.Substring(0, TitleMaxLength);
            Options = serial.Options;
            DayOfMonth = serial.DayOfMonth;
            DayOfWeek = serial.DayOfWeek;
            StartTimeInSeconds = serial.StartTimeInSeconds;
            EndTimeInSeconds = serial.EndTimeInSeconds;
            Frequency = serial.Frequency;
            WeeksPeriod = serial.WeeksPeriod;
            OverflowResolution = serial.OverflowResolution;
        }

        /// <summary>
        /// Gets or sets the scheduling frequency (monthly, weekly, etc).
        /// </summary>
        public RecurringFrequency Frequency { get; set; }

        /// <summary>
        /// Gets or sets the weeks period (for weekly frequency). <c>1</c> for every week, <c>2</c> for every two weeks, etc...
        /// </summary>
        public int WeeksPeriod { get; set; }

        /// <summary>
        /// Gets or sets the day of week (for weekly frequency)
        /// </summary>
        public DayOfWeek DayOfWeek { get; set; }

        /// <summary>
        /// Gets or sets the day of month (for monthly frequency)
        /// </summary>
        public int DayOfMonth { get; set; }

        /// <summary>
        /// Gets or sets how overflow are dealt with (for monthly frequency).
        /// </summary>
        public MonthlyOverflowResolution OverflowResolution { get; set; }

        /// <summary>
        /// Gets or sets the start time, in seconds, of an occurence of this entry
        /// </summary>
        public int StartTimeInSeconds { get; set; }

        /// <summary>
        /// Gets or sets the end time, in seconds, of an occurence of this entry
        /// </summary>
        public int EndTimeInSeconds { get; set; }

        /// <summary>
        /// Checks whether the given time is contained within this entry
        /// </summary>
        /// <param name="checkDateTime"></param>
        /// <returns></returns>
        public override bool Contains(DateTime checkDateTime)
        {
            IEnumerable<ScheduleDateTimeRange> ranges = GetRangesInPeriod(checkDateTime, checkDateTime);
            return ranges.Any(sdtr => checkDateTime >= sdtr.From && checkDateTime < sdtr.To);
        }

        /// <summary>
        /// Gets an enumeration of the occurences ranges of this entry within the given interval.
        /// </summary>
        /// <param name="fromDt"></param>
        /// <param name="toDt"></param>
        /// <returns></returns>
        public override IEnumerable<ScheduleDateTimeRange> GetRangesInPeriod(DateTime fromDt, DateTime toDt)
        {
            DateTime startDt = fromDt.Date;
            DateTime endDt = toDt.Date + TimeSpan.FromDays(1);

            if (EndTimeInSeconds > SecondsPerDay)
                startDt -= TimeSpan.FromDays(1);

            DateTime wrkDt = startDt;
            for (; wrkDt < endDt; wrkDt += TimeSpan.FromDays(1))
            {
                ScheduleDateTimeRange range = GetRangeForDay(wrkDt);
                if (range != null)
                    yield return range;
            }
        }

        /// <summary>
        /// Gets the occurence of this entry for the given day.
        /// </summary>
        /// <param name="day"></param>
        /// <returns>The found occurence, or null when this entry does not occur on that day</returns>
        private ScheduleDateTimeRange GetRangeForDay(DateTime day)
        {
            switch (Frequency)
            {
                default:
                    throw new NotImplementedException();

                case RecurringFrequency.Daily:
                    break;

                case RecurringFrequency.Weekdays:
                    if (day.DayOfWeek < DayOfWeek.Monday || day.DayOfWeek > DayOfWeek.Friday)
                        return null;
                    break;

                case RecurringFrequency.Weekends:
                    if (day.DayOfWeek != DayOfWeek.Saturday && day.DayOfWeek != DayOfWeek.Sunday)
                        return null;
                    break;

                case RecurringFrequency.Weekly:
                    DateTime firstInstance = StartDate.AddDays((DayOfWeek - StartDate.DayOfWeek + 7) % 7);
                    if (day.DayOfWeek != DayOfWeek || day.Subtract(firstInstance).Days % (7 * WeeksPeriod) != 0)
                        return null;
                    break;

                case RecurringFrequency.Monthly:
                    if (day.Day != DayOfMonth)
                    {
                        if (day.Day <= 3 && DayOfMonth >= 29)
                        {
                            if (!IsOverflowDate(day))
                                return null;
                        }
                        else if (day.Day >= 28 && DayOfMonth >= 29)
                        {
                            if (!IsOverflowDate(day))
                                return null;
                        }
                        else
                            return null;
                    }
                    break;
            }

            return new ScheduleDateTimeRange(day.Add(TimeSpan.FromSeconds(StartTimeInSeconds)),
                day.Add(TimeSpan.FromSeconds(EndTimeInSeconds)));
        }

        /// <summary>
        /// Checks whether the given day matches the specified day of month according to the overflow options
        /// </summary>
        /// <param name="day"></param>
        /// <returns></returns>
        private bool IsOverflowDate(DateTime day)
        {
            switch (OverflowResolution)
            {
                default:
                    throw new NotImplementedException();

                case MonthlyOverflowResolution.Drop:
                    return false;

                case MonthlyOverflowResolution.OverlapForward:
                    DateTime lastDayOfPreviousMonthDt = day - TimeSpan.FromDays(day.Day);
                    int lastDayOfPreviousMonth = lastDayOfPreviousMonthDt.Day;
                    int dayOfThisMonth = day.Day;
                    return DayOfMonth - lastDayOfPreviousMonth == dayOfThisMonth;

                case MonthlyOverflowResolution.ClipBack:
                    DateTime searchForward = day + TimeSpan.FromDays(1);
                    if (day.Month == searchForward.Month)
                        return false;
                    return DayOfMonth > day.Day;
            }
        }

        /// <summary>
        /// Checks whether the given time intersects with this entry.
        /// </summary>
        /// <param name="timeToTest"></param>
        /// <returns></returns>
        protected override bool Clash(DateTime timeToTest)
        {
            DateTime testTime = (Options & ScheduleEntryOptions.EVETime) != 0 ? timeToTest.ToUniversalTime() : timeToTest;

            ScheduleDateTimeRange range = GetRangeForDay(testTime.Date);
            if (range == null)
                return false;

            DateTime startDate = StartDate.Add(range.From.TimeOfDay);

            // in the event m_endDate is set to Forever (DateTime.MaxValue) we can't add anything to it
            DateTime endDate = EndDate == DateTime.MaxValue ? EndDate : EndDate.Add(range.From.TimeOfDay);

            if (startDate < testTime && testTime < endDate)
                return range.From < testTime && testTime < range.To;

            return false;
        }

        /// <summary>
        /// Checks whether this entry occurs on the given day.
        /// </summary>
        /// <param name="timeToTest"></param>
        /// <returns></returns>
        public override bool IsToday(DateTime timeToTest)
        {
            if (timeToTest >= StartDate && timeToTest <= EndDate)
                return GetRangeForDay(timeToTest.Date) != null;

            return false;
        }

        /// <summary>
        /// Exports the data to a serialization object.
        /// </summary>
        /// <returns></returns>
        internal override SerializableScheduleEntry Export()
            => new SerializableRecurringScheduleEntry
            {
                StartDate = StartDate,
                EndDate = EndDate,
                Title = Title,
                Options = Options,
                DayOfMonth = DayOfMonth,
                DayOfWeek = DayOfWeek,
                StartTimeInSeconds = StartTimeInSeconds,
                EndTimeInSeconds = EndTimeInSeconds,
                Frequency = Frequency,
                WeeksPeriod = WeeksPeriod,
                OverflowResolution = OverflowResolution
            };
    }
}