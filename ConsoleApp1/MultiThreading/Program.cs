// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MultiThreading;
using System;
using System.Collections.Concurrent;

var loggerFactory = LoggerFactory.Create(builder => 
{
    builder.AddFilter("MultiThreading", LogLevel.Information);
    builder.ClearProviders();
    builder.AddConsole();
});

var logger = loggerFactory.CreateLogger<InMemoryProducerConsumerCollection<NumberIdentifier>>();
InMemoryProducerConsumerCollection<NumberIdentifier> numbers =
    new InMemoryProducerConsumerCollection<NumberIdentifier>(
    new MemoryCacheState<string, string>(new MemoryCache(new MemoryCacheOptions())), logger);

InMemoryProducerConsumerCollection<NumberIdentifier> inputNumbers =
    new InMemoryProducerConsumerCollection<NumberIdentifier>(
    new MemoryCacheState<string, string>(new MemoryCache(new MemoryCacheOptions())), logger);

InMemoryProducerConsumerCollection<NumberIdentifier> outputNumbers =
    new InMemoryProducerConsumerCollection<NumberIdentifier>(
    new MemoryCacheState<string, string>(new MemoryCache(new MemoryCacheOptions())), logger);

var learning = new Learning(logger, numbers, inputNumbers, outputNumbers);
var cancelationTokenSource = new CancellationTokenSource();
var cancellationToken = cancelationTokenSource.Token;

//try
//{
//    learning.Read(cancellationToken);
//}
//catch (TaskCanceledException tce) 
//{
//    Console.WriteLine("Program catch a task cancelled exceptio");
//}
//catch (Exception e)
//{
//    Console.WriteLine($"{e.Message}");
//}

Task.Run(async () =>
{
    await Task.Delay(500);
    cancelationTokenSource.Cancel();
});

try
{
    await learning.Write("test", cancellationToken);
}
catch (TaskCanceledException tce)
{
    Console.WriteLine("Program catch a task cancelled exception when writting");
}
catch (Exception e) 
{
    if (e is OperationCanceledException) 
    {
        Console.WriteLine("Program catch a task cancelled exception when writting");
    }
        if (cancellationToken.IsCancellationRequested) 
    {
        Console.WriteLine("Program cancellation token wwas cancelled");
    }
    if (e is AggregateException aggException)
    {
        foreach (Exception exception in aggException.Flatten().InnerExceptions)
        {
            if (exception is OperationCanceledException) 
            {
                
            }
        }
    }
    //Console.WriteLine("Program catch exception");
}


//learning.PopulateItemsToProcess(); 

//Console.WriteLine("Waiting for task to finish");
////await Task.WhenAll(tasks);
//await learning.ProcessingTask(cancellationToken);
//Console.WriteLine("Press any key to process again");
//Console.ReadKey();
////should find all item in cache
//Console.WriteLine("Adding items");
//learning.PopulateItemsToProcess();
//Console.WriteLine("Populating items finished, processing items start");
//await learning.ProcessingTask(cancellationToken);

//Console.WriteLine("All tasks completed");

//Console.WriteLine($"Was all processed correctly {learning.ValidCorrectness()}");

Console.ReadKey();

//Learning.ParallelForWithStep(0, 10, 2, (i) => Console.WriteLine($"Iteration {i}"));


//Parallel.For(0, 10, (i, state) => 
//{
//    var rnd = new Random();
//    var num = rnd.Next(0, 10);
//    if (num / 2 == 0) 
//    {
//        Console.WriteLine($"Num: {num}");

//    }
//    Console.WriteLine($"Thread ID: {Thread.CurrentThread.ManagedThreadId}");
//});

//Console.WriteLine($"Num of processors {Environment.ProcessorCount}");

//Learning.MyParallelFor(0, 10, (i)=> Console.WriteLine($"{i}"));

//Learning.MyParallelForEach(Enumerable.Range(0, 10), (i) => Console.WriteLine("{i}"));

//Console.WriteLine("Hello, World!");
