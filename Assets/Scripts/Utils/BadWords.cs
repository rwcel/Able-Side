using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

// 영어 금칙어
// https://www.reddit.com/r/Twitch/comments/2wrrav/is_there_a_list_of_words_that_the_globally_banned/
// -> http://www.bannedwordlist.com/lists/swearWords.txt

public class BadWords : MonoBehaviour
{
    [SerializeField] TextAsset[] files;

    private List<string[]> rowList;

    private void Start()
    {
        rowList = new List<string[]>(files.Length);

        for (int i = 0, length = files.Length; i < length; i++)
        {
            rowList.Add(files[i].text.Split(new string[] { "\r\n" }, System.StringSplitOptions.RemoveEmptyEntries));
        }
    }

    public bool CheckFilter(string text)
    {
        if (text.Length <= 1)
            return false;

        text = text.ToLower();
        foreach (string[] rows in rowList)
        {
            foreach (var row in rows)
            {
                if (text.Contains(row))
                    return false;
            }
        }

        return true;
    }
}
