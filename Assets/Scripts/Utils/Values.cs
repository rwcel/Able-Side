using UnityEngine;


public static class Values
{
    #region LevelData

    public static LevelData _LevelData = LevelData.Instance;

    public static readonly Vector3 FirstPos = _LevelData.FirstPos;
    public static readonly Vector3 BetweenPos = _LevelData.BetweenPos;

    public static readonly Vector3 LeftOutPos = _LevelData.LeftOutPos;
    public static readonly Vector3 RightOutPos = _LevelData.RightOutPos;

    public static readonly Vector3 FrontParticlePos = _LevelData.FirstPos + new Vector3(0,1,0);

    public static readonly int MaxShowCharacterNum = _LevelData.MaxShowCharacterNum;     // 한 게임에 보여줄 최대 캐릭터 수
    public static readonly int MaxSpawnCharacterNum = _LevelData.MaxSpawnCharacterNum;     // 최대 스폰 캐릭터 수

    public static readonly int[] OpenScores = _LevelData.OpenScores;
    public static readonly FTimeInfo[] TimeInfos = _LevelData.TimeInfos;

    // 아웃게임 정보
    public static readonly int MaxTicket = _LevelData.MaxTicket;
    public static readonly int TicketTime = _LevelData.TicketTime;

    // 인게임 정보
    public static readonly float MaxTimeBase = _LevelData.MaxTimeBase;
    public static readonly int MaxBombCount = _LevelData.MaxBombCount;
    public static readonly int DecreaseBombCount = _LevelData.DecreaseBombCount;
    public static readonly int FeverComboNum = _LevelData.FeverComboNum;
    public static readonly int DeleteBombCharacters = _LevelData.DeleteBombCharacters;

    // LobbyItem
    public static readonly int AddMaxTime_Value = _LevelData.LobbyItemDatas[(int)ELobbyItem.AddMaxTime].value;
    public static readonly int AddMaxTime_Price = _LevelData.LobbyItemDatas[(int)ELobbyItem.AddMaxTime].price;

    public static readonly int AddBombPower_Value = _LevelData.LobbyItemDatas[(int)ELobbyItem.AddBombPower].value;
    public static readonly int AddBombPower_Price = _LevelData.LobbyItemDatas[(int)ELobbyItem.AddBombPower].price;

    public static readonly int MaxItem_Value = _LevelData.LobbyItemDatas[(int)ELobbyItem.MaxItem].value;
    public static readonly int MaxItem_Price = _LevelData.LobbyItemDatas[(int)ELobbyItem.MaxItem].price;

    public static readonly int Shield_Value = _LevelData.LobbyItemDatas[(int)ELobbyItem.Shield].value;
    public static readonly int Shield_Price = _LevelData.LobbyItemDatas[(int)ELobbyItem.Shield].price;

    public static readonly int SuperFeverStart_Value = _LevelData.LobbyItemDatas[(int)ELobbyItem.SuperFeverStart].value;
    public static readonly int SuperFeverStart_Price = _LevelData.LobbyItemDatas[(int)ELobbyItem.SuperFeverStart].price;

    public static readonly int AddScore_Value = _LevelData.LobbyItemDatas[(int)ELobbyItem.AddScore].value;
    public static readonly int AddScore_Price = _LevelData.LobbyItemDatas[(int)ELobbyItem.AddScore].price;

    // TicketItem - LobbyItemData
    public static readonly int Ticket_Price = _LevelData.TicketData.price;
    public static readonly int Ticket_BuyValue = _LevelData.TicketData.value;
    public static readonly int Ticket_AdValue = 1;

    // InGameItem
    public static readonly int MaxFever = 3;

    // InGame
    public static readonly int BaseScore = _LevelData.BaseScore;
 
    public static readonly FScoreComboInfo[] ScoreComboInfos = _LevelData.ScoreComboInfos;

    public static readonly FItemComboInfo[] ItemComboInfos = _LevelData.ItemComboInfos;

    public static readonly int SuperFever_value = _LevelData.InGameItemDatas[(int)EInGameItem.SuperFever].value;
    public static readonly float SuperFever_valueTime = _LevelData.InGameItemDatas[(int)EInGameItem.SuperFever].valueTime;

    public static readonly FeverData StartFeverData = _LevelData.FeverDatas[(int)EFever.StartFever];
    public static readonly FeverData[] FeverDatas = _LevelData.FeverDatas;

    public static readonly float BonusCharacter_value = _LevelData.InGameItemDatas[(int)EInGameItem.BonusCharacter].value * 0.01f;

    // 난이도
    public static readonly FJumbleInfo[] JumbleInfos = _LevelData.JumbleInfos;
    public static readonly float JumblePauseTime = _LevelData.JumblePauseTime;

    public static readonly FReverseInfo[] ReverseInfos = _LevelData.ReverseInfos;
    public static readonly float ReversePauseTime = _LevelData.ReversePauseTime;

    public static readonly FBlurInfo[] BlurInfos = _LevelData.BlurInfos;
    public static readonly float BlurPauseTime = _LevelData.BlurPauseTime;

    // public static readonly ProfileData[] ProfileDatas = _LevelData.ProfileDatas;
    public static readonly float Bonus_AllProfile = 1;

    // 무료제공, 광고  -> 일일 초기화
    //public static readonly int Free_ItemGacha = 1;
    //public static readonly int Ad_ItemGacha = 3;

    //public static readonly int Free_Ticket = 0;
    //public static readonly int Ad_Ticket = 3;

    //public static readonly int Free_LobbyItem = 3;
    //public static readonly int Ad_LobbyItem = 5;
    //public static readonly int LobbyItemCharge = 2;             // 한번 보면 충전 수

    //public static readonly int Free_DoubleReward = 0;
    //public static readonly int Ad_DoubleReward = 4;
    //public static readonly int Free_Revive = 0;
    //public static readonly int Ad_Revive = 4;

    #endregion


    public static readonly string Key_Null = "";
    // public static readonly string Key_Character = "Character";
    public static readonly string Key_CorrectEffect = "Flash";
    public static readonly string Key_ShieldEffect = "Shield";

    public static readonly WaitForSeconds BombDelay = new WaitForSeconds(0.05f);
    public static readonly WaitForSeconds Delay05 = new WaitForSeconds(0.5f);
    public static readonly WaitForSeconds Delay1 = new WaitForSeconds(1f);
    //public static readonly WaitForSeconds Delay005 = new WaitForSeconds(0.05f);

    public static readonly int MaxInGameItemCount = 3;

    public static readonly float BombAddTime = 1f;            // 폭탄 고정적으로 1초 증가

    public static readonly int BaseSum = 10;

    public static readonly int StartID_Goods = 101;
    public static readonly int StartID_LobbyItem = 111;
    public static readonly int StartID_InGameItem = 117;
    public static readonly int StartID_DailyGift = 10001;

    public static readonly int UnlockSerialScore = 5000000;
    public static readonly int DailySerialScore = 1000000;

    public static readonly int PlayRewardScore = 250000;            // 게임 보상
    public static readonly int PlayRewardCount = 5;

    public static readonly int TimeRewardMaxTime = 14400;       // 4시간
    public static readonly int TimeRewardAdTime = 30;          // 30분
    public static readonly int TimeRewardAdCount = 4;             // 1회 4번 가능


    public static readonly int SerialRemainDay = 7;

    public static readonly float WarningTime = 2f;

    public static readonly int Max_Reward_Items = 5;

    public static readonly int Input_Limit_Coupon = 18;
    public static readonly int Input_Limit_Nickname = 12;

    // public static readonly int Infinity = 999;

    // 광고 딜레이
    // public static readonly int 
}
