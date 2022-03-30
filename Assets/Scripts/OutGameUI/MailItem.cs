using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public struct FPostInfo
{
    public BackEnd.PostType serverType;         // Admin, Ranking, User
    public EPost postType;           // 일반, 패키지, 시리얼
    public EItem itemType;
    public int count;

    public string title;
    public string contents;
    public string inDate;
    public System.TimeSpan remainTime;
    public Sprite sprite;

    // id, Value
    public FItemInfo[] itemInfos;

    public string serialDate;
}

public class MailItem : MonoBehaviour
{
    [SerializeField] protected Image iconImage;
    [SerializeField] TextMeshProUGUI titleText;
    [SerializeField] TextMeshProUGUI contentsText;
    [SerializeField] TextMeshProUGUI remainText;
    [SerializeField] protected Button button;

    public FPostInfo PostInfo;

    public System.Action OnRecv;

    private void Start()
    {
        button.onClick.AddListener(RecvMail);
    }

    public void SetData(FPostInfo postInfo)
    {
        this.PostInfo = postInfo;

        iconImage.sprite = postInfo.sprite;
        titleText.text = postInfo.title;
        contentsText.text = postInfo.contents;
        remainText.text = postInfo.remainTime.HourRemainTime();
    }

    public virtual void UpdateData()
    {
        UpdateRemainText();
    }

    public void UpdateRemainText() => remainText.text = PostInfo.remainTime.HourRemainTime();

    /// <summary>
    /// 데이터 읽어오기
    /// </summary>
    protected virtual void RecvMail()
    {
        // Debug.Log($"아이템 획득 : {postInfo.itemType} - {postInfo.count}");
        GameManager.Instance.SelectPostInfo = PostInfo;

        // 미리 받아야 RewardUI가 설정됨
        BackEndServerManager.Instance.ReceivePostItem(PostInfo.serverType, PostInfo.inDate);

        GamePopup.Instance.OpenPopup(EGamePopup.Reward, 
            null, 
            () =>
            {
                gameObject.SetActive(false);
                OnRecv?.Invoke();
            });
    }
}
