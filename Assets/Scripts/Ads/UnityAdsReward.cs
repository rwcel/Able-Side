using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Advertisements;
using System;

public class UnityAdsReward : MonoBehaviour, IUnityAdsLoadListener, IUnityAdsShowListener
{
    private string unitID;
    private Action eventReward = null;
    private Action eventClosed = null;

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

    public void Show(Action Callback, Action Closed)
    {
        eventReward = Callback;
        eventClosed = Closed;

        Advertisement.Show(unitID, this);

#if UNITY_EDITOR
        eventReward?.Invoke();
        eventClosed?.Invoke();

        Advertisement.Load(unitID, this);
#endif
    }

    public void OnUnityAdsShowClick(string placementId)
    {
        Debug.LogFormat("******* OnUnityAdsShowClick: {0}", placementId);
    }

    public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState showCompletionState)
    {
        Debug.LogFormat("******* OnUnityAdsShowComplete: {0}, {1}", placementId, showCompletionState.ToString());

        if (unitID.Equals(placementId) && showCompletionState.Equals(UnityAdsShowCompletionState.COMPLETED))
        {
            Debug.Log("******** Unity Ads Rewarded Ad Completed");

            // Grant a reward.
            eventReward?.Invoke();
            eventClosed?.Invoke();

            // Load another ad:
            Advertisement.Load(unitID, this);
        }
        else
        {
            Debug.Log("******** Unity Ads Complete But Not Reward");
            eventClosed?.Invoke();
        }
    }

    public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message)
    {
        Debug.LogFormat("****** OnUnityAdsShowFailure: {0}, {1}, {2}", placementId, error, message);
        eventClosed?.Invoke();
    }

    public void OnUnityAdsShowStart(string placementId)
    {
        Debug.LogFormat("******* OnUnityAdsShowStart: {0}", placementId);
    }
}
