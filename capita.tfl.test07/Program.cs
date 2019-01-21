using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace capita.tfl.test07
{
    class Program
    {
        private static Proc proc = new Proc();
        static void Main(string[] args)
        {
            int counter = 0;
            proc.Start();

            while (counter++ < 10)
            {
                Debug.WriteLine("{0} Runnig", DateTime.Now.ToLongTimeString());
                Thread.Sleep(1000);
            }

            proc.Stop();
            proc = null;

            Debug.WriteLine("Done");
            Thread.Sleep(1000);
        }
    }

    class Proc
    {
        DataflowLinkOptions linkOptions = new DataflowLinkOptions { PropagateCompletion = true };

        CancellationTokenSource cancellationOuter = null;
        CancellationTokenSource cancellationToken = null;

        BlockingCollection<int> bCollectionA = null;
        BlockingCollection<int> bCollectionB = null;

        Task outer, producerThread, consumerThread;
        TaskFactory factory = new TaskFactory(TaskCreationOptions.LongRunning, TaskContinuationOptions.None);

        public void Start()
        {
            bCollectionA = new BlockingCollection<int>(boundedCapacity: 4);
            cancellationOuter = new CancellationTokenSource();

            outer = factory.StartNew(() =>
            {
                var rnd = new Random();
                while (!cancellationOuter.IsCancellationRequested)
                {
                    Level2Start();

                    for (int i = 0; i < 10; i++)
                    {
                        try
                        {
                            bCollectionA.Add(rnd.Next(), cancellationOuter.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            Debug.WriteLine("OperationCanceledException outer");
                        }
                    }

                    Level2Stop();
                }
            }, cancellationOuter.Token);
        }

        public void Stop()
        {
            if (cancellationOuter == null)
            {
                Console.WriteLine("Stop skip");
                return;
            }
            Console.WriteLine("Stop do");

            cancellationOuter.Cancel();
            outer.Wait();

            outer.Dispose();
            outer = null;

            cancellationOuter.Dispose();
            cancellationOuter = null;

            bCollectionA.Dispose();
            bCollectionA = null;
        }

        public void Level2Start()
        {
            cancellationToken = new CancellationTokenSource();
            bCollectionB = new BlockingCollection<int>(boundedCapacity: 4);

            producerThread = factory.StartNew(() =>
            {
                int val = 0;
                while (!bCollectionA.IsCompleted && !cancellationToken.IsCancellationRequested)
                {
                    if(bCollectionA.TryTake(out val, 100))
                    {
                        Console.WriteLine($"Producer {val}");
                        try
                        {
                            bCollectionB.Add(val, cancellationToken.Token);
                        }
                        catch(OperationCanceledException)
                        {
                            Debug.WriteLine("OperationCanceledException level 2");
                        }
                    }
                    else
                        Thread.Sleep(100);
                }

                bCollectionB.CompleteAdding();
            });

            consumerThread = factory.StartNew(() =>
            {
                int val = 0;
                while (!bCollectionB.IsCompleted && !cancellationToken.IsCancellationRequested)
                {
                    if (bCollectionB.TryTake(out val, 100))
                    {
                        Console.WriteLine($"Consumer {val}");
                    }
                    Thread.Sleep(100);
                }
            });
        }

        public void Level2Stop()
        {
            if (cancellationToken == null)
            {
                Console.WriteLine("Level2Stop skip");
                return;
            }
            Console.WriteLine("Level2Stop do");

            cancellationToken.Cancel();
            Task.WaitAll(producerThread, consumerThread);

            producerThread.Dispose();
            consumerThread.Dispose();

            producerThread = null;
            consumerThread = null;

            cancellationToken.Dispose();
            cancellationToken = null;

            bCollectionB.Dispose();
            bCollectionB = null;
        }
    }
}
