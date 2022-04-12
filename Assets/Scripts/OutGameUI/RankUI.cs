using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RankUI : DockUI
{
    [SerializeField] GameObject rankPrefab;
    [SerializeField] Transform poolParent;

    [Header("1,2,3��")]
    [SerializeField] RankItem[] topRanks;

    [Header("�ڽ��� ������")]
    [SerializeField] Image iconImage;
    [SerializeField] TextMeshProUGUI myRankText;
    [SerializeField] TextMeshProUGUI myNameText;
    [SerializeField] TextMeshProUGUI myScoreText;
    [SerializeField] TextMeshProUGUI myComboText;

    // private List<RankItem> rankList = new List<RankItem>();

    BackEndServerManager _BackEndServerManager;

    public override void OnStart()
    {
        // UpdateDatas���� ��� ó��
        _BackEndServerManager = BackEndServerManager.Instance;
    }

    /// <summary>
    /// ��ü ��ŷ, �ڽ� ��ŷ ���
    /// </summary>
    public override void UpdateDatas()
    {
        var rankList = BackEndServerManager.Instance.GetScoreRankList();
        if (rankList == null)               // �����ʿ��� ������ ���� Ȯ��
            return;

        int count = poolParent.childCount;
        int topTier = topRanks.Length;

        // Debug.Log($"rank List count : {rankList.Count}");

        for (int i = 0, length = topTier; i < length && i < rankList.Count; i++)
        {
            topRanks[i].SetData(rankList[i]);
        }
        // ������ �����ϸ� Instantiate ���ϰ� �ֱ�
        for (int i = topTier, length = count + topTier; i < length; i++)
        {
            poolParent.GetChild(i - topRanks.Length).GetComponent<RankItem>().SetData(rankList[i]);
        }
        // ������ Isntantiate
        for (int i = count + topTier, length = rankList.Count; i < length; i++)
        {
            var item = Instantiate(rankPrefab, poolParent).GetComponent<RankItem>();
            item.SetData(rankList[i]);
        }

        MyRankDatas();
    }

    private void MyRankDatas()
    {
        GameManager _GameManager = GameManager.Instance;

        myRankText.text = _BackEndServerManager.GetMyScoreRank().ToString();//.Ordinalnumber();
        iconImage.sprite = _GameManager.ProfileData.sprite;
        myNameText.text = _GameManager.NickName;
        myScoreText.text = _GameManager.BestScore.CommaThousands();
        myComboText.text = _GameManager.BestMaxCombo.ToString();
    }
}
