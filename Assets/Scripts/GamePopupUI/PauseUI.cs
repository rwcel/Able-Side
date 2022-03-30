using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PauseUI : PopupUI
{
    [SerializeField] Button resumeButton;
    [SerializeField] Button exitButton;

    protected override void Start()
    {
        base.Start();

        resumeButton.onClick.AddListener(_GamePopup.ClosePopup);
        exitButton.onClick.AddListener(OnExit);
    }

    public void OnExit()
    {
        SystemPopupUI.Instance.OpenTwoButton(15, 222, 0, 1, OnClickToLobby, null);
    }

    /// <summary>
    /// *Action보다 IsGameStart가 먼저 적용되어 NotAction 사용
    /// </summary>
    public void OnClickToLobby()
    {
        _GamePopup.ClosePopupNotAction();
        Time.timeScale = 1f;
        AudioManager.Instance.PauseBGM(false);

        GameManager.Instance.IsGameStart = false;
    }
}
