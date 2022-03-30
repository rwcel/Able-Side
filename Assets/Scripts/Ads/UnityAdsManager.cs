using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// using Utility;
using UnityEngine.Advertisements;

public class UnityAdsManager : Singleton<UnityAdsManager>
{
    public UnityAdsBanner bannerAd;
    public UnityAdsInterstitial interstitialAD;
    public UnityAdsReward itemGachaRewardAD;
    public UnityAdsReward ticketRewardAD;
    public UnityAdsReward lobbyItemRewardAD;
    public UnityAdsReward reviveRewardAD;
    public UnityAdsReward doubleRewardAD;
    public UnityAdsReward timeRewardAD;

    private string unitID = "";
    private string banner_unitID = "Banner_Android";
    private string interstitial_unitID = "Interstitial_Android";
    private string reward_itemGacha_unitID = "Reward_ItemGacha_Android";            // 아이템 얻는 광고
    private string reward_ticket_unitID = "Reward_Ticket_Android";                  // 아이템 얻는 광고
    private string reward_lobbyItem_unitID = "Reward_LobbyItem_Android";            // 아이템 얻는 광고
    private string reward_revive_unitID = "Reward_Revive_Android";           // 게임 더하기
    private string reward_double_unitID = "Reward_Double_Android";           // 2배 보상
    private string reward_time_unitID = "Reward_Time_Android";           // 버스

    public string AdBannerID { get { return banner_unitID; } }
    public string AdInterstitialID { get { return interstitial_unitID; } }
    public string AdItemGachaRewardID { get { return reward_itemGacha_unitID; } }
    public string AdTicketRewardID { get { return reward_ticket_unitID; } }
    public string AdLobbyItemRewardID { get { return reward_lobbyItem_unitID; } }
    public string AdReviveRewardID { get { return reward_revive_unitID; } }
    public string AdDoubleRewardID { get { return reward_double_unitID; } }
    public string AdTimeRewardID { get { return reward_time_unitID; } }

    protected override void AwakeInstance()
    {
    }

    protected override void DestroyInstance() { }

    void Start()
    {
#if UNITY_ANDROID
        unitID = "4641683";
#elif UNITY_IOS
        unitID = "4641682";
#endif
        Advertisement.Initialize(unitID);
        StartAds();
    }

    public void StartAds()
    {
        bannerAd.Load(banner_unitID);
        interstitialAD.Load(interstitial_unitID);
        itemGachaRewardAD.Load(reward_itemGacha_unitID);
        ticketRewardAD.Load(reward_ticket_unitID);
        lobbyItemRewardAD.Load(reward_lobbyItem_unitID);
        reviveRewardAD.Load(reward_revive_unitID);
        doubleRewardAD.Load(reward_double_unitID);
        timeRewardAD.Load(reward_time_unitID);
    }

    public void ShowBannerAD()
    {
        bannerAd.Show();
    }

    public void ShowInterstitialAD()
    {
        interstitialAD.Show();
    }

    public void ShowRewardAD(System.Action Callback, System.Action Closed, EDailyGift adType)
    {
        Debug.Log($"리워드 광고 : {adType}");

        switch (adType)
        {
            case EDailyGift.ItemGacha:
                itemGachaRewardAD.Show(Callback, Closed);
                break;
            case EDailyGift.Ticket:
                ticketRewardAD.Show(Callback, Closed);
                break;
            case EDailyGift.LobbyItem:
                lobbyItemRewardAD.Show(Callback, Closed);
                break;
            case EDailyGift.Revive:
                reviveRewardAD.Show(Callback, Closed);
                break;
            case EDailyGift.DoubleReward:
                doubleRewardAD.Show(Callback, Closed);
                break;
            case EDailyGift.TimeReward:
                timeRewardAD.Show(Callback, Closed);
                break;
        }
    }
}
