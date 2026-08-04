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
}
