using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RewardUI : PopupUI
{
    [System.Serializable]
    private struct FRewardItem
    {
        public GameObject obj;
        public Image iconImage;
        public TextMeshProUGUI countText;
    }

    [SerializeField] TextMeshProUGUI titleText;
    // [SerializeField] TextMeshProUGUI contentsText;
    [SerializeField] Button recvButton;

    [SerializeField] FRewardItem[] rewardItems;

    protected override void Start()
    {
        base.Start();

        recvButton.onClick.AddListener(OnReceive);
    }

    protected override void UpdateData()
    {
        var items = BackEndServerManager.Instance.RecvItems;

        // Debug.Log("Reward UI Open : " + items.Count);

        for (int i = 0, length = items.Count; i < length; i++)
        {
            rewardItems[i].obj.SetActive(true);
        }
        for (int i = items.Count, length = rewardItems.Length; i < length; i++)
        {
            rewardItems[i].obj.SetActive(false);
        }

        for (int i = 0, length = items.Count; i < length; i++)
        {
            rewardItems[i].iconImage.sprite = items[i].icon;
            rewardItems[i].countText.text = $"x{items[i].count}";
        }
    }

    void OnReceive()
    {
        BackEndServerManager.Instance.ClearRecvItems();

        _GamePopup.ClosePopup();
    }
}
