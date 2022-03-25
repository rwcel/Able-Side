using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PauseUI : PopupUI
{
    [SerializeField] Button resumeButton;
    [SerializeField] Button exitButton;

    private void Start()
    {
        resumeButton.onClick.AddListener(_GamePopup.ClosePopup);
        exitButton.onClick.AddListener(OnExit);
    }

    public void OnExit()
    {
        SystemPopupUI.Instance.OpenTwoButton(84, 87, 80, 22, OnClickToLobby, null);
    }

    // **메인메뉴 기능이 있으면 그쪽으로 만들어야..
    public void OnClickToLobby()
    {
        GameManager.Instance.IsGameStart = false;
        _GamePopup.ClosePopup();
    }
}
