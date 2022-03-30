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


    protected override void Start()
    {
        base.Start();

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
        // 확률 계산
        var itemInfo = BackEndServerManager.Instance.RecvItems[0];
        if(itemInfo.icon != null)
        {
            iconImage.sprite = itemInfo.icon;
        }
    }

    void OnReceive()
    {
        // 두번이상 클릭 못하게
        if (isOpen)
            return;

        _GamePopup.OpenPopup(EGamePopup.Reward, () => isOpen = true, () => _GamePopup.AllClosePopup(null));
    }
}
