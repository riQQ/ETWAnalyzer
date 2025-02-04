﻿//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using Microsoft.Windows.EventTracing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ETWAnalyzer.TraceProcessorHelpers
{
    /// <summary>
    /// Helper class to calculate from multiple threads the overlapping time durations. 
    /// This is not the sum as WPA is doing it but count overlapping durations only once.
    /// We do this to get a meaningful value when many threads are e.g. waiting. The wait sum for many threads
    /// becomes meaningless. Instead we do if e.g. all threads start at time x, some stop at y, and one at z 
    /// we count [x-z] as the total duration which resembles to measured wall clock times much better.
    /// The times can therefore never be greater than the wall clock time the threads were active (or waiting).
    /// </summary>
    internal class TimeRangeCalculator
    {
        List<KeyValuePair<Timestamp, Duration>> myTimeRanges = new List<KeyValuePair<Timestamp, Duration>>();

        /// <summary>
        /// Add a timepoint with a duration which will be used to calculate the total duration
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="duration"></param>
        public void Add(Timestamp startTime, Duration duration)
        {
            myTimeRanges.Add(new KeyValuePair<Timestamp, Duration>(startTime, duration));
        }

        /// <summary>
        /// The duration is the sume of all time ranges where overlaps in the time ranges count only once.
        /// </summary>
        /// <returns>Total duration</returns>
        public TimeSpan GetDuration()
        {
            List<KeyValuePair<Timestamp, Duration>> sorted = myTimeRanges.OrderBy(x => x.Key).ToList();
            Timestamp totalDuration = Timestamp.Zero;

            Timestamp previousEndTime = Timestamp.Zero;

            for(int i = 0; i < sorted.Count; i++)
            {
                var current = sorted[i];
                if(previousEndTime >= current.Key )
                {
                    if (previousEndTime.Nanoseconds > current.Key.Nanoseconds + current.Value.Nanoseconds)
                    {
                        // ignore this one
                    }
                    else
                    {
                        long durationns= current.Value.Nanoseconds - (previousEndTime.Nanoseconds - current.Key.Nanoseconds);
                        totalDuration += new Duration(durationns);
                        Timestamp newEndtime = current.Key + current.Value;
                        previousEndTime = newEndtime;
                    }
                }
                else
                {
                    totalDuration += current.Value;
                    previousEndTime = Timestamp.FromNanoseconds( current.Key.Nanoseconds  + current.Value.Nanoseconds);
                }
            }

            return TimeSpan.FromTicks(totalDuration.Nanoseconds/100);
        }
    }
}
