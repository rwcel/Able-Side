using UnityEditor;
using UnityEngine;


[CreateAssetMenu(fileName = "InGame Item", menuName = "ScriptableObject/InGame Item")]
public class InGameItemData : ScriptableObject
{
    [Header("����")]
    public EInGameItem type;
    public Sprite sprite;

    [Header("�̸�")]
    public string itemName;
    public int languageNum;

    [Header("ȿ��")]
    public int value;                  // ȿ��
    public float valueTime;         // ȿ�� �ð� (�Ϻθ�)

    [Header("Ȯ��")]
    public float normalPercent;
    public float rarePercent;

    public float startPercent;

    //public bool inNormalBox;      // �ۼ�Ʈ�� 0���ϸ� false 
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
