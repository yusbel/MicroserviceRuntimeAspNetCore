using ConsoleApp1.Learning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace ConsoleApp1
{
    public class WorkingWithTransactions
    {
        public static async Task Write() 
        {
            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled)) 
            {
                if (File.Exists($"{Directory.GetCurrentDirectory()}\\Log.txt"))
                    File.Delete($"{Directory.GetCurrentDirectory()}\\Log.txt");

                var fileEnlistment = new FileEnlisment(File.CreateText($"{Directory.GetCurrentDirectory()}\\Log.txt"), "Hello World");
                Transaction.Current.EnlistVolatile(fileEnlistment, EnlistmentOptions.None);

               try
                {
                    WriteMessage();
                }
                catch (Exception ex) 
                {
                    return;
                }
                
                scope.Complete();
            }


        }

        private static void WriteMessage()
        {
            //throw new NotImplementedException();
        }


        private static async Task WriteLog()
        {
            if (File.Exists($"{Directory.GetCurrentDirectory()}\\Log.txt"))
                File.Delete($"{Directory.GetCurrentDirectory()}\\Log.txt");

            var fileEnlistment = new FileEnlisment(File.CreateText($"{Directory.GetCurrentDirectory()}\\Log.txt"), "Hello World");
            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled)) 
            {
                Transaction.Current.EnlistVolatile(fileEnlistment, EnlistmentOptions.None);
            }
            
        }
    }
}
