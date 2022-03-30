using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TimeRewardUI : PopupUI
{
    [SerializeField] TextMeshProUGUI timeText;
    [SerializeField] TextMeshProUGUI reduceText;

    [SerializeField] Button recvButton;
    [SerializeField] Button reduceButton;


    protected override void Start()
    {
        base.Start();

        recvButton.onClick.AddListener(OnRecv);
        reduceButton.onClick.AddListener(OnReduceTime);
    }

    protected override void UpdateData()
    {
        base.UpdateData();

        if(System.DateTime.Now < _GameManager.TimeRewardTime)
        {   // 아직 못받음. 1분 이상을 유지해야함
            timeText.text = (_GameManager.TimeRewardTime.AddMinutes(1) - System.DateTime.Now).ToString(@"hh\:mm");
            recvButton.interactable = false;
        }
        else
        {   // 받을 수 있음
            timeText.text = 151.Localization();
            recvButton.interactable = true;
        }

        reduceText.text = string.Format(202.Localization(), Values.TimeRewardAdTime);
    }

    void OnRecv()
    {
        AudioManager.Instance.PlaySFX(ESFX.Touch);
        BackEndServerManager.Instance.Gacha(EGacha.TimeReward);
    }

    void OnReduceTime()
    {
        AudioManager.Instance.PlaySFX(ESFX.Touch);
        _GameManager.UseDailyGift(EDailyGift.TimeReward, CompleteAd);
    }

    void CompleteAd()
    {
        BackEndServerManager.Instance.Ad_TimeReward();
        UpdateData();
    }
}
