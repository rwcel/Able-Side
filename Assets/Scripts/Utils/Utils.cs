using UnityEngine;
using System;
using System.Text.RegularExpressions;

public static class Utils
{
    public static string CommaThousands(this int value)
    {
        return string.Format("{0:#,0}", value);
    }

    public static void CopyToClipboard(this string str)
    {
        GUIUtility.systemCopyBuffer = str;
    }

    // 서수 구하기
    public static string Ordinalnumber(this int value)
    {
        // 순위권 밖
        if (value == 0 || value > 100)
            return "UnRanked";
        else if ((value % 10 == 1) && (value != 11))
            return $"{value}st";
        else if ((value % 10 == 2) && (value != 12))
            return $"{value}nd";
        else if ((value % 10 == 3) && (value != 13))
            return $"{value}rd";
        else
            return $"{value}th";
    }

    public static string RemainTime(this System.TimeSpan time)
    {
        if(time.Days > 0)
        {
            return $"{time.Days}Days";
        }
        else if(time.Hours > 0)
        {
            return $"{time.Hours}Hours";
        }
        else
        {
            return $"Under 1Hour";
        }
        //else if(time.Minutes > 0)
        //{
        //    return $"{time.Minutes}Minites";
        //}
        //else
        //{
        //    // *초 계속 갱신해줘야 함     -> 열어놨을때 계속 불러오게 할지
        //    return $"Under 1Minites";
        //}
    }

    // 현재 언어 알아야함
    public static string Localization(this int num)
    {
        return (BackEndServerManager.Instance.Language == ELanguage.English)
            ? LocalizationManager.Instance.englishs[num] 
            : LocalizationManager.Instance.koreans[num];
    }

    public static string HyphenWord(this string serial)
    {
        string result = Regex.Replace(serial, @"[^a-zA-Z0-9]", "").ToUpper();
        result = Regex.Replace(result, @"(\w{4})(\w{4})(\w{4})(\w{4})", "$1-$2-$3-$4");
        return result;
    }
}
