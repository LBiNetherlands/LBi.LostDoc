/*
 * Copyright 2012 LBi Netherlands B.V.
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


using System.Diagnostics;

namespace LBi.LostDoc.Core.Diagnostics
{
    public static class TraceExtensions
    {
        public static void TraceWarning(this TraceSource source, string message, params object[] args)
        {
            source.TraceEvent(TraceEventType.Warning, 0, message, args);
        }
        public static void TraceWarning(this TraceSource source, string message)
        {
            source.TraceEvent(TraceEventType.Warning, 0, message);
        }

        public static void TraceError(this TraceSource source, string message, params object[] args)
        {
            source.TraceEvent(TraceEventType.Error, 0, message, args);
        }
        public static void TraceError(this TraceSource source, string message)
        {
            source.TraceEvent(TraceEventType.Error, 0, message);
        }

        public static void TraceCritical(this TraceSource source, string message, params object[] args)
        {
            source.TraceEvent(TraceEventType.Critical, 0, message, args);
        }
        public static void TraceCritical(this TraceSource source, string message)
        {
            source.TraceEvent(TraceEventType.Critical, 0, message);
        }

        public static void TraceVerbose(this TraceSource source, string message, params object[] args)
        {
            source.TraceEvent(TraceEventType.Verbose, 0, message, args);
        }
        public static void TraceVerbose(this TraceSource source, string message)
        {
            source.TraceEvent(TraceEventType.Verbose, 0, message);
        }

        public static TraceActivity TraceActivity(this TraceSource source, string activity, params object[] args)
        {
            string msg = string.Format(activity, args);
            return new TraceActivity(source, msg, msg);
        }

        public static TraceActivity TraceActivity(this TraceSource source, string activity)
        {
            return new TraceActivity(source, activity, activity);
        }

        public static TraceActivity TraceActivity(this TraceSource source, string startMessage, string stopMessage)
        {
            return new TraceActivity(source, startMessage, stopMessage);
        }

    }
}
