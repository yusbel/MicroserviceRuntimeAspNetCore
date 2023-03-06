using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Extensions
{
    public static class ListExtentions
    {
        public static List<Task> AddTaskWithConfigureAwaitFalse(this List<Task> taskList, params Task[] tasks) 
        {
            taskList.AddRange(tasks);
            taskList.ForEach(task => { task.ConfigureAwait(false); });
            return taskList;
        }
    }
}
