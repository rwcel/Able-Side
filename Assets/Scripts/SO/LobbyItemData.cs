using UnityEditor;
using UnityEngine;


[CreateAssetMenu(fileName = "Lobby Item", menuName = "ScriptableObject/Lobby Item")]
public class LobbyItemData : ScriptableObject
{
    [Header("����")]
    public ELobbyItem type;
    public Sprite sprite;

    [Header("�̸�")]
    public string itemName;         // �ѱ�           // **type -> nameText ���� �ʿ�
    public int nameLanguageNum;
    [TextArea(1,5)]
    public string description;        // ����
    public int descLanguageNum;

    [Header("����")]
    public int value;               // ȿ��
    public int price;

    [Header("����")]
    // **�Ϸ� ���� ��û ���� ������ �� �� ����
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

/* *Value ����
 * AddTime : �ִ� �ð� ���� (��)
 * AddBomb : ��ź ȿ�� ����
 * MaxItem : ������ ä������ ����
 * SuperFeverStart : �����ǹ� ���� ȿ�� [�ð��� ����]
 * AddScore : ���� �� �߰� ���� ����
 * Shield : ��� Ƚ��
 * */
