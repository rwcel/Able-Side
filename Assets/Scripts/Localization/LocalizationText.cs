using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LocalizationText : MonoBehaviour
{
    [SerializeField] int languageNum;

    private TextMeshProUGUI text;

    private LocalizationManager _LocalizationManager;

    private void Awake()
    {
        text = GetComponent<TextMeshProUGUI>();
        _LocalizationManager = LocalizationManager.Instance;
    }

    private void OnEnable()
    {
        text.text = languageNum.Localization();

        //LocalizationManager.Instance.OnChangeLanguage += 
        //    () => text.text = languageNum.Localization();

        _LocalizationManager.AddLocalize(text, languageNum);
    }

    private void OnDisable()
    {
        if (_LocalizationManager == null)
            return;

        _LocalizationManager.SubLocalize(text);
    }

    public void ChangeText(int num)
    {
        if (_LocalizationManager == null)
            _LocalizationManager = LocalizationManager.Instance;

        //_LocalizationManager.SubLocalize(text, languageNum);
        languageNum = num;
        text.text = languageNum.Localization();
        //_LocalizationManager.AddLocalize(text, languageNum);
    }
}
