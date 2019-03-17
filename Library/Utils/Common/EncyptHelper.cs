using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Configuration;

namespace Utils.Common
{
    /// <summary>
    /// 加密解密类
    /// </summary>
    public static class EncyptHelper
    {
        /// <summary>
        /// 加密字符串
        /// </summary>
        /// <param name="orignString">原文</param>
        /// <param name="key">密钥,不能为空</param>
        /// <returns>加密后的数据</returns> 
        public static string Encypt(string orignString, string key)
        {
            if (orignString.GetString().Equals(string.Empty) || key.GetString().Equals(string.Empty)) return orignString;
            StringBuilder tempStringBuilder = null;
            using (DESCryptoServiceProvider provider = new DESCryptoServiceProvider())
            {
                byte[] strBytes = Encoding.UTF8.GetBytes(orignString);
                if (key.Length >= provider.Key.Length) 
                    provider.Key = ASCIIEncoding.UTF8.GetBytes(key.Substring(0, provider.Key.Length));
                else
                {
                    int len = provider.Key.Length - key.Length;
                    while (len-- > 0) { key += 'a'; }
                    provider.Key = ASCIIEncoding.UTF8.GetBytes(key);
                }
                provider.IV = provider.Key;
                MemoryStream ms = new MemoryStream();
                CryptoStream stream = new CryptoStream(ms, provider.CreateEncryptor(), CryptoStreamMode.Write);
                stream.Write(strBytes, 0, strBytes.Length);
                stream.FlushFinalBlock();
                byte[] data = ms.ToArray();
                tempStringBuilder = new StringBuilder(data.Length);
                foreach (byte b in ms.ToArray())
                    tempStringBuilder.AppendFormat("{0:X2}", b);
            }
            return tempStringBuilder.ToString();
        }

        /// <summary>
        /// 加密字符串,使用系统默认密钥加密
        /// </summary>
        /// <param name="orignString">原文</param>
        /// <returns>密文</returns>
        public static string Encypt(string orignString)
        {
            return Encypt(orignString, ConfigurationManager.AppSettings["EncyptKey"].GetString());
        }

        /// <summary>
        /// 解密字符串
        /// </summary>
        /// <param name="encyptString">密文</param>
        /// <param name="key">密钥,不能为空</param>
        /// <returns>原文</returns>
        public static string DesEncypt(string encyptString, string key)
        {
            if (encyptString.GetString().Equals(string.Empty) || key.GetString().Equals(string.Empty)) return encyptString;
            using (DESCryptoServiceProvider des = new DESCryptoServiceProvider())
            {
                byte[] strBytes = new byte[encyptString.Length / 2];
                for (int x = 0; x < encyptString.Length / 2; x++)
                {
                    int i = (Convert.ToInt32(encyptString.Substring(x * 2, 2), 16));
                    strBytes[x] = (byte)i;
                }
                if (key.Length >= des.Key.Length)
                    des.Key = ASCIIEncoding.UTF8.GetBytes(key.Substring(0, des.Key.Length));
                else
                {
                    int len = des.Key.Length - key.Length;
                    while (len-- > 0) { key += 'a'; }
                    des.Key = ASCIIEncoding.UTF8.GetBytes(key);
                }
                des.IV = des.Key;
                MemoryStream ms = new MemoryStream();
                CryptoStream cs = new CryptoStream(ms, des.CreateDecryptor(), CryptoStreamMode.Write);
                cs.Write(strBytes, 0, strBytes.Length);
                cs.FlushFinalBlock();
                return System.Text.Encoding.UTF8.GetString(ms.ToArray());   
            }
        }

        /// <summary>
        /// 解密字符串,原文必须是使用系统默认的密钥加密
        /// </summary>
        /// <param name="encyptString">密文</param>
        /// <returns>原文</returns>
        public static string DesEncypt(string encyptString)
        {
            return DesEncypt(encyptString, ConfigurationManager.AppSettings["EncyptKey"].GetString());
        }
    }
}