using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PrivacyUI : MonoBehaviour
{
    [SerializeField] Toggle termToggle;
    [SerializeField] Toggle policyToggle;

    [SerializeField] Button termButton;
    [SerializeField] Button policyButton;

    private void Start()
    {
        termButton.onClick.AddListener(OnTerm);
        policyButton.onClick.AddListener(OnPolicy);
    }

    void OnTerm()
    {
        GameApplication.Instance.ShowWebView("AbleX", "http://ablegames.co.kr/terms-of-service");
    }

    void OnPolicy()
    {
        GameApplication.Instance.ShowWebView("AbleX", "http://ablegames.co.kr/terms-of-service");
    }

    void AllAgree()
    {
        termToggle.isOn = true;
        policyToggle.isOn = true;
    }
}
