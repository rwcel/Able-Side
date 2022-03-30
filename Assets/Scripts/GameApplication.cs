using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;
using Gpm.WebView;
using UniRx;
using UniRx.Triggers;

public class GameApplication : Singleton<GameApplication>
{
    public bool IsTestMode;

    private int version;
    private float deltaTime = 0.0f;


    protected override void AwakeInstance()
    {
        //GarbageCollector.GCMode = GarbageCollector.Mode.Enabled;

        // 디바이스 로그 표시?
#if UNITY_EDITOR
        Debug.unityLogger.logEnabled = true;
#else
        if(!IsTestMode)
        {
            Debug.unityLogger.logEnabled = false;
        }
#endif

        var obj = FindObjectsOfType<GameApplication>();
        if (obj.Length == 1)
            DontDestroyOnLoad(gameObject);
        else
        {
            Destroy(gameObject);
        }
    }

    protected override void DestroyInstance() { }

    private void Start()
    {
#if UNITY_EDITOR
        Application.runInBackground = true;
#endif

        Caching.compressionEnabled = false;

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        // Time.fixedDeltaTime = 

        //var clickStream = this.UpdateAsObservable().Where(_ => Input.GetKeyDown(KeyCode.Escape));

        //clickStream
        //    .Buffer(clickStream.Throttle(System.TimeSpan.FromSeconds(1)))
        //    .Where(x => x.Count >= 2)
        //    .Subscribe(_ => QuitMessage());
    }

    private void Update()
    {
        if (IsTestMode)
        {
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            QuitMessage();
        }
    }

    private void OnGUI()
    {
        if(IsTestMode)
        {
            var w = Screen.width;
            var h = Screen.height;
            var style = new GUIStyle();

            var rect = new Rect(0, 0, w, h * 2 / 100);
            style.alignment = TextAnchor.UpperLeft;
            style.fontSize = 36;
            style.normal.textColor = Color.black;

            float msec = deltaTime * 1000.0f;
            float fps = 1.0f / deltaTime;
            string text = string.Format("{0:0.0}ms({1:0.}fps)", msec, fps);
            GUI.Label(rect, text, style);
        }
    }

    public void QuitMessage()
    {
        SystemPopupUI.Instance.OpenTwoButton(15, 115, 2, 1, Quit, null);
    }

    public void Quit()
    {
        StartCoroutine(nameof(CoQuitGame));
    }

    IEnumerator CoQuitGame()
    {
        yield return Values.Delay1;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }


    #region Webview

    public void ShowWebView(string titleName, string url)
    {
        AudioManager.Instance.PlaySFX(ESFX.Touch);

        GpmWebView.ShowUrl(url,
            new GpmWebViewRequest.Configuration()
            {
                style = GpmWebViewStyle.FULLSCREEN,
                isClearCookie = false,
                isClearCache = false,
                isNavigationBarVisible = true,
                title = titleName,
                isBackButtonVisible = true,
                isForwardButtonVisible = true,
#if UNITY_IOS
                contentMode = GpmWebViewContentMode.MOBILE
#elif UNITY_ANDROID
                supportMultipleWindows = true
#endif
            },
            OnOpenCallback,
            OnCloseCallback,
            new List<string>()
            {
                "USER_CUSTOM_SCHEME"
            },
            OnSchemeEvent);
    }

    private void OnOpenCallback(GpmWebViewError error)
    {
        if (error == null)
        {
            Debug.Log("[OnOpenCallback] succeeded.");
        }
        else
        {
            Debug.Log(string.Format("[OnOpenCallback] failed. error:{0}", error));
        }
    }

    private void OnCloseCallback(GpmWebViewError error)
    {
        if (error == null)
        {
            Debug.Log("[OnCloseCallback] succeeded.");
        }
        else
        {
            Debug.Log(string.Format("[OnCloseCallback] failed. error:{0}", error));
        }
    }

    private void OnSchemeEvent(string data, GpmWebViewError error)
    {
        if (error == null)
        {
            Debug.Log("[OnSchemeEvent] succeeded.");

            if (data.Equals("USER_ CUSTOM_SCHEME") == true || data.Contains("CUSTOM_SCHEME") == true)
            {
                Debug.Log(string.Format("scheme:{0}", data));
            }
        }
        else
        {
            Debug.Log(string.Format("[OnSchemeEvent] failed. error:{0}", error));
        }
    }

    #endregion Webview
}
