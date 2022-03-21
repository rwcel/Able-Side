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
        logOutButton.onClick.AddListener(_BackEndServerManager.LogOut);
        signOutButton.onClick.AddListener(_BackEndServerManager.SignOut);
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
}
