// See https://aka.ms/new-console-template for more information
using Grpc.Net.Client;

var channel = GrpcChannel.ForAddress("https://samplegrpcyusbel.azurewebsites.net");

var client = new PayRollRetrival.PayRollRetrivalClient(channel);

var response = await client.GetPayRollAsync(new PayRollRequest() { PayRollIdentifier = Guid.NewGuid().ToString() });

//Console.WriteLine(response.Message);

Console.WriteLine($"Hello, World! identifier {response.PayRollIdentifier}");
