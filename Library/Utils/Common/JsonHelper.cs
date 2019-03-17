using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Utils.Common
{
    public static class JsonHelper
    {
        /// <summary>
        /// 将对象序列化成JSON格式字符串
        /// </summary>
        /// <param name="o">对象</param>
        /// <returns>字符串</returns>
        public static string SerializeJsonString(object o)
        {
            try
            {
                return JsonConvert.SerializeObject(o);
            }
            catch { return string.Empty; }
        }
        public static T DeSerializerJsonString<T>(string jsonString) 
        {
            try
            {
                JsonSerializer serializer = new JsonSerializer();
                return new JsonSerializer().Deserialize<T>(new JsonTextReader(new StringReader(jsonString)));   
            }
            catch { return default(T); }
        }
        public static T DeSerializeAnonymousJsonString<T>(string jsonString, T anonymousType)
        {
            try
            {
                return JsonConvert.DeserializeAnonymousType(jsonString, anonymousType);
            }
            catch { return default(T); }
        }
    }
}
