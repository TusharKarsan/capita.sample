using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace capita.tfl.test03
{
    class Program
    {
        static void Main(string[] args)
        {
            const int threads = 2;

            var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };

            var producer = new TransformBlock<string, string>(uri =>
            {
                //Console.WriteLine("Downloading '{0}'...", uri);
                Thread.Sleep(1000);
                return "This is a test " + Thread.CurrentThread.ManagedThreadId.ToString();
            }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = threads });

            var writeLog = new TransformBlock<string, string>(file =>
            {
                //Console.WriteLine("Log writer...");
                Thread.Sleep(1000);
                return file;
            }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = threads });

            var consumer = new ActionBlock<string>(file =>
            {
                Thread.Sleep(1000);
                Console.WriteLine("Done {0}", file);
            }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = threads });

            producer.LinkTo(writeLog, linkOptions);
            writeLog.LinkTo(consumer, linkOptions);

            for (int i = 0; i < 50; i++)
            {
                producer.Post("Message " + i.ToString());
            }

            producer.Complete();
            consumer.Completion.Wait();
        }

    }
}
