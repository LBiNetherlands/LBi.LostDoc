/*
 * Copyright 2013 DigitasLBi Netherlands B.V.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License. 
 */

using System;

namespace LBi.LostDoc.Repository.Web.Areas.Administration
{

    // TODO this needs to be pushed into the front-end
    public static class Extensions
    {
        public static string ToHumanReadableString(this TimeSpan timeSpan)
        {
            if (timeSpan.TotalDays > 7)
                return string.Format("{0:N0} weeks", timeSpan.TotalDays / 7);

            if (timeSpan.TotalDays >= 1)
                return string.Format("{0:N0} days", timeSpan.TotalDays);

            if (timeSpan.TotalHours >= 2)
                return string.Format("{0:N0} hours", timeSpan.TotalHours);

            if (timeSpan.TotalHours >= 1)
                return string.Format("{0:N0} hour", timeSpan.TotalHours);

            if (timeSpan.TotalMinutes >= 2)
                return string.Format("{0:N0} minutes", timeSpan.TotalMinutes);

            if (timeSpan.TotalMinutes >= 1)
                return string.Format("{0:N0} minute", timeSpan.TotalMinutes);

            if (timeSpan.TotalSeconds >= 5)
                return string.Format("less than one minute");

            return string.Format("{0:N0} seconds", timeSpan.TotalSeconds);
        }
    }
}