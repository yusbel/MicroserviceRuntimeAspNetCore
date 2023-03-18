using JsonFlatten;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sample.Sdk.Core.Extensions;
using Sample.Sdk.Core.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Sample.Sdk.Tests.Security
{
    [TestClass]
    public class ExternalMessageConverterTests
    {
        public class TestObj 
        {
            public bool IsValid { get; set; }
            public string Name { get; set; }
            public int Num { get; set; }
            public NestedObj NestedObj { get; set; }
            public NestedComplexObject NestedComplexObject { get; set; } = new NestedComplexObject();
        }

        public class NestedObj 
        {
            public List<string> Names { get; set; }
        }

        public class NestedComplexObject 
        {
            public string TaskName { get; set; }
            public List<NestedObj> NestedObjects { get;set; }
        }

        [TestMethod]
        public void GiveAnExternalMessageThenReturnDictionaryOfStrings() 
        {
            var externalMsgConverter = new ExternalMessageConverter();
            var obj = new TestObj() { IsValid = true, Name = "yusbel", Num = 9, NestedObj = new NestedObj()
            {
                Names = new List<string>() { { "Yusbel" }, { "Margaret" }, { "Isabel" }, { "Max" } }
            }, NestedComplexObject = new NestedComplexObject
            {
                TaskName = "Work", NestedObjects = null
            } };
            var resultKeyValuePair = JsonConvert.SerializeObject(obj, new JsonSerializerSettings() 
            { 
                NullValueHandling = NullValueHandling.Ignore
            });
            try
            {
                var str = resultKeyValuePair;
                var jObj = JObject.Parse(str);
                var flatten = jObj.Flatten();
                var flat = flatten.ConvertObjectToBase64();

                var jsonStr = JsonConvert.SerializeObject(flat, Formatting.Indented);
                try
                {
                    
                    var testObj = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonStr);
                }
                catch (Exception e)
                {

                    throw;
                }

                var resultDeserialize = JsonConvert.DeserializeObject<KeyValuePair<string, string>>(resultKeyValuePair);
            }
            catch (Exception e)
            {
                throw;
            }

        }
    }
}
