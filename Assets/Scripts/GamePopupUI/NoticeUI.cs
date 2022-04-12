using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// **처음 입장 시 호출 필요 
public class NoticeUI : PopupUI
{
    // *Button으로 지정해서 Onclick 사용 시 둘의 개수가 다른경우 문제가 생길 수 있기때문에 serialize로 연결
    [SerializeField] GameObject[] engObjs;          
    [SerializeField] GameObject[] korObjs;

    GameApplication _GameApplication;

    protected override void Start()
    {
        base.Start();

        _GameApplication = GameApplication.Instance;
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
        _GameApplication.ShowWebView("Cafe", "https://cafe.naver.com/ablegames/68");
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

