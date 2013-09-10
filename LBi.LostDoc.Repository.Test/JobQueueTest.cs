using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LBi.LostDoc.Repository.Scheduling;
using LBi.LostDoc.Repository.Web;
using Xunit;

namespace LBi.LostDoc.Repository.Test
{
    public class JobQueueTest
    {
        [Fact]
        public void ExecuteAllJobs()
        {
            IJobQueue jobQueue = new JobQueue();

            List<int> results = new List<int>();

            for (int i = 0; i < 10; i++)
            {
                int local = i;
                jobQueue.Enqueue(new Job("Job" + i.ToString(), c => results.Add(local)));
            }

            while (jobQueue.Any())
                Thread.Sleep(100);

            Assert.Equal(Enumerable.Range(0, 10), results);

            results.Clear();


            for (int i = 0; i < 10; i++)
            {
                int local = i;
                jobQueue.Enqueue(new Job("Job" + i.ToString(), c => results.Add(local)));
            }

            while (jobQueue.Any())
                Thread.Sleep(100);

            Assert.Equal(Enumerable.Range(0, 10), results);
        }

        [Fact]
        public void JobStartedSetCorrectly()
        {
            IJobQueue jobQueue = new JobQueue();
            IJob firstJob = new Job("Job1", c => Thread.Sleep(1000));
            jobQueue.Enqueue(firstJob);
            Thread.Sleep(500);
            Assert.NotNull(firstJob.Started);
        }

        [Fact]
        public void FailingJobDoesntStopProcessing()
        {
            IJobQueue jobQueue = new JobQueue();
            IJob faultedJob = null;
            IJob completedJob = null;
            int counter = 0;

            IJob firstJob = new Job("Failing", c => { throw new Exception("Fail!"); });
            Job secondJob = new Job("Shoud finish", c => counter++);
            
            jobQueue.Faulted += (sender, args) => faultedJob = args.Job;
            jobQueue.Completed += (sender, args) => completedJob = args.Job;

            jobQueue.Enqueue(firstJob);
            jobQueue.Enqueue(secondJob);

            while (jobQueue.Any())
                Thread.Sleep(100);

            Assert.True(object.ReferenceEquals(faultedJob, firstJob));
            Assert.True(object.ReferenceEquals(completedJob, secondJob));
            Assert.Equal(1, counter);
        }
    }
}
