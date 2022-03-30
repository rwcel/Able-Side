using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PrivacyUI : MonoBehaviour
{
    [SerializeField] LoginUI loginUI;

    [Header("토글")]
    [SerializeField] Toggle termToggle;
    [SerializeField] Toggle policyToggle;
    [SerializeField] Toggle pushToggle;

    [Header("버튼")]
    [SerializeField] Button termButton;
    [SerializeField] Button policyButton;

    [SerializeField] Button allAgreeButton;
    [SerializeField] Button agreeButton;


    private void Start()
    {
        agreeButton.onClick.AddListener(OnAgree);
        allAgreeButton.onClick.AddListener(OnAllAgree);

        termButton.onClick.AddListener(OnWebTerm);
        policyButton.onClick.AddListener(OnWebPolicy);

        termToggle.onValueChanged.AddListener(CheckAgreeButton);
        policyToggle.onValueChanged.AddListener(CheckAgreeButton);
        pushToggle.onValueChanged.AddListener(CheckPush);
    }

    void OnWebTerm()
    {
        GameApplication.Instance.ShowWebView("AbleX", "http://ablegames.co.kr/terms-of-service");
    }

    void OnWebPolicy()
    {
        GameApplication.Instance.ShowWebView("AbleX", "http://ablegames.co.kr/privacy-policy");
    }

    /// <summary>
    /// value 사용 안함
    /// </summary>
    void CheckAgreeButton(bool value)
    {
        agreeButton.interactable = (termToggle.isOn && policyToggle.isOn);
    }

    void CheckPush(bool value)
    {
        BackEndServerManager.Instance.SetPushNotification(value);
    }

    void OnAllAgree()
    {
        termToggle.isOn = true;
        policyToggle.isOn = true;

        CheckPush(true);

        OnAgree();
    }

    void OnAgree()
    {
        loginUI.ShowNickNameUI();
    }
}
