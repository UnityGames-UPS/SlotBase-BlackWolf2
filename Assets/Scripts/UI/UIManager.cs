using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Text;

public class UIManager : MonoBehaviour
{
    [Header("Slot UI")]
    [SerializeField] private Button SpinButton;
    [SerializeField] private Button StopSpinButton;
    [SerializeField] private Button AutoSpinButton;

    [Header("Main UI Text")]
    [SerializeField] private TMP_Text Balance_Text;
    [SerializeField] private TMP_Text WinAmount_Text;
    [SerializeField] private TMP_Text Bet_Text;
    [SerializeField] private Button BetPlus_Button;
    [SerializeField] private Button BetMinus_Button;

    [SerializeField] private GameObject FreeSpinCountPanel;
    [SerializeField] private TMP_Text FreeSpinText;

    [Header("Bonus Text")]
    [SerializeField] private TMP_Text GrandText;
    [SerializeField] private TMP_Text MajorText;
    [SerializeField] private TMP_Text MinorText;
    [SerializeField] private TMP_Text MiniText;

    [Header("Bg UI Reference")]
    [SerializeField] private Image Bg_Image;
    [SerializeField] private Sprite Day_Sprite;
    [SerializeField] private Sprite Night_Sprite;
    [SerializeField] private Sprite Bonus_Sprite;

    [Header("Main Popus UI Object")]
    [SerializeField] private GameObject MainPopup_Object;

    [Header("Paytable Popup UI References")]
    [SerializeField] private GameObject[] Pages;
    [SerializeField] private Button Paytable_Button;
    [SerializeField] private GameObject PaytablePopup_Object;
    [SerializeField] private Button PaytableExit_Button;
    [SerializeField] private Button PaytableLeft_Button;
    [SerializeField] private Button PaytableRight_Button;

    [Header("Sound/Music UI References")]
    [SerializeField] private Button Sound_Button;
    [SerializeField] private Button Music_Button;
    [SerializeField] private Sprite SoundOff_Sprite;
    [SerializeField] private Sprite SoundOn_Sprite;
    [SerializeField] private Sprite MusicOff_Sprite;
    [SerializeField] private Sprite MusicOn_Sprite;

    [Header("Win Popup UI References")]
    [SerializeField] private Image BigWin_Image;
    [SerializeField] private Image HugeWin_Image;
    [SerializeField] private Image MegaWin_Image;
    [SerializeField] private Image DoublePay_Image;
    [SerializeField] private GameObject WinPopup_Object;
    [SerializeField] private Transform WinPopupParent;
    [SerializeField] private TMP_Text Win_Text;
    [SerializeField] private Button SkipWinAnimation;
    private Image Win_Image;

    [Header("Disconnection Popup UI References")]
    [SerializeField] private Button CloseDisconnect_Button;
    [SerializeField] private GameObject DisconnectPopup_Object;

    [Header("Reconnection Popup")]
    [SerializeField] private GameObject ReconnectPopup_Object;

    [Header("AnotherDevice Popup UI References")]
    [SerializeField] private Button CloseAD_Button;
    [SerializeField] private GameObject ADPopup_Object;

    [Header("LowBalance Popup UI References")]
    [SerializeField] private Button LBExit_Button;
    [SerializeField] private GameObject LBPopup_Object;

    [Header("Quit Popup UI References")]
    [SerializeField] private GameObject QuitPopup_Object;
    [SerializeField] private Button YesQuit_Button;
    [SerializeField] private Button NoQuit_Button;
    [SerializeField] private Button CrossQuit_Button;
    [SerializeField] private Button GameExit_Button;

    [Header("Managers")]
    [SerializeField] private AudioController _audioController;
    [SerializeField] private SocketIOManager _socketManager;
    [SerializeField] private SlotManager _slotManager;

    private double currentTotalBet = 0;
    internal int betCounter = 0;
    private bool isMusic = true;
    private bool isSound = true;
    private bool isExit = false;
    private Tween WinPopupTextTween;
    private Tween ClosePopupTween;
    private Tween WinTextScaleTween;
    private Tween WinImageScaleTween;

    internal int FreeSpins;
    private int paytablePageCounter;

    // ─────────────────────────────────────────────────────────────────────────
    // Sprite-text helper
    // Sprite asset index mapping:
    //   0-9  → digit characters '0'-'9'
    //   10   → decimal point '.'
    //   11   → comma ','
    //   12   → plus '+'
    // Any unrecognised character is silently skipped.
    // ─────────────────────────────────────────────────────────────────────────
    private static string ToSpriteString(string value)
    {
        var sb = new StringBuilder();
        foreach (char c in value)
        {
            if (c >= '0' && c <= '9')
                sb.Append($"<sprite index={(c - '0')}>");
            else if (c == '.')
                sb.Append("<sprite index=10>");
            else if (c == ',')
                sb.Append("<sprite index=11>");
            else if (c == '+')
                sb.Append("<sprite index=12>");
            // skip any other characters (e.g. '-', letters)
        }
        return sb.ToString();
    }

    // Convenience overloads so call-sites stay clean
    private static string ToSpriteString(double value, string format = "F2")
        => ToSpriteString(value.ToString(format));

    private static string ToSpriteString(int value)
        => ToSpriteString(value.ToString());

    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        if (SpinButton) SpinButton.onClick.AddListener(OnSpinButtonPressed);
        if (AutoSpinButton) AutoSpinButton.onClick.AddListener(OnAutoSpinButtonPressed);
        if (StopSpinButton) StopSpinButton.onClick.AddListener(OnStopSpinButtonPressed);

        if (BetPlus_Button) BetPlus_Button.onClick.AddListener(delegate { ChangeBet(true); });
        if (BetMinus_Button) BetMinus_Button.onClick.AddListener(delegate { ChangeBet(false); });

        isMusic = true;
        isSound = true;

        if (_audioController) _audioController.ToggleMute(false);

        if (Paytable_Button) Paytable_Button.onClick.RemoveAllListeners();
        if (Paytable_Button) Paytable_Button.onClick.AddListener(delegate { OpenPaytable(); });

        if (PaytableExit_Button) PaytableExit_Button.onClick.RemoveAllListeners();
        if (PaytableExit_Button) PaytableExit_Button.onClick.AddListener(delegate { ClosePopup(PaytablePopup_Object); });

        if (PaytableLeft_Button) PaytableLeft_Button.onClick.RemoveAllListeners();
        if (PaytableLeft_Button) PaytableLeft_Button.onClick.AddListener(() => { SwitchPages(false); });

        if (PaytableRight_Button) PaytableRight_Button.onClick.RemoveAllListeners();
        if (PaytableRight_Button) PaytableRight_Button.onClick.AddListener(() => { SwitchPages(true); });

        if (GameExit_Button) GameExit_Button.onClick.RemoveAllListeners();
        if (GameExit_Button) GameExit_Button.onClick.AddListener(delegate { OpenPopup(QuitPopup_Object); });

        if (NoQuit_Button) NoQuit_Button.onClick.RemoveAllListeners();
        if (NoQuit_Button) NoQuit_Button.onClick.AddListener(delegate { if (!isExit) ClosePopup(QuitPopup_Object); });

        if (CrossQuit_Button) CrossQuit_Button.onClick.RemoveAllListeners();
        if (CrossQuit_Button) CrossQuit_Button.onClick.AddListener(delegate { if (!isExit) ClosePopup(QuitPopup_Object); });

        if (LBExit_Button) LBExit_Button.onClick.RemoveAllListeners();
        if (LBExit_Button) LBExit_Button.onClick.AddListener(delegate { ClosePopup(LBPopup_Object); });

        if (YesQuit_Button) YesQuit_Button.onClick.RemoveAllListeners();
        if (YesQuit_Button) YesQuit_Button.onClick.AddListener(delegate { CallOnExitFunction(); });

        if (CloseDisconnect_Button) CloseDisconnect_Button.onClick.RemoveAllListeners();
        if (CloseDisconnect_Button) CloseDisconnect_Button.onClick.AddListener(CallOnExitFunction);

        if (CloseAD_Button) CloseAD_Button.onClick.RemoveAllListeners();
        if (CloseAD_Button) CloseAD_Button.onClick.AddListener(CallOnExitFunction);

        if (Sound_Button) Sound_Button.onClick.RemoveAllListeners();
        if (Sound_Button) Sound_Button.onClick.AddListener(ToggleSound);

        if (Music_Button) Music_Button.onClick.RemoveAllListeners();
        if (Music_Button) Music_Button.onClick.AddListener(ToggleMusic);

        if (SkipWinAnimation) SkipWinAnimation.onClick.RemoveAllListeners();
        if (SkipWinAnimation) SkipWinAnimation.onClick.AddListener(SkipWin);
    }

    private void OpenPaytable()
    {
        _audioController.PlayButtonAudio();
        foreach (GameObject gameObject in Pages)
            gameObject.SetActive(false);
        paytablePageCounter = 0;
        Pages[0].SetActive(true);
        MainPopup_Object.SetActive(true);
        PaytablePopup_Object.SetActive(true);
    }

    private void SwitchPages(bool IncDec)
    {
        _audioController.PlayButtonAudio();
        if (IncDec)
        {
            paytablePageCounter++;
            if (paytablePageCounter == Pages.Length) paytablePageCounter = 0;
        }
        else
        {
            paytablePageCounter--;
            if (paytablePageCounter == -1) paytablePageCounter = Pages.Length - 1;
        }
        foreach (GameObject gameObject in Pages)
            gameObject.SetActive(false);
        Pages[paytablePageCounter].SetActive(true);
    }

    internal void LowBalPopup()
    {
        OpenPopup(LBPopup_Object);
    }

    internal void DisconnectionPopup()
    {
        if (!isExit)
        {
            isExit = true;
            OpenPopup(DisconnectPopup_Object);
        }
    }

    internal void ReconnectionPopup()
    {
        OpenPopup(ReconnectPopup_Object);
    }

    internal void PopulateWin(int value, double amount)
    {
        switch (value)
        {
            case 1: Win_Image = BigWin_Image;   break;
            case 2: Win_Image = HugeWin_Image;  break;
            case 3: Win_Image = MegaWin_Image;  break;
            case 4: Win_Image = DoublePay_Image; break;
        }
        _audioController.PlayWLAudio("megaWin");
        StartPopupAnim(amount);
    }

    void SkipWin()
    {
        Debug.Log("Skip win called");
        if (ClosePopupTween    != null) { ClosePopupTween.Kill();    ClosePopupTween    = null; }
        if (WinPopupTextTween  != null) { WinPopupTextTween.Kill();  WinPopupTextTween  = null; }
        if (WinImageScaleTween != null) { WinImageScaleTween.Kill(); WinImageScaleTween = null; }
        if (WinTextScaleTween  != null) { WinTextScaleTween.Kill();  WinTextScaleTween  = null; }
        EndPopupAnim();
    }

    private void StartPopupAnim(double amount)
    {
        double initAmount = 0;
        WinPopupParent.localScale = Vector3.zero;

        if (Win_Image) Win_Image.gameObject.SetActive(true);
        if (WinPopup_Object) WinPopup_Object.SetActive(true);

        WinImageScaleTween = WinPopupParent.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);

        WinPopupTextTween = DOTween.To(() => initAmount, (val) => initAmount = val, amount, 1.5f)
            .OnUpdate(() =>
            {
                // Win popup text also rendered as sprites
                if (Win_Text) Win_Text.text = ToSpriteString(initAmount, "F3");
            });

        ClosePopupTween = DOVirtual.DelayedCall(2f, () => { SkipWin(); });
    }

    void EndPopupAnim()
    {
        WinPopupParent.DOScale(Vector3.zero, 0.5f)
            .SetEase(Ease.InBack)
            .OnComplete(() =>
            {
                if (Win_Image) Win_Image.gameObject.SetActive(false);
                Win_Image = null;
                if (WinPopup_Object) WinPopup_Object.SetActive(false);
                _slotManager._checkPopups = false;
            });
    }

    internal void ADfunction()
    {
        OpenPopup(ADPopup_Object);
    }

    // ── Called once on init and again after reconnect ────────────────────────
    internal void InitialiseUIData(Root root)
    {
        UpdateBalance(root.player.balance);
        PopulateBonusInfo(root.features);
        UpdateWin(0.00);
        if (Bet_Text) Bet_Text.text = ToSpriteString(root.gameData.bets[betCounter]);
    }

    // ── Jackpot values (integers from backend) ───────────────────────────────
    private void PopulateBonusInfo(Features features)
    {
        if (GrandText) GrandText.text = ToSpriteString(features.jackpots.grand);
        if (MajorText) MajorText.text = ToSpriteString(features.jackpots.major);
        if (MinorText) MinorText.text = ToSpriteString(features.jackpots.minor);
        if (MiniText)  MiniText.text  = ToSpriteString(features.jackpots.mini);
    }

    // ── Balance — two decimal places (e.g. 1234.56) ─────────────────────────
    internal void UpdateBalance(double balance)
    {
        if (Balance_Text) Balance_Text.text = ToSpriteString(balance, "F2");
    }

    // ── Win amount — two decimal places ──────────────────────────────────────
    internal void UpdateWin(double winAmount)
    {
        if (WinAmount_Text) WinAmount_Text.text = ToSpriteString(winAmount, "F2");
    }

    // ── Bet change ────────────────────────────────────────────────────────────
    private void ChangeBet(bool IncDec)
    {
        if (_audioController) _audioController.PlayButtonAudio();
        if (IncDec)
        {
            betCounter++;
            if (betCounter >= _socketManager.initialData.bets.Count) betCounter = 0;
        }
        else
        {
            betCounter--;
            if (betCounter < 0) betCounter = _socketManager.initialData.bets.Count - 1;
        }
        if (Bet_Text) Bet_Text.text = ToSpriteString(_socketManager.initialData.bets[betCounter]);
        currentTotalBet = _socketManager.initialData.bets[betCounter];
    }

    private void CallOnExitFunction()
    {
        isExit = true;
        _audioController.PlayButtonAudio();
        StartCoroutine(_socketManager.CloseSocket());
    }

    private void OpenPopup(GameObject Popup)
    {
        if (_audioController) _audioController.PlayButtonAudio();
        if (Popup) Popup.SetActive(true);
        if (MainPopup_Object) MainPopup_Object.SetActive(true);
    }

    private void ClosePopup(GameObject Popup)
    {
        if (_audioController) _audioController.PlayButtonAudio();
        if (Popup) Popup.SetActive(false);
        if (!DisconnectPopup_Object.activeSelf)
            if (MainPopup_Object) MainPopup_Object.SetActive(false);
    }

    internal void CheckAndClosePopups()
    {
        if (ReconnectPopup_Object.activeInHierarchy) ClosePopup(ReconnectPopup_Object);
        if (DisconnectPopup_Object.activeInHierarchy) ClosePopup(DisconnectPopup_Object);
    }

    private void ToggleMusic()
    {
        _audioController.PlayButtonAudio();
        if (isMusic)
        {
            Music_Button.image.sprite = MusicOff_Sprite;
            _audioController.ToggleMute(true, "bg");
            isMusic = false;
        }
        else
        {
            Music_Button.image.sprite = MusicOn_Sprite;
            _audioController.ToggleMute(false, "bg");
            isMusic = true;
        }
    }

    private void ToggleSound()
    {
        _audioController.PlayButtonAudio();
        if (isSound)
        {
            Sound_Button.image.sprite = SoundOff_Sprite;
            if (_audioController) _audioController.ToggleMute(true, "button");
            if (_audioController) _audioController.ToggleMute(true, "wl");
            isSound = false;
        }
        else
        {
            Sound_Button.image.sprite = SoundOn_Sprite;
            if (_audioController) _audioController.ToggleMute(false, "button");
            if (_audioController) _audioController.ToggleMute(false, "wl");
            isSound = true;
        }
    }

    private void OnSpinButtonPressed()
    {
        _slotManager.StartSlots();
        StopSpinButton.gameObject.SetActive(true);
        SpinButton.gameObject.SetActive(false);
    }

    internal void OnStopSpinButtonPressed()
    {
        SpinButton.gameObject.SetActive(true);
        StopSpinButton.gameObject.SetActive(false);
    }

    private void OnAutoSpinButtonPressed()
    {
        if (!_slotManager._isAutoSpin)
        {
            _slotManager.AutoSpin();
            StopSpinButton.gameObject.SetActive(true);
            SpinButton.gameObject.SetActive(false);
        }
        else
        {
            _slotManager.StopAutoSpin();
            SpinButton.gameObject.SetActive(true);
            StopSpinButton.gameObject.SetActive(false);
        }
    }

    internal void ToggleBackground(bool isDay)
    {
        Bg_Image.sprite = isDay ? Day_Sprite : Night_Sprite;
    }

    internal void ToggleBonusBackground()
    {
        Bg_Image.sprite = Bonus_Sprite;
    }

    internal void ToggleFreeSpinUI(bool isFreeSpin, int freeSpinLeft)
    {
        if (isFreeSpin)
        {
            FreeSpinCountPanel.SetActive(true);
            ToggleBackground(false);
            // Free-spin counter rendered as sprites too
            if (FreeSpinText) FreeSpinText.text = ToSpriteString(freeSpinLeft);
        }
        else
        {
            FreeSpinCountPanel.SetActive(false);
            ToggleBackground(true);
        }
    }
}