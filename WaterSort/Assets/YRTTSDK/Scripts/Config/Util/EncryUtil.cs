using System;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using Beebyte.Obfuscator;

namespace YRTT
{
    public static class EncryUtil
    {
        static Dictionary<string, string> codeDict = new Dictionary<string, string>();
        static Dictionary<string, string> offsetDict = new Dictionary<string, string>();

        public static string GetPPPP(string url)
        {
            if (!codeDict.ContainsKey(url))
            {
                InitPV(url);
            }
            return codeDict[url];
        }
        public static string GetVVVV(string url)
        {
            if (!offsetDict.ContainsKey(url))
            {
                InitPV(url);
            }
            return offsetDict[url];
        }

        static void InitPV(string url)
        {
            string str = GetTopDomainName(url).MD5String();

            codeDict.Add(url, str.Substring(0, 16));
            offsetDict.Add(url, str.Substring(16, 16));
        }

        public enum EntryType
        {
            AES,
            Base64,
        }

        [SkipRename]
        /// <summary>
        /// 加密数据
        /// </summary>
        /// <param name="entryType"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public static string EncryData(this string content, string url = null, EntryType entryType = EntryType.AES)
        {
            if (string.IsNullOrEmpty(url))
            {
                url = YRTTConfig.ServerUrl;
            }
            try
            {
                switch (entryType)
                {
                    case EntryType.AES:
                        return AesEncrypt(content, GetPPPP(url), GetVVVV(url));
                }

            }
            catch (Exception e)
            {
                throw new Exception("加密失败：" + e.Message);
            }
            return "";
        }

        [SkipRename]
        /// <summary>
        /// 解密数据
        /// </summary>
        /// <param name="entryType">解密类型</param>
        /// <param name="content">解密内容</param>
        /// <returns></returns>
        public static string DecrypData(this string content, string url = null, EntryType entryType = EntryType.AES)
        {
            if(string.IsNullOrEmpty(url))
            {
                url = YRTTConfig.ServerUrl;
            }
            try
            {
                switch (entryType)
                {
                    case EntryType.AES:
                        return AesDecrypt(content, GetPPPP(url), GetVVVV(url));
                }
            }
            catch (Exception e)
            {
                Debug.LogError("解密失败：" + e.Message);
            }
            return content;
        }

        [SkipRename]
        /// <summary>
        ///  AES 加密
        /// </summary>
        /// <param name="str">明文（待加密）</param>
        /// <param name="key">密文</param>
        /// <returns></returns>
        private static string AesEncrypt(this string str, string key, string offset)
        {
            if (string.IsNullOrEmpty(str)) return null;
            Byte[] toEncryptArray = Encoding.UTF8.GetBytes(str);

            RijndaelManaged rm = new RijndaelManaged
            {
                Key = Encoding.UTF8.GetBytes(key),
                IV = Encoding.UTF8.GetBytes(offset),
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7
            };
            ICryptoTransform cTransform = rm.CreateEncryptor();
            Byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
            return Convert.ToBase64String(resultArray, 0, resultArray.Length);
        }
        [SkipRename]
        /// <summary>
        ///  AES 解密
        /// </summary>
        /// <param name="str">明文（待解密）</param>
        /// <param name="key">密文</param>
        /// <param name="offset">偏移量</param>
        /// <returns></returns>
        private static string AesDecrypt(this string str, string key, string offset)
        {
            if (string.IsNullOrEmpty(str)) return null;
            Byte[] toEncryptArray = Convert.FromBase64String(str);
            RijndaelManaged rm = new RijndaelManaged
            {
                Key = Encoding.UTF8.GetBytes(key),
                IV = Encoding.UTF8.GetBytes(offset),
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7
            };
            ICryptoTransform cTransform = rm.CreateDecryptor();
            Byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
            return Encoding.UTF8.GetString(resultArray);
        }


        /// <summary>
        /// 获取域名的顶级域名
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        public static string GetTopDomainName(string domain)
        {
            domain = domain.Trim().ToLower();
            // 删除协议
            if (domain.StartsWith("http://")) domain = domain.Substring(7);
            if (domain.StartsWith("https://")) domain = domain.Substring(8);
            if (domain.StartsWith("www.")) domain = domain.Substring(4);

            // 删除URL的路径部分
            int slashIndex = domain.IndexOf("/");
            if (slashIndex > 0) domain = domain.Substring(0, slashIndex);

            // 后缀列表，按长度排序，确保正确处理长后缀
            string[] rootDomains = new string[] { ".com.cn", ".gov.cn", ".cn", ".com", ".net", ".org", ".so", ".co", ".mobi",
                ".tel", ".biz", ".info", ".name", ".me", ".cc", ".tv", ".asia", ".hk", ".xyz",
                ".top", ".vip", ".club", ".online", ".live", ".nl", ".la", ".tangclub" };
            Array.Sort(rootDomains, (x, y) => y.Length.CompareTo(x.Length)); // 优先匹配长后缀

            // 查找匹配的后缀并提取域名
            foreach (string rootDomain in rootDomains)
            {
                if (domain.EndsWith(rootDomain))
                {
                    int rootPos = domain.Length - rootDomain.Length;
                    // 查找根域名前的最后一个点
                    int dotIndex = domain.LastIndexOf(".", rootPos - 1);
                    if (dotIndex >= 0)
                    {
                        return domain.Substring(dotIndex + 1);
                    }
                    return domain; // 如果没有点，返回整个域名
                }
            }
            return ""; // 如果没有匹配的后缀，返回空字符串
        }



    }
}