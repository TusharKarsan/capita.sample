using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace capita.tfl.test06
{
    class Program
    {
        static void Main(string[] args)
        {
            BlockingCollection<int> bCollection = new BlockingCollection<int>(boundedCapacity: 4);

            Task producerThread = Task.Factory.StartNew(() =>
            {
                for (int i = 0; i < 10; ++i)
                {
                    bCollection.Add(i);
                }

                bCollection.CompleteAdding();
            });

            Task consumerThread = Task.Factory.StartNew(() =>
            {
                do
                {
                    int item = bCollection.Take();
                    Console.WriteLine(item);
                    Thread.Sleep(1000);
                }
                while (!bCollection.IsCompleted);
            });

            Task.WaitAll(producerThread, consumerThread);
        }
    }
}
