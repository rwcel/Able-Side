using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AccountUI : PopupUI
{
    // [SerializeField] GameObject[] commonObjs;
    [SerializeField] GameObject[] federationObjs;
    [SerializeField] GameObject[] guestObjs;

    [SerializeField] Button googleButton;
    [SerializeField] Button facebookButton;
    [SerializeField] Button logOutButton;
    [SerializeField] Button signOutButton;

    BackEndServerManager _BackEndServerManager;

    private void Start()
    {
        BackEndServerManager _BackEndServerManager = BackEndServerManager.Instance;

        googleButton.onClick.AddListener(() => _BackEndServerManager.ChangeFederation(ELogin.Google));
        facebookButton.onClick.AddListener(() => _BackEndServerManager.ChangeFederation(ELogin.Facebook));
        logOutButton.onClick.AddListener(LogOut);
        signOutButton.onClick.AddListener(SignOut);

        closeButton.onClick.AddListener(_GamePopup.ClosePopup);
    }

    protected override void UpdateData()
    {
        if(_BackEndServerManager == null)
        {
            _BackEndServerManager = BackEndServerManager.Instance;
        }

        switch (_BackEndServerManager.LoginType)
        {
            case ELogin.Google:
            case ELogin.Facebook:
                foreach (var obj in federationObjs)
                {
                    obj.SetActive(true);
                }
                foreach (var obj in guestObjs)
                {
                    obj.SetActive(false);
                }
                break;
            case ELogin.Guest:
                foreach (var obj in federationObjs)
                {
                    obj.SetActive(false);
                }
                foreach (var obj in guestObjs)
                {
                    obj.SetActive(true);
                }
                break;
        }
    }

    private void LogOut()
    {
        SystemPopupUI.Instance.OpenTwoButton(84, 85, 53, 22, _BackEndServerManager.LogOut, null);
    }

    private void SignOut()
    {
        SystemPopupUI.Instance.OpenTwoButton(84, 86, 53, 22, _BackEndServerManager.SignOut, null);
    }
}
