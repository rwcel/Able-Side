public enum ESide
{
    Left, 
    Right, 
}

// �� ���ӿ� �ִ� 6����
public enum ECharacter
{
    Alpaca,
    Chicken,
    Frog, 
    Monkey,
    Bear,
    Dog,
    //Panda,
    //Pig,

    Bomb,           // �������� ��ź�� �ǵ���


    //Bomb = 99,
}

public enum EMode
{
    TimeAttack,
    Life
}

public enum EGamePopup
{
    ItemSelect,
    ItemBuy,
    Result,
    Pause,
    ProfileInfo,
    ProfileEdit,
    SerialCode,
    DailyGacha,
    TicketShop,
    Coupon,
    Notice,
    Account,
    Reward,                 // ���� �ö���� ��� ���Ƽ� �� ��
}

public enum ESystemPopup
{
    //BuyItemTwoButton,
    OneButton,
    TwoButton,
    NoneTouch,
}

// ItemData���̺�
public enum EItem
{
    CashDia = 101,
    FreeDia,
    Ticket,
    //AddDia1,
    //AddDia3,
    //AddDia5,
    // AddDia9,

    AddMaxTime = 111,            // �κ� ������
    PermBombPower,
    MaxItem,
    SuperFeverStart,
    BonusScore,
    PermShield,             // Permanent Ȯ��

    BonusCharacter = 121,        // �ΰ��� ������
    TempBombPower,
    BombFullGauge,
    TimeCharge,
    PreventInterrupt,
    TempShield,         // Temporary
    SuperFever,

    ShopDia1 = 1001,
    ShopDia2,
    ShopDia3,
    ShopDia4,
    ShopDia5,

    WeelyPackage1 = 2001,

    AdItemGacha = 10001,
    AdTicket,
    AdLobbyItem,
    AdRevive,
    AdDoubleReward
}

// ���� �� ������
public enum ELobbyItem
{
    AddMaxTime,
    AddBombPower,
    MaxItem,
    SuperFeverStart,
    AddScore,
    Shield,                     // = PermShield
}

public enum EInGameItem
{
    AddDia1,
    AddDia3,
    AddDia5,
    AddDia9,
    AddBombPower,
    BonusCharacter,
    BombFullGauge,
    TimeCharge,
    PreventInterrupt,
    PreventInCorrect,               // = TempShield
    SuperFever,
}

public enum EGoods
{
    FreeDia,
    CashDia
}

public enum EDailyGift
{
    ItemGacha,
    Ticket,
    LobbyItem,
    Revive,                 // 0, 4
    DoubleReward,       // 0, 4
}

public enum EDock
{
    Shop,
    Rank,
    Home,
    Mail,
    Option
}

public enum EFever
{
    StartFever = 0,

    SuperFever = 1,
    UltraFever,
    HyperFever,
}

public enum EChart
{
    Item,
    LobbyItem,
    InGameItem,
    DailyGift,
    Shop,
    Serial,
}

public enum EPost
{
    Normal,
    Package,
    Ranking,
    Serial,
}

public enum ELogin
{ 
    Google,
    Facebook,
    Guest,
}

public enum EObstacle
{
    None = -1,
    Blur,
    Jumble,
    Reverse
}

public enum ELanguage
{
    English,        // Default
    Korean,
}

public enum EShopItem
{
    Dia,
    Weekly,
    DailyGift,
}

public enum ESFX
{
    Touch,
    Toggle,
    Dock,
    BuyItem,            // BuyDia ����
    Gacha,
    ReadyGo,
    Obstacle,
    Fever,
    GetItem,
    GetDia,
    SpawnBomb,
    UseItem,
    Shield,
    Clean,
    Countdown,
    GameOver,
    Result,
}