using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


[System.Serializable]
public struct FTimeInfo
{
    public int score;                       // ���� ����
    // public float maxTime;            // ��ü �ð�
    public float decreaseTime;          // �ʴ� �پ��� �ð�
    public float successTime;          // �ʴ� �پ��� �ð�
    public float failTime;                  // ���� �ð�

    public FTimeInfo(int score, float decreaseTime, float successTime, float failTime)
    {
        this.score = score;
        this.decreaseTime = decreaseTime;
        this.successTime = successTime;
        this.failTime = failTime;
    }
}

[System.Serializable]
public struct FScoreComboInfo
{
    public int combo;            // ���� �޺�
    public int score;              // ������ ����
    public int rewardScore;     // ���� 1ȸ �޼� �� �ִ� ����
    public Color color;             // �޺� �÷� ����
}

[System.Serializable]
public struct FItemComboInfo
{
    public int combo;                         // ���� �޺�
    public int normalBoxCount;              // �Ϲ� ���� ����
    public float normalBoxPercent;       // �Ϲ� ���� %
    public int rareBoxCount;                // ������ ���� ����
    public float rareBoxPercent;            // ������ ���� %
}

#region ���̵� ���

[System.Serializable]
public struct FJumbleInfo
{
    public int score;                       // ���� ����
    public int count;                       // � �̵� �� Ȯ�� ����
    public int percent;                     // �߻� Ȯ��
    // ����ð� 2�� ����

    public FJumbleInfo(int score, int count, int percent)
    {
        this.score = score;
        this.count = count;
        this.percent = percent;
    }
}

[System.Serializable]
public struct FReverseInfo
{
    public int score;                        // ���� ����
    public int count;                       // � �̵� �� Ȯ�� ����
    public int percent;                     // �߻� Ȯ��
    public float time;                         // ���� �ð�
    // ����ð� 2�� ����

    public FReverseInfo(int score, int count, int percent, float time)
    {
        this.score = score;
        this.count = count;
        this.percent = percent;
        this.time = time;
    }
}

[System.Serializable]
public struct FBlurInfo
{
    public int score;                         // ���� ����
    public int count;                        // � �̵� �� Ȯ�� ����
    public int percent;                      // �߻� Ȯ��
    // ����ð� 2�� ����
    public float time;                 // ������ �ð�

    public FBlurInfo(int score, int count, int percent, float time)
    {
        this.score = score;
        this.count = count;
        this.percent = percent;
        this.time = time;
    }
}

#endregion

[CreateAssetMenu(fileName = "LevelData", menuName = "ScriptableObject/LevelData")]
public class LevelData : ScriptableObject
{
    private const string FileDirectory = "Assets/Resources";
    private const string FilePath = "Assets/Resources/LevelData.asset";
    private static LevelData instance;
    public static LevelData Instance
    {
        get
        {
            if (instance != null)
                return instance;
            instance = Resources.Load<LevelData>("LevelData");
#if UNITY_EDITOR
            if (instance == null)
            {
                if (!AssetDatabase.IsValidFolder(FileDirectory))
                {
                    AssetDatabase.CreateFolder("Assets", "Resources");
                }
                instance = AssetDatabase.LoadAssetAtPath<LevelData>(FilePath);
                if (instance == null)
                {
                    instance = CreateInstance<LevelData>();
                    AssetDatabase.CreateAsset(instance, FilePath);
                }
            }
#endif
            return instance;
        }
    }

    [Header("[�ΰ���]")]

    // InGame
    public Vector3 FirstPos = new Vector3(0f, -6.5f, 0f);
    public Vector3 BetweenPos = new Vector3(0f, 2f, 0.5f);

    public Vector3 LeftOutPos = new Vector3(-3.5f, -14f, -1f);
    public Vector3 RightOutPos = new Vector3(3.5f, -14f, -1f);

    public int MaxShowCharacterNum = 6;
    public int MaxSpawnCharacterNum = 8;

    [Header("[�ƿ�����]")]

    // OutGame
    public int MaxTicket = 5;
    public int TicketTime = 300;
    public int TicketPrice = 126;       // 3�� ����

    [Header("[��������]")]

    // LobbyItem
    public float MaxTimeBase = 10;

    public int MaxBombCount = 30;
    public int DecreaseBombCount = 5;

    public int FeverComboNum = 10;

    public int DeleteBombCharacters = 10;

    [Header("[������]")]

    public Sprite[] GoodsSprites;
    public Sprite PackageSprite;
    public LobbyItemData[] LobbyItemDatas;
    public InGameItemData[] InGameItemDatas;
    public FeverData[] FeverDatas;
    public ProfileData[] ProfileDatas;
    public ShopData[] ShopDatas;
    public DailyGiftData[] DailyGiftDatas;
    public LobbyItemData TicketData;            // LobbyItemData�޾Ƽ� ����

    [Header("[���̵�]")]
    public int BaseScore;

    // Difficulty
    public int[] OpenScores;
    public FTimeInfo[] TimeInfos;
    //public FTimeInfo[] TimeInfos =
    //    {
    //    new FTimeInfo(0, 1f, 1f, 2f),
    //    new FTimeInfo(50000, 1.5f, 1f, 2.5f),
    //    new FTimeInfo(300000, 2f, 1f, 3f),
    //    new FTimeInfo(1000000, 3f, 1f, 4f),
    //    new FTimeInfo(1000000, 3f, 1f, 4f),
    //};

    public FScoreComboInfo[] ScoreComboInfos;

    public FItemComboInfo[] ItemComboInfos;
    public int ItemComboUnit = 10;

    public FJumbleInfo[] JumbleInfos;
    public float JumblePauseTime = 2;

    public FReverseInfo[] ReverseInfos;
    public float ReversePauseTime = 2;

    public FBlurInfo[] BlurInfos;
    public float BlurPauseTime = 2;

}