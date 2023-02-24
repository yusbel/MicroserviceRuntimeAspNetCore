using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiThreading
{
    internal class Learning
    {
        private readonly ILogger _logger;
        public readonly InMemoryProducerConsumerCollection<NumberIdentifier> InMemoryMsg;
        private readonly InMemoryProducerConsumerCollection<NumberIdentifier> _inputNumbers;
        private readonly InMemoryProducerConsumerCollection<NumberIdentifier> _outputNumbers;
        

        public bool ValidCorrectness() 
        {
            var counter = processCounter;
            if (_inputNumbers.Count != _outputNumbers.Count)
                return false;
            while (_inputNumbers.TryTakeDeDuplicate(out var num, CancellationToken.None)) 
            {
                _outputNumbers.TryTakeDeDuplicate(out var outputNum, CancellationToken.None);
            }
            if (_inputNumbers.Count != _outputNumbers.Count)
                return false;
            return true;
        }
        public Learning(ILogger logger
            , InMemoryProducerConsumerCollection<NumberIdentifier> inMemory
            , InMemoryProducerConsumerCollection<NumberIdentifier> inputNumbers
            , InMemoryProducerConsumerCollection<NumberIdentifier> outputNumbers)
        {
            _logger = logger;
            InMemoryMsg = inMemory;
            _inputNumbers = inputNumbers;
            _outputNumbers = outputNumbers;
        }

        private int processCounter = 0;

        private CancellationTokenSource _writeCancellationTokenSource;
        private CancellationTokenSource _readCancellationTokenSource;
        public void Read(CancellationToken cancellationToken) 
        {
            _readCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            try
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                var token = _readCancellationTokenSource.Token;
                Task.Run(() =>
                {
                    while (!token.IsCancellationRequested || sw.ElapsedMilliseconds == 1000)
                    {
                        Console.WriteLine("Reading ...");
                    }
                },token);
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("Reading Task cancelled exception");
            }
            catch (OperationCanceledException oce) 
            {
                Console.WriteLine("ReadingTask cancelled exception");
            }
            catch(Exception e) 
            {
            }

        }
        public async Task Write(string text, CancellationToken cancellationToken) 
        {
            _writeCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = _writeCancellationTokenSource.Token;
            
            
               await Task.Run(() => 
                {
                    while (!token.IsCancellationRequested) 
                    {
                        Console.WriteLine("Writting ...");
                    }
                    if(token.IsCancellationRequested)
                        token.ThrowIfCancellationRequested();
                }, token);
            
        }

        public void PopulateItemsToProcess() 
        {
            for (var i = 0; i < 2000000; i++)
            {
                InMemoryMsg.TryAdd(new NumberIdentifier() { Id = i.ToString() });
            }
        }
        
        public Task ProcessingTask(CancellationToken token) 
        {
            
            return Task.Run(async() =>
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                //await Task.Delay(30000);
                try
                {
                    while(InMemoryMsg.TryTakeAllWithoutDuplicate(out var msgs, CancellationToken.None)) 
                    {
                        await Parallel.ForEachAsync(msgs, async (source, cancellationToken) =>
                        {
                            Console.WriteLine($"Number computed: {source.Number} with thread: {Thread.CurrentThread.ManagedThreadId}");

                            _outputNumbers.TryAdd(source);
                        });

                        //msgs.AsParallel().Select(item =>
                        //{
                        //    Console.WriteLine($"Number computed: {item.Number} with thread: {Thread.CurrentThread.ManagedThreadId}");
                        //    Interlocked.Increment(ref processCounter);
                        //    return item;
                        //}).ForAll(item => _outputNumbers.TryAdd(item));
                    }

                    //while (InMemoryMsg.TryTakeDeDuplicate(out var itemToProcess, token))
                    //{
                    //    var toProcess = new List<NumberIdentifier>();
                    //    _inputNumbers.TryAdd(itemToProcess);

                    //    var limitSize = 1000;
                    //    var counter = 0;
                    //    while (InMemoryMsg.TryTakeDeDuplicate(out var item, token))
                    //    {
                    //        toProcess.Add(item);
                    //        _inputNumbers.TryAdd(item);
                    //    }
                    //toProcess.AsParallel().Select(item =>
                    //{
                    //    Console.WriteLine($"Number computed: {item.Number} with thread: {Thread.CurrentThread.ManagedThreadId}");
                    //    return item;
                    //}).ForAll(o => 
                    //{
                    //    output.Add(o);
                    //});

                    
                    //capture output and error to produce message accordingly
                    //}

                    //var result = Parallel.For(0, _inMemory.Count, new ParallelOptions() { CancellationToken = token }, (i, state) =>
                    //        {
                    //            //Task.Delay(10).Wait();
                    //            if (_inMemory.TryTakeDeDuplicate(out var item, token))
                    //            {
                    //                Console.WriteLine($"Number computed: {item?.Number} with thread: {Thread.CurrentThread.ManagedThreadId}");
                    //            }
                    //        });

                    //if (!result.IsCompleted && result.LowestBreakIteration.HasValue)
                    //{
                    //    Console.WriteLine($"Operation was cancelled numbers count {_inMemory.Count}");
                    //}
                }
                catch (TaskCanceledException tce)
                {
                    _logger.LogCritical(tce, "Task cancelled excetion");
                }
                catch (OperationCanceledException oce) 
                {
                    _logger.LogCritical(oce, "Operation cancelled exception");
                }
                catch (Exception e)
                {
                    _logger.LogCritical(e, "");
                }

                var timeSpan = sw.Elapsed;
                sw.Stop();
                _logger.LogInformation($"Parallel processing time in minutes {timeSpan.TotalMinutes}");
            }, token);

            
        }
        public static void ParallelForWithStep(int fromInclusive, int toExclusive, int step, Action<int> body)
        {
            if (step < 1)
            {
                throw new ArgumentOutOfRangeException("step");
            }
            else if (step == 1)
            {
                Parallel.For(fromInclusive, toExclusive, body);
            }
            else // step > 1
            {
                int len = (int)Math.Ceiling((toExclusive - fromInclusive) / (double)step);
                Parallel.For(0, len, i => body(fromInclusive + (i * step)));
            }
        }

        public static void MyParallelFor(int inclusiveLowerBound, int exclusiveUpperBound, Action<int> body)
        {
            // Determine the number of iterations to be processed, the number of
            // cores to use, and the approximate number of iterations to process
            // in each thread.
            int size = exclusiveUpperBound - inclusiveLowerBound;
            int numProcs = Environment.ProcessorCount;
            int range = size / numProcs;
            // Use a thread for each partition. Create them all,
            // start them all, wait on them all.
            var threads = new List<Thread>(numProcs);
            for (int p = 0; p < numProcs; p++)
            {
                int start = p * range + inclusiveLowerBound;
                int end = (p == numProcs - 1) ?
                exclusiveUpperBound : start + range;
                threads.Add(new Thread(() => {
                    for (int i = start; i < end; i++) body(i);
                }));
            }
            foreach (var thread in threads) thread.Start();
            foreach (var thread in threads) thread.Join();
        }

        public static void MyParallelForEach<T>(IEnumerable<T> source, Action<T> body)
        {
            int numProcs = Environment.ProcessorCount;
            int remainingWorkItems = numProcs;
            using (var enumerator = source.GetEnumerator())
            {
                using (ManualResetEvent mre = new ManualResetEvent(false))
                {
                    // Create each of the work items.
                    for (int p = 0; p < numProcs; p++)
                    {
                        ThreadPool.QueueUserWorkItem(delegate
                        {
                            // Iterate until there's no more work.
                            while (true)
                            {
                                // Get the next item under a lock,
                                // then process that item.
                                T nextItem;
                                lock (enumerator)
                                {
                                    if (!enumerator.MoveNext()) break;
                                    nextItem = enumerator.Current;
                                }
                                body(nextItem);
                            }
                            if (Interlocked.Decrement(ref remainingWorkItems) == 0)
                                mre.Set();
                        });
                    }
                    // Wait for all threads to complete.
                    mre.WaitOne();
                }
            }
        }


    }
}
