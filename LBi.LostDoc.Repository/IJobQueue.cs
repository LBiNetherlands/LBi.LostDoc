using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LBi.LostDoc.Diagnostics;

namespace LBi.LostDoc.Repository
{
    public interface IJobQueue : IEnumerable<KeyValuePair<decimal, IJob>>
    {
        void Enqueue(IJob job);
        bool Remove(decimal value);
        bool Reorder(decimal oldValue, decimal newValue);
    }

    public interface IJob
    {
        string Name { get; }

        DateTimeOffset Created { get; }

        DateTimeOffset? Started { get; }

        void Execute(CancellationToken cancellationToken);
    }

    public class Job : IJob
    {
        private readonly Action<CancellationToken> _action;

        public Job(string name, Action<CancellationToken> action)
        {
            this.Name = name;
            this._action = action;
        }

        public string Name { get; private set; }

        public DateTimeOffset Created { get; private set; }

        public DateTimeOffset? Started { get; private set; }

        public void Execute(CancellationToken cancellationToken)
        {
            this._action(cancellationToken);
        }
    }

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
                                                      decimal min = this._queue.Max(kvp => kvp.Key);
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
                }
                catch (Exception jobException)
                {
                    // TODO report this
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
