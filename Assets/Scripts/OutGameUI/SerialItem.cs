using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SerialItem : MailItem
{
    private enum EState
    {
        Progress,
        Clear,
        Fail,
    }

    [Header("�ø���")]
    [SerializeField] Image percentImage;
    [SerializeField] TextMeshProUGUI percentText;

    [Header("��������Ʈ")]
    [SerializeField] Sprite[] iconSprites;

    private float percent;

    private EState state;

    public void SetData(string date, int score)
    {
        PostInfo.serialDate = date;
        PostInfo.count = score;
    }

    public override void UpdateData()
    {
        base.UpdateData();

        // *score �ٽ� ��� �ʿ�

        percent = (float) BackEndServerManager.Instance.SerialScore(PostInfo.serialDate) / Values.DailySerialScore;
        Debug.Log($"{percent} == {(int)(percent * 100)}%");

        percentImage.fillAmount = percent;
        percentText.text = $"{(int)(percentImage.fillAmount * 100)}%";

        button.interactable = percent >= 1f;

        state = percent >= 1f ? EState.Clear :
                            (PostInfo.serialDate == BackEndServerManager.Instance.ServerDate ? EState.Progress : EState.Fail);

        iconImage.sprite = iconSprites[(int)state];
    }

    /// <summary>
    /// �ۼ�Ʈ�� �� á������ ���� ����
    /// </summary>
    protected override void RecvMail()
    {
        //base.RecvMail();
        //BackEndServerManager.Instance.ReceivePostItem(postInfo.serverType, postInfo.inDate);

        GameManager.Instance.SelectSerialCode = BackEndServerManager.Instance.RecvSerialCode(PostInfo.serialDate);

        // **�ڵ� ���� �ʿ� : ������?
        GamePopup.Instance.OpenPopup(EGamePopup.SerialCode);
    }
}
