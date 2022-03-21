using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Purchasing;

public class DailyGachaUI : PopupUI
{
    //[SerializeField] TextMeshProUGUI countText;
    [SerializeField] Image iconImage;

    [SerializeField] Button boxButton;
    //[SerializeField] Button recvButton;

    bool isOpen;


    private void Start()
    {
        boxButton.onClick.AddListener(OnReceive);
        //recvButton.onClick.AddListener(OnReceive);
    }

    protected override void UpdateData()
    {
        isOpen = false;
        OnOpen();
    }

    private void OnOpen()
    {
        // 두번이상 클릭 못하게
        if (isOpen)
            return;

        isOpen = true;
        // 확률 계산

        var itemInfo = BackEndServerManager.Instance.Probability_NormalGacha();
        if(itemInfo.icon != null)
        {
            iconImage.sprite = itemInfo.icon;
        }
    }

    void OnReceive()
    {
        _GamePopup.OpenPopup(EGamePopup.Reward, null, () => _GamePopup.AllClosePopup(null));
    }
}
