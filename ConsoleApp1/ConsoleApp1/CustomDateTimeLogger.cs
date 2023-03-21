namespace ConsoleApp1
{
    internal class CustomDateTimeLogger : ICustomLogger
    {
        public void Log(string message)
        {
            Console.WriteLine($"{DateTime.Now}-{message}");
        }
    }
}
