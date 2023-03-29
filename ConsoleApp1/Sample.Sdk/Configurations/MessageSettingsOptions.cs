using Microsoft.Extensions.Configuration;
using Sample.Sdk.Msg.Data.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Configurations
{
    /// <summary>
    /// Load message settings configuration one time per service lifetime.
    /// </summary>
    internal class MessageSettingsOptions
    {
        private readonly ReaderWriterLockSlim _readerWriterLock = new ReaderWriterLockSlim();

        private List<AzureMessageSettingsOptions> _options = null;

        internal static MessageSettingsOptions Create()
        {
            return new MessageSettingsOptions();
        }

        internal List<AzureMessageSettingsOptions> GetConfigOptions(IConfiguration configuration) 
        {
            Func<List<AzureMessageSettingsOptions>> loadOptions = () => 
            {
                LoadOptions(configuration);
                return GetOptions();
            };
            var options = GetOptions();
            return options != null ? options : loadOptions.Invoke();
        }

        private void LoadOptions(IConfiguration configuration) 
        {
            try
            {
                _readerWriterLock.EnterWriteLock();
                _options = new List<AzureMessageSettingsOptions>();
                var senderOptions = new List<AzureMessageSettingsOptions>();
                var receiverOptions = new List<AzureMessageSettingsOptions>();
                configuration.GetSection(AzureMessageSettingsOptions.RECEIVER_SECTION_ID).Bind(receiverOptions);
                configuration.GetSection(AzureMessageSettingsOptions.SENDER_SECTION_ID).Bind(senderOptions);
                _options.AddRange(receiverOptions);
                _options.AddRange(senderOptions);
            }
            finally 
            {
                _readerWriterLock.ExitWriteLock();
            }
        }
        private List<AzureMessageSettingsOptions> GetOptions()
        {
            try
            {
                _readerWriterLock.EnterReadLock();
                if (_options != null)
                    return _options;
                return null;
            }
            finally 
            {
                _readerWriterLock.ExitReadLock();
            }
        }
        
    }
}
