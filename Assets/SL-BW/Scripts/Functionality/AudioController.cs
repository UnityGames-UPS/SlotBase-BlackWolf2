using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
internal class AudioController : MonoBehaviour
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgMusicSource;
    [SerializeField] private AudioSource gameSoundSource;
    // [SerializeField] private AudioSource uiSource;

    [Header("Background")]
    [SerializeField] private AudioClip bgMusic;
    [SerializeField] private AudioClip bonusbgMusic;

    [Header("Game Sounds")]
    [SerializeField] private AudioClip anotherWin;
    [SerializeField] private AudioClip normalWin;
    [SerializeField] private AudioClip betButton;
    [SerializeField] private AudioClip WolfAppear;
    [SerializeField] private AudioClip BonusWin;
    [SerializeField] private AudioClip BoostWin;
    [SerializeField] private AudioClip MoonIconPop;
    [SerializeField] private AudioClip LightSound;
    [SerializeField] private AudioClip SpinStarts;
    [SerializeField] private AudioClip ReelHit;
    //[SerializeField] private AudioClip SpinStops;

    // [Header("UI Sounds")]
    // [SerializeField] private AudioClip uiButton;

    // [Header("Sound Buttons")]
    // [SerializeField] private Button SoundButton;
    // [SerializeField] private Button SoundMuteButton;
    // [SerializeField] private Button MusicButton;
    // [SerializeField] private Button MusicMuteButton;

    private bool isGameMuted = false;
    private bool isMusicMuted = false;

    private void Start()
    {
        // if (SoundButton)
        // {
        //     SoundButton.onClick.RemoveAllListeners();
        //     SoundButton.onClick.AddListener(ToggleGameSound);
        // }

        // if (MusicButton)
        // {
        //     MusicButton.onClick.RemoveAllListeners();
        //     MusicButton.onClick.AddListener(ToggleBackgroundMusic);
        // }

        // if (SoundMuteButton)
        // {
        //     SoundMuteButton.onClick.RemoveAllListeners();
        //     SoundMuteButton.onClick.AddListener(ToggleGameSound);
        // }

        // if (MusicMuteButton)
        // {
        //     MusicMuteButton.onClick.RemoveAllListeners();
        //     MusicMuteButton.onClick.AddListener(ToggleBackgroundMusic);
        // }

        PlayBackground();
    }

    // private void ToggleGameSound()
    // {
    //     Debug.Log("button pressed!");
    //     if (!isGameMuted)
    //     {
    //         SoundMuteButton.gameObject.SetActive(true);
    //         SoundButton.gameObject.SetActive(false);
    //     }
    //     else
    //     {
    //         SoundButton.gameObject.SetActive(true);
    //         SoundMuteButton.gameObject.SetActive(false);
    //     }
    //     isGameMuted = !isGameMuted;
    //     MuteGame(isGameMuted);
    // }

    // private void ToggleBackgroundMusic()
    // {
    //     if (!isMusicMuted)
    //     {
    //         MusicMuteButton.gameObject.SetActive(true);
    //         MusicButton.gameObject.SetActive(false);
    //     }
    //     else
    //     {
    //         MusicButton.gameObject.SetActive(true);
    //         MusicMuteButton.gameObject.SetActive(false);
    //     }
    //     isMusicMuted = !isMusicMuted;
    //     MuteBackground(isMusicMuted);
    // }


    internal void PlayBackground()
    {
        if (!bgMusic) return;

        bgMusicSource.clip = bgMusic;
        bgMusicSource.loop = true;
        if (!bgMusicSource.isPlaying)
            bgMusicSource.Play();
    }

    internal void PlayBonusBackground()
    {
        if (!bonusbgMusic) return;

        bgMusicSource.clip = bonusbgMusic;
        bgMusicSource.loop = true;
        if (!bgMusicSource.isPlaying)
            bgMusicSource.Play();
    }

    internal void StopBackground()
    {
        bgMusicSource.Stop();
    }

    internal void PlayAnotherWin()
    {
        PlayGame(anotherWin, false);
    }

    internal void PlayNormalWin()
    {
        PlayGame(normalWin, false);
    }

    internal void PlayBetButton()
    {
        PlayGame(betButton, false);
    }
    internal void PlayWolfAppear()
    {
        PlayGame(WolfAppear, false);
    }

    internal void PlaySpinStarts()
    {
        PlayGame(SpinStarts, false);
    }
    internal void PlayReelHit()
    {
        PlayGame(ReelHit, false);
    }
    internal void PlayLightSound()
    {
        PlayGame(LightSound, false);
    }
    internal void PlayBonusWin()
    {
        PlayGame(BonusWin, false);
    }
    internal void PlayBoostWin()
    {
        PlayGame(BoostWin, false);
    }
    internal void PlayMoonIconPop()
    {
        PlayGame(MoonIconPop, false);
    }

    private void PlayGame(AudioClip clip, bool loop)
    {
        if (!clip) return;

        gameSoundSource.Stop();
        gameSoundSource.clip = clip;
        gameSoundSource.loop = loop;
        gameSoundSource.Play();
    }

    internal void StopGameAudio()
    {
        gameSoundSource.Stop();
        gameSoundSource.loop = false;
    }

    // internal void PlayUIButton()
    // {
    //     gameSoundSource.PlayOneShot(uiButton);
    // }

    private bool isForceMuted = false;
    private bool preFocusMusicMuted;
    private bool preFocusGameMuted;

    // Focus-driven — called from BOTH UIManager.OnFocusChanged (JS bridge) and OnApplicationFocus below.
    internal void SetMuteAll(bool forceMute)
    {
        if (forceMute == isForceMuted) return;
        isForceMuted = forceMute;

        if (forceMute)
        {
            preFocusMusicMuted = bgMusicSource.mute;
            preFocusGameMuted = gameSoundSource.mute;
            bgMusicSource.mute = true;
            gameSoundSource.mute = true;
        }
        else
        {
            bgMusicSource.mute = preFocusMusicMuted;
            gameSoundSource.mute = preFocusGameMuted;
        }
    }


    internal void MuteBackground(bool mute) => bgMusicSource.mute = mute;
    internal void MuteGame(bool mute) => gameSoundSource.mute = mute;
    // internal void MuteUI(bool mute) => uiSource.mute = mute;

    // Native/editor focus path — calls the SAME method the WebGL OnFocusChanged path calls.
    private void OnApplicationFocus(bool hasFocus)
    {
        SetMuteAll(!hasFocus);
    }
}
