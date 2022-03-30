using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Advertisements;
using System;

public class UnityAdsInterstitial : MonoBehaviour, IUnityAdsLoadListener, IUnityAdsShowListener
{
    private string unitID;
    //private Action eventReward = null;
    //private Action eventClosed = null;

    public void Load(string unitID)
    {
        this.unitID = unitID;
        Advertisement.Load(unitID, this);
    }

    public void OnUnityAdsAdLoaded(string placementId)
    {
        Debug.LogFormat("***** OnUnityAdsAdLoaded: {0}", placementId);
    }

    public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message)
    {
        Debug.LogFormat("******* OnUnityAdsFailedToLoad: {0}, {1}, {2}", placementId, error, message);
    }

    /// <summary>
    /// **이벤트가 굳이 필요한지에 대해 생각
    /// </summary>
    public void Show()
    {
        Advertisement.Show(unitID, this);
    }

    public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message)
    {
        Debug.LogFormat("OnUnityAdsShowFailure: {0} {1} {2}", placementId, error, message);
    }

    public void OnUnityAdsShowStart(string placementId)
    {
        Debug.LogFormat("OnUnityAdsShowStart: {0}", placementId);
    }

    public void OnUnityAdsShowClick(string placementId)
    {
        Debug.LogFormat("OnUnityAdsShowClick: {0}", placementId);
    }

    public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState showCompletionState)
    {
        Debug.LogFormat("OnUnityAdsShowComplete: {0}, {1}", placementId, showCompletionState.ToString());
    }
}
