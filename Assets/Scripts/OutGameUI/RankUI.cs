using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RankUI : DockUI
{
    [SerializeField] GameObject rankPrefab;
    [SerializeField] Transform poolParent;

    [Header("1,2,3등")]
    [SerializeField] RankItem[] topRanks;

    [Header("자신의 데이터")]
    [SerializeField] Image iconImage;
    [SerializeField] TextMeshProUGUI myRankText;
    [SerializeField] TextMeshProUGUI myNameText;
    [SerializeField] TextMeshProUGUI myScoreText;
    [SerializeField] TextMeshProUGUI myComboText;

    // private List<RankItem> rankList = new List<RankItem>();

    BackEndServerManager _BackEndServerManager;

    public override void OnStart()
    {
        // UpdateDatas에서 모두 처리
        _BackEndServerManager = BackEndServerManager.Instance;
    }

    /// <summary>
    /// 전체 랭킹, 자신 랭킹 계산
    /// </summary>
    public override void UpdateDatas()
    {
        var rankList = BackEndServerManager.Instance.GetScoreRankList();
        if (rankList == null)               // 서버쪽에서 데이터 변경 확인
            return;

        int count = poolParent.childCount;
        int topTier = topRanks.Length;

        // Debug.Log($"rank List count : {rankList.Count}");

        for (int i = 0, length = topTier; i < length && i < rankList.Count; i++)
        {
            topRanks[i].SetData(rankList[i]);
        }
        // 기존에 존재하면 Instantiate 안하고 넣기
        for (int i = topTier, length = count + topTier; i < length; i++)
        {
            poolParent.GetChild(i - topRanks.Length).GetComponent<RankItem>().SetData(rankList[i]);
        }
        // 나머지 Isntantiate
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
