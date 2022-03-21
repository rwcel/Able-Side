using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using UnityEngine.Audio;

public class AudioManager : Singleton<AudioManager>
{
    [Header("BGM")]
    [SerializeField] AudioClip[] lobbyBgmClips;
    [SerializeField] AudioClip[] ingameBgmClips;
    [SerializeField] AudioSource bgmSource;
    [SerializeField] AudioMixerGroup bgmGroup;
    private bool onoffBGM;
    public bool OnOffBGM => onoffBGM;
    private static readonly string _Key_SwitchBGM = "SwitchBGM";
    private bool isPlayLobby;

    [SerializeField] [Range(0f, 1f)]
    private float lobbyVolume;
    [SerializeField] [Range(0f, 1f)]
    private float ingameVolume;

    [Header("SFX")]
    [SerializeField] AudioClip[] sfxClips;
    [SerializeField] AudioClip[] comboClips;
    [SerializeField] Transform sfxParent;
    [SerializeField] GameObject sfxPrefab;
    [SerializeField] AudioMixerGroup sfxGroup;
    private List<AudioSource> sfxSources;
    private bool onoffSFX;
    public bool OnOffSFX => onoffSFX;
    private static readonly string _Key_SwitchSFX = "SwitchSFX";

    protected override void AwakeInstance()
    {
        //sfxSources = new List<AudioSource>(sfxParent.childCount);         // 가변 사이즈 생각해서 capacity 냅둠
        sfxSources = new List<AudioSource>();
        foreach (Transform child in sfxParent)
        {
            sfxSources.Add(child.GetComponent<AudioSource>());
        }

        var obj = FindObjectsOfType<AudioManager>();
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
        AddListeners();

        LoadData();

        PlayLobbyBGM();
    }

    private void AddListeners()
    {
        this.ObserveEveryValueChanged(_ => onoffBGM)
            .Skip(System.TimeSpan.Zero)
            .Subscribe(value => SaveBGM(value))
            .AddTo(this.gameObject);

        this.ObserveEveryValueChanged(_ => onoffSFX)
            .Skip(System.TimeSpan.Zero)
            .Subscribe(value => SaveSFX(value))
            .AddTo(this.gameObject);
    }

    private void LoadData()
    {
        // PlayerPrefs
        onoffBGM = PlayerPrefs.HasKey(_Key_SwitchBGM)
                            ? PlayerPrefs.GetInt(_Key_SwitchBGM) == 0 ? false : true
                            : true;
        bgmGroup.audioMixer.SetFloat("BGM", onoffBGM ? 0 : -80);

        onoffSFX = PlayerPrefs.HasKey(_Key_SwitchSFX)
                            ? PlayerPrefs.GetInt(_Key_SwitchSFX) == 0 ? false : true
                            : true;
        sfxGroup.audioMixer.SetFloat("SFX", onoffSFX ? 0 : -80);
    }

    /// <summary>
    /// **On하면 처음부터 실행하는게 맞는지
    /// </summary>
    public bool SwitchBGM()
    {
        onoffBGM = !onoffBGM;
        bgmGroup.audioMixer.SetFloat("BGM", onoffBGM ? 0 : -80);
        //bgmMixer.SetFloat();

        return onoffBGM;
    }

    public bool SwitchSFX()
    {
        onoffSFX = !onoffSFX;
        sfxGroup.audioMixer.SetFloat("SFX", onoffSFX ? 0 : -80);
        //sfxMixer.SetFloat("SFX", onoffSFX ? 0 : -80);

        return onoffSFX;
    }

    private void SaveBGM(bool onoff)
    {
        PlayerPrefs.SetInt(_Key_SwitchBGM, onoff ? 1 : 0);
    }

    private void SaveSFX(bool onoff)
    {
        PlayerPrefs.SetInt(_Key_SwitchSFX, onoff ? 1 : 0);
    }

    public void PlayLobbyBGM()
    {
        if (isPlayLobby)
            return;

        int rand = Random.Range(0, lobbyBgmClips.Length);
        bgmSource.clip = lobbyBgmClips[rand];
        bgmSource.Play();

        bgmSource.volume = lobbyVolume;      // **지우기

        isPlayLobby = true;
    }

    public void PlayInGameBGM()
    {
        int rand = Random.Range(0, ingameBgmClips.Length);
        bgmSource.clip = ingameBgmClips[rand];
        bgmSource.Play();

        bgmSource.volume = ingameVolume;      // **지우기

        isPlayLobby = false;
    }

    public void StopBGM()
    {
        bgmSource.Stop();
    }

    public void PauseBGM(bool isPause)
    {
        if (isPause)
            bgmSource.Pause();
        else
            bgmSource.UnPause();
    }

    public void PlaySFX(ESFX sfx)
    {
        GetSfxSource().PlayOneShot(sfxClips[(int)sfx]);
        // sfxSources[0].
    }

    public void PlayComboSFX(int combo)
    {
        GetSfxSource().PlayOneShot(comboClips[Mathf.Clamp(combo, 0, comboClips.Length - 1)]);
        // sfxSources[0].clip = comboClips[Mathf.Clamp(combo, 0, comboClips.Length)];
    }

    private AudioSource GetSfxSource()
    {
        foreach (var sfxSource in sfxSources)
        {
            if (!sfxSource.isPlaying)
                return sfxSource;
        }

        return CreateSFX();
    }

    private AudioSource CreateSFX()
    {
        var newSFX = Instantiate(sfxPrefab, sfxParent).GetComponent<AudioSource>();
        sfxSources.Add(newSFX);

        return newSFX;
    }
}
