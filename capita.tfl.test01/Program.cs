using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace capita.tfl.test01
{
    class Program
    {
        static void Produce(ITargetBlock<byte[]> target)
        {
            Random rand = new Random();

            for (int i = 0; i < 100; i++)
            {
                byte[] buffer = new byte[1024];
                rand.NextBytes(buffer);
                target.Post(buffer);
                Console.WriteLine("Prod {0}", i);
            }

            target.Complete();
        }

        static async Task<int> ConsumeAsync(ISourceBlock<byte[]> source)
        {
            int bytesProcessed = 0;

            while (await source.OutputAvailableAsync())
            {
                byte[] data = source.Receive();
                bytesProcessed += data.Length;
                Thread.Sleep(100);
                Console.WriteLine("Cons {0}", data.Length);
            }

            return bytesProcessed;
        }

        static void Main(string[] args)
        {
            var buffer = new BufferBlock<byte[]>();
            var consumer = ConsumeAsync(buffer);
            Produce(buffer);
            consumer.Wait();
            Console.WriteLine("Processed {0} bytes.", consumer.Result);
            Console.ReadKey();
        }
    }
}
