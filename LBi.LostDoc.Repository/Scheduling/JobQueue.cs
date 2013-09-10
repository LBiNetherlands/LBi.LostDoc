/*
 * Copyright 2013 LBi Netherlands B.V.
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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LBi.LostDoc.Diagnostics;

namespace LBi.LostDoc.Repository.Scheduling
{
    public class JobQueue : IJobQueue
    {
        protected static TraceSource Trace = new TraceSource("LBi.LostDoc.Repository.JobQueue", SourceLevels.All);

        private readonly object _queueLock;
        private readonly SortedList<decimal, IJob> _queue;
        private IJob _current;
        private Task _worker;

        public JobQueue()
        {
            this._queueLock = new object();
            this._queue = new SortedList<decimal, IJob>();
            this._worker = null;
        }


        public void Enqueue(IJob job)
        {
            using (Trace.TraceActivity("Enqueue: {0}", new[] { job.Name }))
            {
                lock (this._queueLock)
                {
                    if (this._worker == null)
                    {
                        Trace.TraceInformation("No worker, create one.");
                        // just create a task that executes the job
                        this._current = job;
                        this._worker = Task.Factory.StartNew(this.ExecuteJob, job);
                    }
                    else
                    {
                        // enqueue task for later
                        decimal index = 1m;
                        if (this._queue.Count > 0)
                        {
                            decimal max = this._queue.Max(kvp => kvp.Key);
                            index += max;
                        }
                        Trace.TraceInformation("Enqueue with key: {0}", index);
                        this._queue.Add(index, job);
                    }
                }
            }
        }

        private void ExecuteJob(object o)
        {
            IJob localJob = (IJob)o;

            using (Trace.TraceActivity("Execute: {0}", new[] { localJob.Name }))
            {

                // set up continuation 
                this._worker.ContinueWith(t =>
                                          {
                                              Trace.TraceInformation("Setting up next job.");
                                              lock (this._queueLock)
                                              {
                                                  this._worker.Dispose();
                                                  this._worker = null;
                                                  this._current = null;

                                                  if (this._queue.Count > 0)
                                                  {
                                                      decimal min = this._queue.Min(kvp => kvp.Key);
                                                      IJob nextJob = this._queue[min];
                                                      this._queue.Remove(min);
                                                      this._current = nextJob;
                                                      this._worker = Task.Factory.StartNew(this.ExecuteJob, nextJob);
                                                  }
                                              }
                                          });

                //TODO add support for cancelling jobs
                try
                {
                    localJob.Execute(CancellationToken.None);
                    this.OnCompleted(localJob);
                }
                catch (Exception jobException)
                {
                    this.OnFaulted(localJob, jobException);
                    Trace.TraceCritical(jobException.ToString());
                }

            }
        }

        public bool Remove(decimal value)
        {
            lock (this._queueLock)
                return this._queue.Remove(value);
        }

        public bool Reorder(decimal oldValue, decimal newValue)
        {
            bool ret = false;
            lock (this._queueLock)
            {
                IJob job;
                if (this._queue.TryGetValue(oldValue, out job))
                {
                    this._queue.Remove(oldValue);
                    this._queue.Add(newValue, job);
                    ret = true;
                }
            }

            return ret;
        }

        public event EventHandler<JobEventArgs> Completed;

        protected virtual void OnCompleted(IJob job)
        {
            EventHandler<JobEventArgs> handler = this.Completed;
            if (handler != null) handler(this, new JobEventArgs(job));
        }

        public event EventHandler<JobFaultedEventArgs> Faulted;

        protected virtual void OnFaulted(IJob job, Exception exception)
        {
            EventHandler<JobFaultedEventArgs> handler = this.Faulted;
            if (handler != null) handler(this, new JobFaultedEventArgs(job, exception));
        }

        public event EventHandler<JobEventArgs> Cancelled;

        protected virtual void OnCancelled(IJob job)
        {
            EventHandler<JobEventArgs> handler = this.Cancelled;
            if (handler != null) handler(this, new JobEventArgs(job));
        }


        public IEnumerator<KeyValuePair<decimal, IJob>> GetEnumerator()
        {
            lock (this._queueLock)
            {
                var current = Enumerable.Empty<KeyValuePair<decimal, IJob>>();
                if (this._current != null)
                    current = new[] { new KeyValuePair<decimal, IJob>(decimal.MinValue, this._current) };

                return current.Concat(this._queue.ToArray().AsEnumerable()).GetEnumerator();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}