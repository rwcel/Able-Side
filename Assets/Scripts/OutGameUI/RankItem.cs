using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public struct FRankInfo
{
    public int iconNum;
    public string nickname;
    public int score;
    public int combo;
    public int rank;
    public string inDate;
    public Sprite sprite;

    // Name, Value
    public List<string> itemList;
}

public class RankItem : MonoBehaviour
{
    [SerializeField] Image iconImage;
    [SerializeField] TextMeshProUGUI rankText;
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI scoreText;
    [SerializeField] TextMeshProUGUI comboText;

    private FRankInfo rankInfo;


    public void SetData(FRankInfo rankInfo)
    {
        this.rankInfo = rankInfo;

        iconImage.sprite = LevelData.Instance.ProfileDatas[rankInfo.iconNum].sprite;
        nameText.text = rankInfo.nickname;
        if (rankText != null)
            rankText.text = rankInfo.rank.ToString();//.Ordinalnumber();
        scoreText.text = rankInfo.score.CommaThousands();
        comboText.text = rankInfo.combo.ToString();
    }
}
