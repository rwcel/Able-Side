using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CouponUI : PopupUI
{
    [SerializeField] TMP_InputField inputField;
    [SerializeField] Button applyButton;
    [SerializeField] Button cancelButton;

    private string couponText;

    private void Start()
    {
        applyButton.onClick.AddListener(CheckCoupon);
        cancelButton.onClick.AddListener(_GamePopup.ClosePopup);

        //inputField.onEndEdit
        inputField.onValueChanged.AddListener(ChangeText);
    }

    protected override void UpdateData()
    {
        base.UpdateData();

        inputField.text = "";
    }

    private void ChangeText(string text)
    {
        inputField.text = text.ToUpper();
    }

    private void CheckCoupon()
    {
        couponText = inputField.text;       // ÇÏÀÌÇÂ ±ß°Å³ª ¾ÈÇÔ

        if (BackEndServerManager.Instance.IsValidCoupon(couponText))
        {
            _GamePopup.OpenPopup(EGamePopup.Reward, null, () => _GamePopup.AllClosePopup(null));
            SystemPopupUI.Instance.OpenNoneTouch(72);
        }
        else
        {
            SystemPopupUI.Instance.OpenNoneTouch(79);
            inputField.text = "";
        }
    }
}
