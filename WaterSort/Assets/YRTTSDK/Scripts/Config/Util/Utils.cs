using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace YRTT
{

    public static class Utils
    {
        /// <summary>
        /// 添加子节点
        /// </summary>
        /// <param name="trans"></param>
        /// <returns></returns>
        public static GameObject AddChild(this Transform trans)
        {
            GameObject go = new GameObject();
            go.AddComponent<RectTransform>();
            go.transform.parent = trans;
            go.transform.localScale = Vector3.one;
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = new Quaternion(0, 0, 0, 1);

            return go;
        }

        /// <summary>
        /// 清除所有子节点
        /// </summary>
        /// <param name="trans"></param>
        public static void ClearChildren(this Transform trans)
        {
            int count = trans.childCount;
            for (int i = count - 1; i >= 0; i--)
            {
                GameObject.Destroy(trans.GetChild(i));
            }
        }


        /// <summary>
        /// 时区转换
        /// </summary>
        /// <param name="dateTime"></param>
        /// <param name="hours"> GMT +8  8</param>
        /// <returns></returns>
        public static DateTime ConvertTimeToUtc(this DateTime dateTime, double hours = 8)
        {
            DateTime dt = TimeZoneInfo.ConvertTimeToUtc(dateTime, TimeZoneInfo.Local);
            dt = dt.AddHours(hours);
            return dt;
        }

        public static string getRandomString(int length)
        {
            string s = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ123456789";
            StringBuilder sb = new StringBuilder();
            sb.Append(s[UnityEngine.Random.Range(0, 32)]);
            for (int i = 1; i < length; i++)
            {
                sb.Append(s[UnityEngine.Random.Range(0, s.Length)]);
            }

            return sb.ToString();
        }

        public static string getuuId()
        {
            return Guid.NewGuid().ToString();
        }

        /// <summary>
        /// 重置
        /// </summary>
        /// <param name="rect"></param>
        public static void resetRectTransform(this RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0, Screen.height);
            rect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Bottom, 0, Screen.height);
            rect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0, Screen.width);
            rect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Right, 0, Screen.width);
        }


        /// <summary>
        /// Base64加密，采用utf8编码方式加密
        /// </summary>
        /// <param name="source">待加密的明文</param>
        /// <returns>加密后的字符串</returns>
        public static string Base64Encode(this string source)
        {
            return source.Base64Encode(Encoding.UTF8);
        }

        /// <summary>
        /// Base64加密
        /// </summary>
        /// <param name="encodeType">加密采用的编码方式</param>
        /// <param name="source">待加密的明文</param>
        /// <returns></returns>
        public static string Base64Encode(this string source, Encoding encodeType)
        {
            string encode = string.Empty;
            byte[] bytes = encodeType.GetBytes(source);
            try
            {
                encode = Convert.ToBase64String(bytes);
            }
            catch
            {
                encode = source;
            }

            return encode;
        }

        /// <summary>
        /// MD5加密
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public static string MD5String(this string content)
        {
            string result = "";
            MD5 md5 = MD5.Create();
            byte[] bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(content));
            for (int i = 0; i < bytes.Length; ++i)
            {
                result += Convert.ToString(bytes[i], 16).PadLeft(2, '0');
            }

            return result.PadLeft(32, '0');
        }

        /// <summary>
        /// 获取时间戳
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static long ToMilliseconds(this DateTime dateTime)
        {
            return Convert.ToInt64((dateTime - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds);
        }

        public static long ToSeconds(this DateTime dateTime)
        {
            return Convert.ToInt64((dateTime - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalSeconds);
        }

        public static string ToSqlType(Type type)
        {
            if (type.Name.Equals(typeof(string).Name))
            {
                return "TEXT";
            }
            else if (type.Name.Equals(typeof(int).Name) || type.Name.Equals(typeof(uint).Name) || type.Name.Equals(typeof(short).Name) || type.Name.Equals(typeof(long).Name))
            {
                return "INTEGER";
            }
            else if (type.Name.Equals(typeof(float).Name) || type.Name.Equals(typeof(double).Name))
            {
                return "REAL";
            }
            else return type.Name;
        }

        public static string str(this long value)
        {
            return value.ToString();
        }

        /// <summary>
        /// 转最大整形
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int largest(this float value)
        {
            int a = Mathf.RoundToInt(value);
            if (value > a) a += 1;
            return a;
        }

        /// <summary>
        /// 对象上添加脚本
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="tran"></param>
        /// <returns></returns>
        public static T AddComponent<T>(this Transform tran) where T : MonoBehaviour
        {
            T t = tran.GetComponent<T>();
            if (t == null) t = tran.gameObject.AddComponent<T>();
            return t;
        }


        /// <summary>
        /// 数量单位换算
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        const string splitNormal = ".";
        const string splitOther = ",";
        public static string NumberToUnit(this string value)
        {
            value = value.GetRealNum();
            string split = splitNormal;
            bool hasDot = false;

            if (value.Contains(splitOther))
            {
                //split = splitOther;
                hasDot = true;

                value = value.Replace(splitOther, split);
            }

            string front, after;
            string[] s = value.Split(split.ToCharArray());

            front = s[0];
            after = s.Length >= 2 ? s[1] : string.Empty;
            int numL = 3;
            if (front.Length <= numL)
            {
                front = value.CombineFloatTemp().NumberTrimTemp();
            }
            else if (front.Length <= numL * 2)
            {
                front = front.Insert(front.Length - numL, split).CombineFloatTemp().NumberTrimTemp() + "K";
            }
            else if (front.Length <= numL * 3)
            {
                front = front.Insert(front.Length - numL * 2, split).CombineFloatTemp().NumberTrimTemp() + "M";
            }
            else
            {
                front = front.Insert(front.Length - numL * 3, split).NumberToUnit() + "B";
            }


            if (hasDot)
            {
                front = front.Replace(splitNormal, splitOther);
            }

            return front;
        }

        public static string NumberToUnit(this double value)
        {
            return NumberToUnit(value.ToString());
        }

        public static string NumberToUnit(this float value)
        {
            return NumberToUnit(value.ToString());
        }

        public static string NumberTrimTemp(this string s)
        {
            var split = splitNormal;
            if (s.Contains(splitOther))
            {
                split = splitOther;
            }

            while ((s.LastIndexOf('0') == s.Length - 1 || s.LastIndexOf(split) == s.Length - 1) && s.Contains(split))
            {
                s = s.Remove(s.Length - 1);
            }

            s = s.Replace(" ", "");
            return s;
        }

        /// <summary>
        /// 拼接浮点数,保留位数,不四舍五入
        /// </summary>
        /// <param name="value"></param>
        /// <param name="lenth"></param>
        /// <returns></returns>
        public static string CombineFloatTemp(this string value, int lenth = 2)
        {
            string split = splitNormal;
            if (value.Contains(splitOther))
            {
                split = splitOther;
            }

            string[] s = value.Split(split.ToCharArray());
            if (s.Length == 1 || s[1].Length <= 0)
            {
                return value;
            }

            StringBuilder sb = new StringBuilder(s[0]);
            sb.Insert(sb.Length, split);
            sb.Insert(sb.Length, s[1].Substring(0, Math.Min(s[1].Length, lenth)));

            return sb.ToString();
        }

        public static string NumberTrim(this string s)
        {
            string[] vaule = s.Split('.');
            if (vaule.Length == 1) return s;
            else
            {
                int num = int.Parse(vaule[1], System.Globalization.CultureInfo.InvariantCulture);
                if (num > 0) return s;
                return vaule[0];
            }
        }


        public static string GetRealNum(this string num)
        {
            int dotIndex = num.IndexOf(".");
            int eIndex = num.IndexOf("E");
            int addIndex = num.IndexOf("+");
            if (dotIndex < 0 || eIndex < 0 || addIndex < 0)
                return num;
            int numLen = num.Length - addIndex - 1;
            string content = num.Substring(addIndex + 1, numLen);
            int count = int.Parse(content, System.Globalization.CultureInfo.InvariantCulture);
            StringBuilder temp = new StringBuilder();
            temp.AppendFormat(num.Substring(0, dotIndex));
            temp.AppendFormat(num.Substring(dotIndex + 1, eIndex - dotIndex - 1));
            for (int i = 0; i < count - (eIndex - dotIndex) + 1; i++)
            {
                temp.Append("0");
            }

            // Debug.Log(temp.ToString());
            return temp.ToString();
        }

        #region

        public static string UrlEncode(this string value)
        {
            if (value == null)
            {
                return null;
            }

            byte[] bytes = Encoding.UTF8.GetBytes(value);
            return Encoding.UTF8.GetString(UrlEncode(bytes, 0, bytes.Length, false));
        }

        private static byte[] UrlEncode(byte[] bytes, int offset, int count, bool alwaysCreateNewReturnValue)
        {
            byte[] buffer = UrlEncode(bytes, offset, count);
            if ((alwaysCreateNewReturnValue && (buffer != null)) && (buffer == bytes))
            {
                return (byte[])buffer.Clone();
            }

            return buffer;
        }

        private static byte[] UrlEncode(byte[] bytes, int offset, int count)
        {
            if (!ValidateUrlEncodingParameters(bytes, offset, count))
            {
                return null;
            }

            int num = 0;
            int num2 = 0;
            for (int i = 0; i < count; i++)
            {
                char ch = (char)bytes[offset + i];
                if (ch == ' ')
                {
                    num++;
                }
                else if (!IsUrlSafeChar(ch))
                {
                    num2++;
                }
            }

            if ((num == 0) && (num2 == 0))
            {
                return bytes;
            }

            byte[] buffer = new byte[count + (num2 * 2)];
            int num4 = 0;
            for (int j = 0; j < count; j++)
            {
                byte num6 = bytes[offset + j];
                char ch2 = (char)num6;
                if (IsUrlSafeChar(ch2))
                {
                    buffer[num4++] = num6;
                }
                else if (ch2 == ' ')
                {
                    buffer[num4++] = 0x2b;
                }
                else
                {
                    buffer[num4++] = 0x25;
                    buffer[num4++] = (byte)IntToHex((num6 >> 4) & 15);
                    buffer[num4++] = (byte)IntToHex(num6 & 15);
                }
            }

            return buffer;
        }

        private static bool IsUrlSafeChar(char ch)
        {
            if ((((ch >= 'a') && (ch <= 'z')) || ((ch >= 'A') && (ch <= 'Z'))) || ((ch >= '0') && (ch <= '9')))
            {
                return true;
            }

            switch (ch)
            {
                case '(':
                case ')':
                case '*':
                case '-':
                case '.':
                case '_':
                case '!':
                    return true;
            }

            return false;
        }

        private static bool ValidateUrlEncodingParameters(byte[] bytes, int offset, int count)
        {
            if ((bytes == null) && (count == 0))
            {
                return false;
            }

            if (bytes == null)
            {
                //throw new ArgumentNullException("bytes");
            }

            if ((offset < 0) || (offset > bytes.Length))
            {
                //throw new ArgumentOutOfRangeException("offset");
            }

            if ((count < 0) || ((offset + count) > bytes.Length))
            {
                //throw new ArgumentOutOfRangeException("count");
            }

            return true;
        }

        private static char IntToHex(int n)
        {
            if (n <= 9)
            {
                return (char)(n + 0x30);
            }

            return (char)((n - 10) + 0x41);
        }

        //public static bool IsEmail(this string inputData)
        //{
        //    Regex RegEmail = new Regex("^\\s*([A-Za-z0-9_-]+(\\.\\w+)*@(\\w+\\.)+\\w{2,5})\\s*$");//w 英文字母或数字的字符串，和 [a-zA-Z0-9] 语法一样 
        //    Match m = RegEmail.Match(inputData);
        //    return m.Success;
        //}


        /// <summary>
        /// 将双精度浮点数转为带有K、M、G的简化字符串，并保留两位小数
        /// </summary>
        /// <param name="data">双精度浮点数</param>
        /// <returns>简化字符串</returns>
        public static string ConvertToKMG(double data, int decimalPlaces = 2)
        {
            var numK = 1000;
            if (Math.Abs(data) < numK)
            {

                return Math.Round(data, decimalPlaces).ToString();
            }
            if (Math.Abs(data) < (Math.Pow(numK, 2)))
            {

                //return Math.Round((data / numK), decimalPlaces) + "K"; //kb
                return Decimal2(data / numK) + "K";
            }
            if (Math.Abs(data) < Math.Pow(numK, 3))
            {

                //return Math.Round((data / Math.Pow(numK, 2)), decimalPlaces) + "M"; //M
                return Decimal2(data / Math.Pow(numK, 2)) + "M";
            }
            if (Math.Abs(data) < Math.Pow(numK, 4))
            {

                //return Math.Round((data / Math.Pow(numK, 3)), decimalPlaces) + "G"; //G
                return Decimal2(data / Math.Pow(numK, 3)) + "G";

            }

            //return Math.Round((data / Math.Pow(numK, 4)), decimalPlaces)  +"T"; //T
            return Decimal2(data / Math.Pow(numK, 4)) + "T";
        }

        public static double Decimal2(double num)
        {
            return (int)(num * 100) / 100.00;
        }



        #endregion
    }
}