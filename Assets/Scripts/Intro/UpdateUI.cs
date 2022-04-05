using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UpdateUI : MonoBehaviour
{
    [SerializeField] Button updateButton;
    [SerializeField] Button cafeButton;

    private void Start()
    {
        updateButton.onClick.AddListener(OnUpdate);
        cafeButton.onClick.AddListener(OnCafe);
    }

    private void OnUpdate()
    {
        Application.OpenURL("market://details?id=com.ablegames.SideTab");
        GameApplication.Instance.Quit();
    }

    private void OnCafe()
    {
        Application.OpenURL("https://cafe.naver.com/ArticleList.nhn?search.clubid=30546852&search.menuid=1&search.boardtype=L");
        // GameApplication.Instance.Quit();
    }
}
