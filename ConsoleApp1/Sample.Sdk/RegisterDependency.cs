
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sample.Sdk.Msg.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk
{
    public static class SdkRegisterDependencies
    {
        public static IServiceCollection AddSampleSdk(this IServiceCollection services, IConfiguration configuration, string serviceBusInfoSection = "")
        {
            services.Configure<List<ServiceBusInfoOptions>>(options => 
            {
                if (string.IsNullOrEmpty(serviceBusInfoSection)) 
                {
                    return;
                }
                var sectionElements = configuration.AsEnumerable()
                                                    .Where(item=>item.Key.StartsWith(serviceBusInfoSection) && item.Key.Length > serviceBusInfoSection.Length + 1)
                                                    .Select(item=> KeyValuePair.Create(item.Key.Substring(serviceBusInfoSection.Length+1), item.Value))
                                                    .Where(item=>item.Value != null)
                                                    .ToList();
                var sectionGroup = sectionElements
                                                .GroupBy(item => item.Key[0])
                                                .Select(g=> KeyValuePair.Create(g.Key, g.Select(groupElem=>KeyValuePair.Create(groupElem.Key.Substring(groupElem.Key.IndexOf(':')+1),groupElem.Value))))
                                                .Where(itemg=>itemg.Value != null)
                                                .ToList();
                sectionGroup.ForEach(item => 
                {
                    var serviceBusInfoOption = new ServiceBusInfoOptions();
                    item.Value.ToList().ForEach(kv => 
                    {
                        var propName = kv.Key;
                        var propValue = kv.Value;
                        var property = serviceBusInfoOption.GetType()
                                                    .GetProperties()
                                                    .ToList()
                                                    .Where(item => item.Name.ToLower() == propName.ToLower())
                                                    .Select(item => item).FirstOrDefault();
                        property?.SetValue(serviceBusInfoOption, propValue);
                    });
                    if (!string.IsNullOrEmpty(serviceBusInfoOption.Identifier)) 
                    {
                        options.Add(serviceBusInfoOption);
                    }
                });
            });
            return services;
        } 
    }
}
