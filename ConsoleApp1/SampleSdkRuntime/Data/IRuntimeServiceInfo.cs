using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleSdkRuntime.Data
{
    public interface IRuntimeServiceInfo
    {
        public enum FaultyType { InfoDataTypeMissMatch, TimeOutReached }
        public bool IsValid { get; set; }
        public bool IsFaulty { get; set; }
    }
}
