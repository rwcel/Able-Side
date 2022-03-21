using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;

public class MailUI : DockUI
{
    [SerializeField] GameObject nomailObj;
    [SerializeField] GameObject postPrefab;
    [SerializeField] GameObject serialPostPrefab;
    [SerializeField] Transform postParent;
    [SerializeField] ScrollRect scrollRect;

    List<MailItem> mailList = new List<MailItem>();

    public override void OnStart()
    {
        SetPost();

        // SetSerial();         서버에서 진행

        this.ObserveEveryValueChanged(_ => mailList.Count)
            .Subscribe(value => nomailObj.SetActive(value == 0))
            .AddTo(this.gameObject);
    }

    void SetPost()
    {
        // 메일 개수 서버에서 가져오기
        var postList = BackEndServerManager.Instance.GetPostList();

        // Instantiate 개수만큼
        foreach (var postData in postList)
        {
            var item = Instantiate(postPrefab, postParent).GetComponent<MailItem>();
            item.SetData(postData);
            item.OnRecv += () => mailList.Remove(item);

            mailList.Add(item);
        }
    }

    void SetSerial()
    {
        foreach (var serialData in BackEndServerManager.Instance.SerialScoreList)
        {
            var item = Instantiate(serialPostPrefab, postParent).GetComponent<SerialItem>();
            item.SetData(serialData.Key, serialData.Value);
            item.OnRecv += () => mailList.Remove(item);

            mailList.Add(item);
        }
    }

    public override void UpdateDatas()
    {
        scrollRect.verticalNormalizedPosition = 1f;

        nomailObj.SetActive(mailList.Count == 0);

        // 메일 시간 다시 계산?
        foreach (var mailItem in mailList)
        {
            mailItem.UpdateData();
        }

        SortMail();
    }

    public void SerialPost(FPostInfo postInfo)
    {
        var item = Instantiate(serialPostPrefab, postParent).GetComponent<MailItem>();
        item.SetData(postInfo);

        mailList.Add(item);
    }

    public void SortMail()
    {
        mailList.Sort(delegate (MailItem a, MailItem b)
        {
            if(a.PostInfo.postType == b.PostInfo.postType)
            {
                if (a.PostInfo.remainTime > b.PostInfo.remainTime)
                    return 1;
                else
                    return -1;
            }
            else
            {
                return a.PostInfo.postType > b.PostInfo.postType ? 1 : -1;
            }
        });
    }
}
