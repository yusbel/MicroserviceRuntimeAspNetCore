﻿using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Interface.Msg
{
    public interface IComputeExternalMessage
    {
        Task<bool> ProcessExternalMessage(List<KeyValuePair<string, string>> externalMessage,
                                        CancellationToken cancellationToken);
    }
}
