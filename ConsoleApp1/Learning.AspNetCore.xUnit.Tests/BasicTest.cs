using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;

namespace Learning.AspNetCore.xUnit.Tests
{
    public class BasicTest : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _webAppFactory;

        public BasicTest(WebApplicationFactory<Program> webAppFactory)
        {
            _webAppFactory = webAppFactory;
        }

        [Theory]
        [InlineData("/")]
        [InlineData("/Home/Privacy")]
        public async Task Get_EndpointReturnSuccessAndCorrectContentType(string url)
        {
            //Arrange
            var client = _webAppFactory.CreateClient();

            //Act
            var response = await client.GetAsync(url);

            //Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal("text/html; charset=utf-8",
            response.Content.Headers.ContentType.ToString());
        }

        [Theory]
        [InlineData("/api/values")]
        public async Task Get_ApiValues(string url) 
        {
            //Arrange
            var webClient = _webAppFactory.CreateClient();

            //Act
            var response = await webClient.GetAsync(url);
            IEnumerable<string>? values = System.Text.Json.JsonSerializer.Deserialize<IEnumerable<string>>(await response.Content.ReadAsStringAsync());

            Assert.Equal(2, values.Count());
            Assert.True(values.First() == "value1");
        }
    }
}