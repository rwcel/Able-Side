using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LocalizationManager : Singleton<LocalizationManager>
{
    [SerializeField] TextAsset englishText;
    [SerializeField] TextAsset koreanText;

    // public System.Action<ELanguage> OnChangeLanguage;

    //List<KeyValuePair<TextMeshProUGUI, int>> localizes = new List<KeyValuePair<TextMeshProUGUI, int>>();
    Dictionary<TextMeshProUGUI, int> localizes = new Dictionary<TextMeshProUGUI, int>();

    public Dictionary<int, string> englishs = new Dictionary<int, string>();
    public Dictionary<int, string> koreans = new Dictionary<int, string>();


    protected override void AwakeInstance()
    {
        var obj = FindObjectsOfType<LocalizationManager>();
        if (obj.Length == 1)
            DontDestroyOnLoad(gameObject);
        else
        {
            Destroy(gameObject);
        }
    }

    protected override void DestroyInstance() { }

    // Default : ¿µ¾î
    private void Start()
    {
        LoadLanguage();
    }

    void LoadLanguage()
    {
        var englishLines = englishText.text.Split(new string[] { "\n" }, System.StringSplitOptions.RemoveEmptyEntries);
        foreach (var row in englishLines)
        {
            string[] texts = row.Split(':');
            englishs.Add(int.Parse(texts[0]), texts[1].Replace(';', ':').Trim());
        }

        var koreanLines = koreanText.text.Split(new string[] { "\n" }, System.StringSplitOptions.RemoveEmptyEntries);
        foreach (var row in koreanLines)
        {
            string[] texts = row.Split(':');
            koreans.Add(int.Parse(texts[0]), texts[1].Replace(';', ':').Trim());
        }
    }

    public void AddLocalize(TextMeshProUGUI text, int num)
    {
        localizes.Add(text, num);
    }

    public void SubLocalize(TextMeshProUGUI text)
    {
        localizes.Remove(text);
    }

    public void OnLocalize(ELanguage language)
    {
        foreach (var item in localizes)
        {
            item.Key.text = language == ELanguage.English 
                                ? englishs[item.Value] : koreans[item.Value];
        }
    }

    //private void OnApplicationQuit()
    //{
    //    localizes = null;
    //}
}
