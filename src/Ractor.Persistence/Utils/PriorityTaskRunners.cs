using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Schedulers;

namespace Ractor.Utils {


    public class PriorityTaskRunner
    {
        private static readonly QueuedTaskScheduler TopScheduler = new QueuedTaskScheduler();
        private TaskScheduler _scheduler;

        public PriorityTaskRunner()
        {
            
        }


        //private static readonly TaskFactory myTaskFactory = new TaskFactory(
        //    CancellationToken.None, TaskCreationOptions.DenyChildAttach,
        //    TaskContinuationOptions.None, TopScheduler);


        public void Foo() {
            QueuedTaskScheduler qts = new QueuedTaskScheduler();
            TaskScheduler pri0 = qts.ActivateNewQueue(priority: 0);
            //TaskScheduler pri1 = qts.CreateQueue(priority: 1);

            var q1 = qts.ActivateNewQueue();
            //var tf = new TaskFactory(,)

            var t = Task.Factory.StartNew(async () => {
                await Task.Delay(1000);
                return 42;
            });
        }
    }
}
