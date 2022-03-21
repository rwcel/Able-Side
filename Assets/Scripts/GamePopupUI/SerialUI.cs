using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SerialUI : PopupUI
{
    [SerializeField] TextMeshProUGUI serialText;
    [SerializeField] Button helpButton;
    [SerializeField] Button copyButton;
    [SerializeField] Button cancelButton;

    private string serialCode;

    private void Start()
    {
        helpButton.onClick.AddListener(OnHelp);
        copyButton.onClick.AddListener(OnCopy);
        cancelButton.onClick.AddListener(_GamePopup.ClosePopup);
    }

    protected override void UpdateData()
    {
        serialCode = GameManager.Instance.SelectSerialCode;
        serialText.text = serialCode.HyphenWord();
    }

    void OnHelp()
    {
        GameApplication.Instance.ShowWebView("Cafe", "https://cafe.naver.com/ablegames/23");
    }

    void OnCopy()
    {
        SystemPopupUI.Instance.OpenNoneTouch(72);
        serialCode.CopyToClipboard();
    }
}
