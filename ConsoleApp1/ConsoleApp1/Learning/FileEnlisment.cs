using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace ConsoleApp1.Learning
{
    public class FileEnlisment : IEnlistmentNotification
    {
        private readonly StreamWriter _sw;
        private readonly String _text;

        public FileEnlisment(StreamWriter sw, String text) => (_sw, _text) = (sw, text);
        public void Commit(Enlistment enlistment)
        {
            _sw.Flush();
            _sw.Close();
            _sw.Dispose();
        }

        public void InDoubt(Enlistment enlistment)
        {
            throw new NotImplementedException();
        }

        public void Prepare(PreparingEnlistment preparingEnlistment)
        {
            _sw.WriteLine(_text);
        }

        public void Rollback(Enlistment enlistment)
        {
            _sw.Close();
            _sw.Dispose();
        }
    }
}
