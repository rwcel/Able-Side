using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Advertisements;

public class UnityAdsBanner : MonoBehaviour
{
    [SerializeField] BannerPosition bannerPosition = BannerPosition.BOTTOM_CENTER;
    private string bannerID;

    public void Load(string unitID)
    {
        bannerID = unitID;
        Advertisement.Banner.SetPosition(bannerPosition);

        BannerLoadOptions options = new BannerLoadOptions
        {
            loadCallback = OnBannerLoaded,
            errorCallback = OnBannerError
        };
        Advertisement.Banner.Load(bannerID, options);
    }

    private void OnBannerLoaded()
    {
        Debug.Log("Banner Loaded");
        // Show();
    }

    private void OnBannerError(string msg)
    {
        Debug.Log($"Banner Error : {msg}");
    }

    public void Show()
    {
        BannerOptions options = new BannerOptions
        {
            clickCallback = OnBannerClicked,
            hideCallback = OnBannerHidden,
            showCallback = OnBannerShown
        };

        Debug.Log($"Show Banner {bannerID}");
        Advertisement.Banner.Show(bannerID, options);
    }

    void OnBannerClicked() { }
    void OnBannerShown() { }
    void OnBannerHidden() { }

    public void Hide()
    {
        Debug.Log("Hide");
        Advertisement.Banner.Hide();
    }
}
