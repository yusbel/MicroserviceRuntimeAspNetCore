using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.Extensions.Azure;
using Microsoft.Graph.Models;
using Sample.Sdk.Msg.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Security
{
    internal class ExternalMessageConverter
    {
        public JsonObject EncryptMessage(JsonObject obj, JsonObject encryptedObject) 
        {
            //First select non json object and add to encrypted object
            var list = obj.ToList();
            
            var jsonObjects = new BlockingCollection<JsonObject>();
            jsonObjects.Add(obj);
            obj.ToList().ForEach(jo =>
            {
                if (jo.Value is JsonObject jObj)
                    jsonObjects.Add(jObj);
            });
            var index = 0;
            while (jsonObjects.Any()) 
            {
                var jsonObject = jsonObjects.Take();
                foreach(var jv in jsonObject)
                {
                    if (jv.Value is JsonValue jsonValue) 
                    {
                        EncryptedValue(jv, encryptedObject);
                        continue;
                    }
                    if (jv.Value is JsonArray jsonArray) 
                    {
                        foreach (var item in jsonArray) 
                        {
                            if (item is JsonObject itemObj) 
                            {
                                jsonObjects.Add(itemObj);
                                continue;
                            }
                        }
                        EncryptedJsonArray(jsonArray, encryptedObject, jv.Key);
                    }
                }
            }
            return obj;
        }

        private void EncryptedJsonArray(JsonArray jsonArray, JsonObject encryptedObject, string index) 
        {
            var array = new JsonArray();
            for(var i =0; i < jsonArray.Count; i++)
            {
                if(jsonArray[i] is JsonValue jsonValue)
                {
                    array.Add(JsonValue.Create($"Encrypted_{jsonValue.ToString()}"));
                }
            }
            encryptedObject.Remove(index);
            encryptedObject.Add(new KeyValuePair<string, JsonNode>(index, array));
        }
        private void EncryptedValue(KeyValuePair<string, JsonNode> jsonObj, JsonObject encryptedObject) 
        {
            var jsonValue = JsonValue.Create($"Encrypted_{jsonObj.Value.ToString()}");
            try
            {
                encryptedObject.Remove(jsonObj.Key);
                encryptedObject.Add(jsonObj.Key, jsonValue);
            }
            catch (Exception e)
            {

                throw;
            }
        }
    }
}
