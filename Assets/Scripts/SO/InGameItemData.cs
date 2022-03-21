using UnityEditor;
using UnityEngine;


[CreateAssetMenu(fileName = "InGame Item", menuName = "ScriptableObject/InGame Item")]
public class InGameItemData : ScriptableObject
{
    [Header("정보")]
    public EInGameItem type;
    public Sprite sprite;

    [Header("이름")]
    public string itemName;
    public int languageNum;

    [Header("효과")]
    public int value;                  // 효과
    public float valueTime;         // 효과 시간 (일부만)

    [Header("확률")]
    public float normalPercent;
    public float rarePercent;

    public float startPercent;

    //public bool inNormalBox;      // 퍼센트가 0이하면 false 
    //public bool inRareBox;


#if UNITY_EDITOR
    public void SaveSO()
    {
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
#endif
}
