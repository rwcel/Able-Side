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

    protected override void Start()
    {
        base.Start();

        BackEndServerManager _BackEndServerManager = BackEndServerManager.Instance;

        googleButton.onClick.AddListener(() => _BackEndServerManager.ChangeFederation(ELogin.Google));
        facebookButton.onClick.AddListener(() => _BackEndServerManager.ChangeFederation(ELogin.Facebook));
        logOutButton.onClick.AddListener(LogOut);
        signOutButton.onClick.AddListener(SignOut);
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
        SystemPopupUI.Instance.OpenTwoButton(15, 186, 0, 1, _BackEndServerManager.LogOut, null);
    }

    private void SignOut()
    {
        SystemPopupUI.Instance.OpenTwoButton(15, 187, 0, 1, _BackEndServerManager.SignOut, null);
    }
}
