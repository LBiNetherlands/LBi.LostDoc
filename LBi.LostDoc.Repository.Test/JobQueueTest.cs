using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

        }
    }
}
