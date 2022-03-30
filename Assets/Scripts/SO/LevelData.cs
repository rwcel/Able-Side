using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


[System.Serializable]
public struct FTimeInfo
{
    public int score;                       // 적용 점수
    // public float maxTime;            // 전체 시간
    public float decreaseTime;          // 초당 줄어드는 시간
    public float successTime;          // 초당 줄어드는 시간
    public float failTime;                  // 감점 시간

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
    public int combo;            // 적용 콤보
    public int score;              // 오르는 점수
    public int rewardScore;     // 최초 1회 달성 시 주는 점수
    public Color color;             // 콤보 컬러 색상
}

[System.Serializable]
public struct FItemComboInfo
{
    public int combo;                         // 적용 콤보
    public int normalBoxCount;              // 일반 상자 개수
    public float normalBoxPercent;       // 일반 상자 %
    public int rareBoxCount;                // 무지개 상자 개수
    public float rareBoxPercent;            // 무지개 상자 %
}

#region 난이도 요소

[System.Serializable]
public struct FJumbleInfo
{
    public int score;                       // 적용 점수
    public int count;                       // 몇개 이동 시 확률 적용
    public int percent;                     // 발생 확률
    // 연출시간 2초 공통

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
    public int score;                        // 적용 점수
    public int count;                       // 몇개 이동 시 확률 적용
    public int percent;                     // 발생 확률
    public float time;                         // 적용 시간
    // 연출시간 2초 공통

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
    public int score;                         // 적용 점수
    public int count;                        // 몇개 이동 시 확률 적용
    public int percent;                      // 발생 확률
    // 연출시간 2초 공통
    public float time;                 // 역방향 시간

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

    [Header("[인게임]")]

    // InGame
    public Vector3 FirstPos = new Vector3(0f, -6.5f, 0f);
    public Vector3 BetweenPos = new Vector3(0f, 2f, 0.5f);

    public Vector3 LeftOutPos = new Vector3(-3.5f, -14f, -1f);
    public Vector3 RightOutPos = new Vector3(3.5f, -14f, -1f);

    public int MaxShowCharacterNum = 6;
    public int MaxSpawnCharacterNum = 8;

    [Header("[아웃게임]")]

    // OutGame
    public int MaxTicket = 5;
    public int TicketTime = 300;
    public int TicketPrice = 126;       // 3개 구입

    [Header("[게임정보]")]

    // LobbyItem
    public float MaxTimeBase = 10;

    public int MaxBombCount = 30;
    public int DecreaseBombCount = 5;

    public int FeverComboNum = 10;

    public int DeleteBombCharacters = 10;

    [Header("[아이템]")]

    public Sprite[] GoodsSprites;
    public Sprite PackageSprite;
    public LobbyItemData[] LobbyItemDatas;
    public InGameItemData[] InGameItemDatas;
    public FeverData[] FeverDatas;
    public ProfileData[] ProfileDatas;
    public ShopData[] ShopDatas;
    public DailyGiftData[] DailyGiftDatas;
    public LobbyItemData TicketData;            // LobbyItemData받아서 쓰기

    [Header("[난이도]")]
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