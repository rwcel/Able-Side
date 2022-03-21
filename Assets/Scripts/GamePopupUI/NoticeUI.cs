using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// **ó�� ���� �� ȣ�� �ʿ� 
public class NoticeUI : PopupUI
{
    // *Button���� �����ؼ� Onclick ��� �� ���� ������ �ٸ���� ������ ���� �� �ֱ⶧���� serialize�� ����
    [SerializeField] GameObject[] engObjs;          
    [SerializeField] GameObject[] korObjs;

    [SerializeField] Button exitButton;

    GameApplication _GameApplication;

    private void Start()
    {
        _GameApplication = GameApplication.Instance;
        exitButton.onClick.AddListener(_GamePopup.ClosePopup);
    }

    protected override void UpdateData()
    {
        base.UpdateData();

        if (BackEndServerManager.Instance.Language == ELanguage.English)
        {
            foreach (var obj in engObjs)
            {
                obj.SetActive(true);
            }
            foreach (var obj in korObjs)
            {
                obj.SetActive(false);
            }
        }
        else
        {
            foreach (var obj in engObjs)
            {
                obj.SetActive(false);
            }
            foreach (var obj in korObjs)
            {
                obj.SetActive(true);
            }
        }
    }

    #region Button OnClick

    public void OnGuide()
    {
        _GameApplication.ShowWebView("Guide", "https://cafe.naver.com/ablegames/18");
    }

    public void OnCommunity()
    {
        _GameApplication.ShowWebView("Cafe", "https://cafe.naver.com/ablegames");
    }

    public void OnAblePang()
    {
        Application.OpenURL("market://details?id=com.ablegames.ablepang");
    }

    public void OnSteamBlock()
    {
        Application.OpenURL("market://details?id=com.ablegames.steamblock");
    }

    #endregion
}
