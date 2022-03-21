using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Advertisements;

public class UnityAdsBanner : MonoBehaviour
{
    [SerializeField] BannerPosition bannerPosition = BannerPosition.BOTTOM_CENTER;
    private string bannerID;

    private bool isBannerShowed;            // **로비에서 켜주기때문에 문제가 발생할 수 있다

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
        Debug.Log("****** Banner Loaded");
        // Show();
    }

    private void OnBannerError(string msg)
    {
        Debug.Log($"********** Banner Error : {msg}");
    }

    public void Show()
    {
        BannerOptions options = new BannerOptions
        {
            clickCallback = OnBannerClicked,
            hideCallback = OnBannerHidden,
            showCallback = OnBannerShown
        };

        Debug.Log($"********** Show Banner {bannerID}");
        isBannerShowed = true;
        Advertisement.Banner.Show(bannerID, options);
    }

    void OnBannerClicked() { }
    void OnBannerShown() { }
    void OnBannerHidden() { }

    public void Hide()
    {
        Debug.Log("********* Hide");
        Advertisement.Banner.Hide();
        isBannerShowed = false;
    }
}
