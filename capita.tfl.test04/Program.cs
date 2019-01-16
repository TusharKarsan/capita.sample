using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace capita.tfl.test04
{

    public class Producer
    {
        private static Random random = new Random((int)DateTime.UtcNow.Ticks);

        public double Produce()
        {
            var value = random.NextDouble();
            Console.WriteLine($"Producing value: {value}");
            return value;
        }
    }

    public class Consumer
    {
        public void Consume(double value) => Console.WriteLine($"Consuming value: {value}");
    }

    class Program
    {
        private static BufferBlock<double> buffer = new BufferBlock<double>();

        static void Main(string[] args)
        {
            var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;

            var producerTask1 = Task.Run(async () =>
            {
                var producer = new Producer();
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(500);
                    buffer.Post(producer.Produce());

                    while (buffer.Count > 5)
                    {
                        await Task.Delay(1000);
                        Console.WriteLine("On hold");
                    }
                }

                buffer.Complete();
            });

            var producerTask2 = Task.Run(async () =>
            {
                var producer = new Producer();
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(500);
                    buffer.Post(producer.Produce());

                    while (buffer.Count > 5)
                    {
                        await Task.Delay(1000);
                        Console.WriteLine("On hold");
                    }
                }

                buffer.Complete();
            });

            var consumerTask1 = Task.Run(async () =>
            {
                var consumer = new Consumer();
                while (!buffer.Completion.IsCompleted)
                {
                    await Task.Delay(1500);
                    consumer.Consume(buffer.Receive(/*new TimeSpan(0, 0, 3)*/));
                }
            });

            var consumerTask2 = Task.Run(async () =>
            {
                var consumer = new Consumer();
                while (!buffer.Completion.IsCompleted)
                {
                    await Task.Delay(1500);
                    consumer.Consume(buffer.Receive(/*new TimeSpan(0, 0, 3)*/));
                }
            });

            buffer.Completion.Wait();

            /*
             * Batch mode. 
             */

            var batchBlock = new BatchBlock<int>(10);
            for (int i = 0; i < 11; i++)
            {
                batchBlock.Post(i);
            }
            batchBlock.Complete();

            if (batchBlock.OutputCount > 0)
                Console.WriteLine("The sum of the elements in batch 1 is {0}.", batchBlock.Receive().Sum());

            if(batchBlock.OutputCount > 0)
                Console.WriteLine("The sum of the elements in batch 2 is {0}.", batchBlock.Receive().Sum());
        }
    }
}
