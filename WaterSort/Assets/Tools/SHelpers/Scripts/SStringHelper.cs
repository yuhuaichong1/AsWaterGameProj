using System.Linq;
using System.Text.RegularExpressions;

public static class SStringHelper
{
    private static string pattern = @"^(?!\.)(""([^""\r\\]|\\[""\r\\])*""|"
            + @"([-a-z0-9!#$%&'*+/=?^_`{|}~]|(?<!\.)\.)*)(?<!\.)"
            + @"@[a-z0-9][\w\.-]*[a-z0-9]\.[a-z][a-z\.]*[a-z]$";//判断邮箱格式的正则表达式判断条件
    
    /// <summary>
    /// 判断string是否符合邮箱格式
    /// </summary>
    /// <param name="email">string内容</param>
    /// <returns>是否符合</returns>
    public static bool IfEmail(this string email)
    {
        if (string.IsNullOrEmpty(email))
            return false;

        return Regex.IsMatch(email, pattern, RegexOptions.IgnoreCase); ;
    }

    /// <summary>
    /// 判断string是否符合电话号码格式
    /// </summary>
    /// <param name="phoneNumber">string内容</param>
    /// <returns>是否符合</returns>
    public static bool IfPhoneNumber(this string phoneNumber) 
    {
        if (string.IsNullOrEmpty(phoneNumber))
            return false;

        if(phoneNumber.Length < 7 ||  phoneNumber.Length > 11)
            return false;

        return !Regex.IsMatch(phoneNumber, @"[^0-9]"); ;
    }


    //验证CPF 测试示例：529.982.247-25
    public static bool IsValidCpf(this string cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf))
            return false;

        // 提取数字
        char[] buffer = new char[11];
        int count = 0;

        for (int i = 0; i < cpf.Length; i++)
        {
            char c = cpf[i];

            if (c >= '0' && c <= '9')
            {
                if (count >= 11)
                    return false;

                buffer[count] = c;
                count++;
            }
        }

        if (count != 11)
            return false;

        // 排除 00000000000、11111111111 等
        bool allSame = true;
        for (int i = 1; i < 11; i++)
        {
            if (buffer[i] != buffer[0])
            {
                allSame = false;
                break;
            }
        }

        if (allSame)
            return false;

        int sum = 0;
        for (int i = 0; i < 9; i++)
            sum += (buffer[i] - '0') * (10 - i);

        int remainder = sum % 11;
        int checkDigit1 = remainder < 2 ? 0 : 11 - remainder;

        if ((buffer[9] - '0') != checkDigit1)
            return false;

        sum = 0;
        for (int i = 0; i < 10; i++)
            sum += (buffer[i] - '0') * (11 - i);

        remainder = sum % 11;
        int checkDigit2 = remainder < 2 ? 0 : 11 - remainder;

        return (buffer[10] - '0') == checkDigit2;
    }
    private static readonly Regex PixPhoneRegex = new Regex(@"^\+?55[1-9][0-9]9[0-9]{8}$", RegexOptions.Compiled);
    /// <summary>
    /// 判断是否为合法的巴西 Pix 手机号（带不带 + 都接受）。
    /// </summary>
    public static bool IsPixPhone(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;
        // 去掉空格、连字符、括号等常见分隔符，但保留 +
        string cleaned = Regex.Replace(input, @"[\s\-\(\)]", "");
        return PixPhoneRegex.IsMatch(cleaned);
    }
    /// <summary>
    /// 校验并归一化为标准 Pix 密钥格式（带 + 的 E.164）。
    /// 返回 null 表示非法。
    /// </summary>
    public static string NormalizeToPixKey(string input)
    {
        if (!IsPixPhone(input))
            return null;
        string digits = Regex.Replace(input, @"[^\d]", ""); // 只留数字
        return "+" + digits;                                 // 统一补上 +
    }

    /// <summary>
    /// 判断传入文本是否为合法的巴西 CNPJ（企业税号）。
    /// 支持带标点（12.345.678/0001-95）或纯数字（12345678000195）两种输入。
    /// </summary>
    public static bool IsCnpj(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;
        // 只保留数字
        string digits = Regex.Replace(input, @"[^\d]", "");
        // CNPJ 必须是 14 位
        if (digits.Length != 14)
            return false;
        // 排除全部相同数字的非法情况（如 00000000000000）
        if (digits.All(c => c == digits[0]))
            return false;
        // 第一位校验码
        int[] weights1 = { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        if (CalcCheckDigit(digits, weights1) != (digits[12] - '0'))
            return false;
        // 第二位校验码
        int[] weights2 = { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        if (CalcCheckDigit(digits, weights2) != (digits[13] - '0'))
            return false;
        return true;
    }
    private static int CalcCheckDigit(string digits, int[] weights)
    {
        int sum = 0;
        for (int i = 0; i < weights.Length; i++)
            sum += (digits[i] - '0') * weights[i];
        int remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }
}
