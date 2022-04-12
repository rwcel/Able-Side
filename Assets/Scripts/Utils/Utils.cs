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

    // ���� ���ϱ�
    public static string Ordinalnumber(this int value)
    {
        // ������ ��
        if (value == 0 || value > 100)
            return "-";
            //return 233.Localization();

        if (BackEndServerManager.Instance.Language == ELanguage.English)
        {
            if ((value % 10 == 1) && (value != 11))
                return $"{value}st";
            else if ((value % 10 == 2) && (value != 12))
                return $"{value}nd";
            else if ((value % 10 == 3) && (value != 13))
                return $"{value}rd";
            else
                return $"{value}th";
        }

        return $"{value}��";
    }

    /// <summary>
    /// �ð� ���� ���� �ð�
    /// </summary>
    public static string HourRemainTime(this System.TimeSpan time)
    {
        if(time.Days > 0)
        {
            return string.Format("{0}{1}", time.Days, time.Days > 1 ? "Days" : "Day");
        }
        else if(time.Hours > 1)
        {
            return string.Format("{0}{1}", time.Hours,  "Hours");
        }
        else
        {
            return "1Hour";
        }

        //else if(time.Hours > 1)
        //{
        //    return $"{time.Hours}Hours";
        //}
    }

    public static string MinRemainTime(this System.TimeSpan time)
    {
        int result = 0;
        if (time.Hours > 0)
        {   // 30�� �̻��̸� �ݿø�
            result = time.Minutes >= 30 ? time.Hours + 1 : time.Hours;
            return $"{result}{18.Localization()}";
        }
        else if(time.Minutes > 0)
        {
            result = time.Minutes + 1;
            return $"{result}{19.Localization()}";
            //result = time.Seconds >= 30 ? time.Minutes + 1 : time.Minutes;
            //return $"{result}{19.Localization()}";
        }
        else
        {   // 0�϶���..?
            if(time.Seconds > 0)
            {
                return $"{1}{19.Localization()}";
            }
            else
            {
                return 151.Localization();
            }
        }
    }

    // ���� ��� �˾ƾ���
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

    public static Color ComboColor(this int combo)
    {
        int idx = 0;
        var scoreComboInfos = LevelData.Instance.ScoreComboInfos;
        Color result = scoreComboInfos[idx].color;

        if(combo >= scoreComboInfos[scoreComboInfos.Length - 1].combo)
        {
            return scoreComboInfos[scoreComboInfos.Length-1].color;
        }

        while (combo >= scoreComboInfos[idx].combo)
        {
            result = scoreComboInfos[idx++].color;
        } 

        return result;
    }

    public static string NumberFormat(this int num)
    {
        if (BackEndServerManager.Instance.Language == ELanguage.English)
        {
            if (num >= 1000000)
            {
                return $"{num / 1000000}M";
            }
            else if (num >= 1000)
            {
                return $"{num / 1000}K";
            }
        }
        else
        {
            if(num >= 100000000)
            {
                return $"{num / 100000000}��";
            }
            else if(num >= 10000)
            {
                return $"{num / 10000}��";
            }
        }
        return num.ToString();
    }

    /// <summary>
    /// ���ڸ��������� �����
    /// </summary>
    public static int VersionCheck(this string version)
    {
        int result = 0;

        var split = version.Split('.');

        result += int.Parse(split[0]) * 10000;
        result += int.Parse(split[1]) * 100;
        result += int.Parse(split[2]);

        return result;
    }
}
