using UnityEditor;
using UnityEngine;


[CreateAssetMenu(fileName = "Lobby Item", menuName = "ScriptableObject/Lobby Item")]
public class LobbyItemData : ScriptableObject
{
    [Header("정보")]
    public ELobbyItem type;
    public Sprite sprite;

    [Header("이름")]
    public string itemName;         // 한글           // **type -> nameText 변경 필요
    public int nameLanguageNum;
    [TextArea(1,5)]
    public string description;        // 설명
    public int descLanguageNum;

    [Header("내용")]
    public int value;               // 효과
    public int price;

    [Header("광고")]
    // **하루 광고 시청 제한 변수가 들어갈 수 있음
    public bool isFree;


#if UNITY_EDITOR
    public void SaveSO()
    {
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
#endif
}

/* *Value 설명
 * AddTime : 최대 시간 증가 (초)
 * AddBomb : 폭탄 효과 증가
 * MaxItem : 아이템 채워지는 개수
 * SuperFeverStart : 슈퍼피버 점수 효과 [시간은 랜덤]
 * AddScore : 종료 시 추가 점수 혜택
 * Shield : 방어 횟수
 * */
