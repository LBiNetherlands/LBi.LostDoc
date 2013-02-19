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
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LBi.LostDoc;
using LBi.LostDoc.Diagnostics;
using LBi.LostDoc.Templating;

namespace LBi.LostDoc.Repository
{
    /// <summary>
    /// This acts as the main content manager for the repository.
    /// </summary>
    // TODO enable different (git?) storage backend
    public class ContentManager
    {
        private const string CONTENT_REF_NAME = ".latest";
        // private const string CONTENT_HISTORY_NAME = ".history";
        private readonly string _contentPath;
        private readonly string _repositoryPath;
        private readonly ConcurrentQueue<Trigger> _buildQueue;
        private readonly BlockingCollection<Trigger> _workQueue;
        private readonly Task _workProcess;
        private readonly Template _template;
        private readonly VersionComponent? _ignoreVersionComponent;
        private readonly ReaderWriterLockSlim _contentLock;
        private static ContentManager _instance;
        private string _currentContentRef;
        private volatile State _state;

        public string ContentFolder
        {
            get
            {
                try
                {
                    this._contentLock.EnterReadLock();
                    if (this._currentContentRef == null)
                    {
                        TraceSources.ContentManagerSource.TraceError("No content available");
                        throw new InvalidOperationException("No content available.");
                    }

                    return this._currentContentRef;
                }
                finally
                {
                    this._contentLock.ExitReadLock();
                }
            }
        }

        public string ContentRoot
        {
            get
            {
                try
                {
                    this._contentLock.EnterReadLock();
                    if (this._currentContentRef == null)
                    {
                        TraceSources.ContentManagerSource.TraceError("No content available");
                        throw new InvalidOperationException("No content available.");
                    }

                    return Path.Combine(this._contentPath, this._currentContentRef);
                }
                finally
                {
                    this._contentLock.ExitReadLock();
                }
            }
        }

        public State CurrentState
        {
            get { return this._state; }
        }


        protected class Trigger
        {
            public string Reason { get; set; }

            public DateTime Created { get; set; }
        }

        public ContentManager(ContentSettings settings)
        {
            this._state = State.Idle;
            this._repositoryPath = settings.RepositoryPath;
            this._contentPath = settings.ContentPath;
            this._ignoreVersionComponent = settings.IgnoreVersionComponent;
            this._buildQueue = new ConcurrentQueue<Trigger>();
            this._workQueue = new BlockingCollection<Trigger>(this._buildQueue);
            this._template = settings.Template;
            this._contentLock = new ReaderWriterLockSlim();
            this._workProcess = new TaskFactory().StartNew(this.WorkerMethod);
            string contentRefPath = Path.Combine(this._contentPath, CONTENT_REF_NAME);
            if (File.Exists(contentRefPath))
                this._currentContentRef = File.ReadAllText(contentRefPath);
            else
                this._currentContentRef = null;
        }

        private void WorkerMethod()
        {
            Thread.CurrentThread.Name = "ContentManager.WorkerMethod";
            using (TraceSources.ContentManagerSource.TraceActivity("Worker starting"))
            {

                foreach (Trigger trigger in this._workQueue.GetConsumingEnumerable())
                {
                    Trigger discardedTrigger;

                    while (this._workQueue.TryTake(out discardedTrigger))
                        TraceSources.ContentManagerSource.TraceInformation("Skipping redundant rebuild trigger: {0}",
                                                                           discardedTrigger.Reason);

                    TraceSources.ContentManagerSource.TraceInformation("Rebuilding content: {0}", trigger.Reason);

                    try
                    {
                        ContentBuilder builder = new ContentBuilder();
                        builder.StateChanged += (o, s) => this._state = s;
                        builder.Template = this._template;
                        builder.IgnoreVersionComponent = this._ignoreVersionComponent;
                        string folder = Guid.NewGuid().ToBase36String();
                        string tmpDir = Path.Combine(this._contentPath, folder);
                        builder.Build(this._repositoryPath, tmpDir);

                        // this acts as the "commit"
                        try
                        {
                            this.SetCurrentContentFolder(folder);
                        }
                        catch (Exception ex)
                        {
                            TraceSources.ContentManagerSource.TraceCritical(
                                "An unhandled exception of type {0} occured while setting current content folder: {1}",
                                ex.GetType().Name,
                                ex.ToString());
                            throw;

                        }
                    }
                    catch (Exception ex)
                    {
                        TraceSources.ContentManagerSource.TraceError(
                                                                     "An unhandled exception of type {0} occured while rebuilding content: {1}",
                                                                     ex.GetType().Name,
                                                                     ex.ToString());
                    }
                }
            }
        }

        public void SetCurrentContentFolder(string contentPath)
        {
            try
            {
                this._contentLock.EnterWriteLock();
                string currentPath = Path.Combine(this._contentPath, CONTENT_REF_NAME);
                File.WriteAllText(currentPath, contentPath);
                this._currentContentRef = contentPath;
            }
            finally
            {
                this._contentLock.ExitWriteLock();
            }
        }

        public void QueueRebuild(string reason)
        {
            this._workQueue.Add(new Trigger { Created = DateTime.UtcNow, Reason = reason });
        }


        public string GetContentRoot(string id)
        {
            string ret = Path.Combine(this._contentPath, id);
            if (!Directory.Exists(ret))
                throw new DirectoryNotFoundException();

            return ret;
        }
    }
}
