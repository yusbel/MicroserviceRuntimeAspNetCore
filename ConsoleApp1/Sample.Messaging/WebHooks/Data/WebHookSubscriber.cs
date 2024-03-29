﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Messaging.WebHooks.Data
{
    public class WebHookSubscriber : WebHookSubscriberEntity
    {
        public string SubscriberKey { get; set; }
        public string MessageKey { get; init; }
        public string WebHookUrl { get; init; }
    }
}
