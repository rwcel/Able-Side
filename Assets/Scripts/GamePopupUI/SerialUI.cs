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

    protected override void Start()
    {
        base.Start();

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
        GameApplication.Instance.ShowWebView("Cafe", "https://cafe.naver.com/MemoList.nhn?search.clubid=30546852&search.menuid=20&viewType=pc");
    }

    void OnCopy()
    {
        SystemPopupUI.Instance.OpenNoneTouch(16);
        serialCode.CopyToClipboard();
    }
}
