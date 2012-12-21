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

using System;
using System.Diagnostics;

namespace LBi.LostDoc.Core.Diagnostics
{
    public class TraceActivity : IDisposable
    {
        public TraceActivity(TraceSource source, string startMessage, string stopMessage)
        {
            this._trace = source;
            this._stopMessage = stopMessage;
            this._oldGuid = Trace.CorrelationManager.ActivityId;
            Guid newGuid = Guid.NewGuid();
            this._trace.TraceTransfer(0, String.Format("Transfer to {0}", startMessage) , newGuid);
            Trace.CorrelationManager.ActivityId = newGuid;
            this._trace.TraceEvent(TraceEventType.Start, 0, startMessage);
        }

        private Guid _oldGuid;
        private string _stopMessage;
        private TraceSource _trace;
        public void Dispose()
        {
            this.Stop();
            GC.SuppressFinalize(this);
        }

        public void Stop(string message = null)
        {
            string  msg = message ?? this._stopMessage;
            this._trace.TraceEvent(TraceEventType.Stop, 0, msg);
            this._trace.TraceTransfer(0, string.Format("Transfer from {0}", msg), this._oldGuid);
            Trace.CorrelationManager.ActivityId = this._oldGuid;
        }

        public void Stop(string message, params object[] args)
        {
            string msg = string.Format(message, args);
            this._trace.TraceEvent(TraceEventType.Stop, 0, msg);
            this._trace.TraceTransfer(0, string.Format("Transfer from {0}", msg), this._oldGuid);
            Trace.CorrelationManager.ActivityId = this._oldGuid;
        }
    }
}