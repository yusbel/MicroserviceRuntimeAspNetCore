﻿using AutoFixture;
using AutoFixture.AutoMoq;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Google.Protobuf.Reflection;
using Microsoft.Extensions.Options;
using Sample.Sdk.InMemory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Moq;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using Sample.Sdk.InMemory.Interfaces;
using Sample.Sdk.Azure;
using Sample.Sdk.Security.Providers.Protocol;
using Sample.Sdk.Data.Options;

namespace Sample.Sdk.Tests.Security
{
    [TestClass]
    public class SecurePointToPointTests
    {
        private IFixture _senderFixture;
        private IInMemoryMessageBus<PointToPointSession> _messages = new InMemoryMessageBus<PointToPointSession>();
        private IOptions<CustomProtocolOptions> _customProtocolOptions;
        private IOptions<AzureKeyVaultOptions> _azureKeyVaultOptions;
        private CertificateClient _certificateClient;
        private HttpClient _httpClient;

        [TestInitialize]
        public void Initialize() 
        {
            var senderFixture = new Fixture().Customize(new AutoMoqCustomization());
            var senderSecurePointToPointOptions = senderFixture.Freeze<IOptions<CustomProtocolOptions>>();
            senderSecurePointToPointOptions.Value.WellknownSecurityEndpoint = "http://localhost:5500/Wellknown";
            senderSecurePointToPointOptions.Value.DecryptEndpoint = "http://localhost:5500/Decrypt";
            senderSecurePointToPointOptions.Value.SessionDurationInSeconds = 100;
            _customProtocolOptions = senderSecurePointToPointOptions;

            var senderServiceOptions = senderFixture.Freeze<IOptions<AzureKeyVaultOptions>>();
            senderServiceOptions.Value.VaultUri = "https://learningkeyvaultyusbel.vault.azure.net/";
            senderServiceOptions.Value.DefaultCertificateName = "HttpMessageAsymetricEncryptorCertificate";
            _azureKeyVaultOptions = senderServiceOptions;

            _certificateClient = new CertificateClient(new Uri(senderServiceOptions.Value.VaultUri)
                                                                , new DefaultAzureCredential());
            _httpClient = new HttpClient();
        }

        [TestMethod]
        public async Task GivenValidCertificatesThenCreateSession()
        {
            //var securePointToPoint = new SecurePointToPoint(_messages
            //                                                , _customProtocolOptions
            //                                                , _azureKeyVaultOptions
            //                                                , _certificateClient
            //                                                , _httpClient
            //                                                , new PointToPointChannel()
            //                                                , new ExternalServiceKeyProvider());

            //try
            //{
            //    var result = await securePointToPoint.Decrypt(_customProtocolOptions.Value.WellknownSecurityEndpoint
            //                                                        , _customProtocolOptions.Value.DecryptEndpoint
            //                                                        , await EncryptWithWellknownPublicKey()
            //                                                        , CancellationToken.None);
            //    Assert.IsNotNull(result);
            //}
            //catch (Exception e)
            //{
            //    throw;
            //}   
        }

        public async Task<byte[]> EncryptWithWellknownPublicKey()
        {
            var httpClient = new HttpClient();
            var result = await httpClient.GetAsync(new Uri($"{_customProtocolOptions.Value.WellknownSecurityEndpoint}?action=publickey"));
            var publicKeyWrapper = System.Text.Json.JsonSerializer.Deserialize<PublicKeyWrapper>(await result.Content.ReadAsByteArrayAsync());
            var certificate = new X509Certificate2(Convert.FromBase64String(publicKeyWrapper.PublicKey));
            return certificate.GetRSAPublicKey()
                .Encrypt(Encoding.UTF8.GetBytes("Hello World"), RSAEncryptionPadding.Pkcs1);
        }
    }
}
