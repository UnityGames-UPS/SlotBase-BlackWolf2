using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public class UIManager : MonoBehaviour
{
    [Header("Slot UI")]
    [SerializeField] private Button SpinButton;
    [SerializeField] private Button StopSpinButton;
    [SerializeField] internal Button AutoSpinButton;

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
    [SerializeField] private Image BgSide_Image;
    [SerializeField] private Sprite Day_Sprite;
    [SerializeField] private Sprite Night_Sprite;
    [SerializeField] private Sprite Bonus_Sprite;
    [SerializeField] private Sprite DaySide_Sprite;
    [SerializeField] private Sprite NightSide_Sprite;
    [SerializeField] private Sprite BonusSide_Sprite;

    [Header("Main Popus UI Object")]
    [SerializeField] private GameObject MainPopup_Object;

    [Header("Paytable Popup UI References")]
    [SerializeField] private GameObject[] Pages;
    [SerializeField] private Button Paytable_Button;
    [SerializeField] private GameObject PaytablePopup_Object;
    [SerializeField] private Button PaytableExit_Button;
    [SerializeField] private Button PaytableLeft_Button;
    [SerializeField] private Button PaytableRight_Button;

    [SerializeField] private List<TMP_Text> ScatterSymbolText;
    [SerializeField] private List<TMP_Text> WildSymbolText;
    [SerializeField] private List<TMP_Text> MooseSymbolText;
    [SerializeField] private List<TMP_Text> LynxSymbolText;
    [SerializeField] private List<TMP_Text> EagleSymbolText;
    [SerializeField] private List<TMP_Text> OwlSymbolText;
    [SerializeField] private List<TMP_Text> ASymbolText;
    [SerializeField] private List<TMP_Text> KSymbolText;
    [SerializeField] private List<TMP_Text> QSymbolText;
    [SerializeField] private List<TMP_Text> JSymbolText;

    [Header("Sound/Music UI References")]
    [SerializeField] private Button Sound_Button;
    [SerializeField] private Button Music_Button;
    [SerializeField] private Sprite SoundOff_Sprite;
    [SerializeField] private Sprite SoundOn_Sprite;
    [SerializeField] private Sprite MusicOff_Sprite;
    [SerializeField] private Sprite MusicOn_Sprite;

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

    internal double currentTotalBet = 0;
    internal int betCounter = 0;
    private bool isMusic = true;
    private bool isSound = true;
    private bool isExit = false;

    private int paytablePageCounter;

    internal static string ToSpriteString(string value)
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

    internal static string ToSpriteString(double value, string format = "F2")
        => ToSpriteString(value.ToString(format));

    internal static string ToSpriteString(int value)
        => ToSpriteString(value.ToString());

    internal static double FromSpriteString(string spriteText)
    {
        if (string.IsNullOrEmpty(spriteText)) return 0;
        var sb = new StringBuilder();
        int i = 0;
        while (i < spriteText.Length)
        {
            if (spriteText[i] == '<')
            {
                int end = spriteText.IndexOf('>', i);
                if (end < 0) break;
                string tag = spriteText.Substring(i, end - i + 1);
                const string prefix = "<sprite index=";
                if (tag.StartsWith(prefix))
                {
                    string numStr = tag.Substring(prefix.Length, tag.Length - prefix.Length - 1);
                    if (int.TryParse(numStr, out int idx))
                    {
                        if (idx >= 0 && idx <= 9) sb.Append((char)('0' + idx));
                        else if (idx == 10) sb.Append('.');
                        else if (idx == 11) sb.Append(',');
                        else if (idx == 12) sb.Append('+');
                    }
                }
                i = end + 1;
            }
            else
            {
                sb.Append(spriteText[i]);
                i++;
            }
        }
        string plain = sb.ToString().Replace(",", "");
        return double.TryParse(plain, System.Globalization.NumberStyles.Any,
                               System.Globalization.CultureInfo.InvariantCulture, out double result)
               ? result : 0;
    }


    private void Start()
    {
        if (SpinButton) SpinButton.onClick.AddListener(OnSpinButtonPressed);
        if (AutoSpinButton) AutoSpinButton.onClick.AddListener(OnAutoSpinButtonPressed);
        if (StopSpinButton) StopSpinButton.onClick.AddListener(OnStopSpinButtonPressed);

        if (BetPlus_Button) BetPlus_Button.onClick.AddListener(delegate { ChangeBet(true); });
        if (BetMinus_Button) BetMinus_Button.onClick.AddListener(delegate { ChangeBet(false); });

        isMusic = true;
        isSound = true;

        //if (_audioController) _audioController.ToggleMute(false);

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

        // if (SkipWinAnimation) SkipWinAnimation.onClick.RemoveAllListeners();
        // if (SkipWinAnimation) SkipWinAnimation.onClick.AddListener(SkipWin);
    }

    private void OpenPaytable()
    {
        //_audioController.PlayButtonAudio();
        foreach (GameObject gameObject in Pages)
            gameObject.SetActive(false);
        paytablePageCounter = 0;
        Pages[0].SetActive(true);
        MainPopup_Object.SetActive(true);
        PaytablePopup_Object.SetActive(true);
    }

    private void SwitchPages(bool IncDec)
    {
        //_audioController.PlayButtonAudio();
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


    internal void ADfunction()
    {
        OpenPopup(ADPopup_Object);
    }

    internal void InitialiseUIData(Root root)
    {
        InitialisePayTable();
        UpdateBalance(root.player.balance);
        _slotManager._currentBalance = root.player.balance;
        PopulateBonusInfo(root.features);
        UpdateWin(0.00);
        if (Bet_Text) Bet_Text.text = ToSpriteString(root.gameData.bets[betCounter] * _socketManager.initialData.lines.Count);
        currentTotalBet = root.gameData.bets[betCounter] * root.gameData.lines.Count;
    }

    private void PopulateBonusInfo(Features features)
    {
        if (GrandText) GrandText.text = ToSpriteString(features.jackpots.grand * _socketManager.initialData.bets[betCounter]);
        if (MajorText) MajorText.text = ToSpriteString(features.jackpots.major * _socketManager.initialData.bets[betCounter]);
        if (MinorText) MinorText.text = ToSpriteString(features.jackpots.minor * _socketManager.initialData.bets[betCounter]);
        if (MiniText) MiniText.text = ToSpriteString(features.jackpots.mini * _socketManager.initialData.bets[betCounter]);
    }

    internal void UpdateBalance(double balance)
    {
        if (Balance_Text) Balance_Text.text = ToSpriteString(balance, "F2");
    }

    internal void UpdateWin(double winAmount)
    {
        if (WinAmount_Text) WinAmount_Text.text = ToSpriteString(winAmount, "F2");
    }

    private void ChangeBet(bool IncDec)
    {
        if (_audioController) _audioController.PlayBetButton();
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
        if (Bet_Text) Bet_Text.text = ToSpriteString(_socketManager.initialData.bets[betCounter] * _socketManager.initialData.lines.Count);
        currentTotalBet = _socketManager.initialData.bets[betCounter] * _socketManager.initialData.lines.Count;
        PopulateBonusInfo(_socketManager.features);
        InitialisePayTable();
    }

    internal void SetBetButtonsInteractable(bool interactable)
    {
        if (BetPlus_Button) BetPlus_Button.interactable = interactable;
        if (BetMinus_Button) BetMinus_Button.interactable = interactable;
    }

    private void CallOnExitFunction()
    {
        isExit = true;
        //_audioController.PlayButtonAudio();
        StartCoroutine(_socketManager.CloseSocket());
    }

    private void OpenPopup(GameObject Popup)
    {
        //if (_audioController) _audioController.PlayButtonAudio();
        if (Popup) Popup.SetActive(true);
        if (MainPopup_Object) MainPopup_Object.SetActive(true);
    }

    private void ClosePopup(GameObject Popup)
    {
        //if (_audioController) _audioController.PlayButtonAudio();
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
        _audioController.PlayBetButton();
        if (isMusic)
        {
            Music_Button.image.sprite = MusicOff_Sprite;
            if (_audioController) _audioController.PlayBetButton();
            if (_audioController) _audioController.MuteBackground(true);
            isMusic = false;
        }
        else
        {
            Music_Button.image.sprite = MusicOn_Sprite;
            if (_audioController) _audioController.PlayBetButton();
            if (_audioController) _audioController.MuteBackground(false);
            isMusic = true;
        }
    }

    private void ToggleSound()
    {
        _audioController.PlayBetButton();
        if (isSound)
        {
            Sound_Button.image.sprite = SoundOff_Sprite;
            if (_audioController) _audioController.PlayBetButton();
            if (_audioController) _audioController.MuteGame(true);
            isSound = false;
        }
        else
        {
            Sound_Button.image.sprite = SoundOn_Sprite;
            if (_audioController) _audioController.PlayBetButton();
            if (_audioController) _audioController.MuteGame(false);
            isSound = true;
        }
    }


    private void OnSpinButtonPressed()
    {
        // Lock the spin button immediately — OnStopSpinButtonPressed re-enables it
        // only after the slots have fully settled.
        _audioController.PlaySpinStarts();
        SpinButton.interactable = false;
        SetBetButtonsInteractable(false);

        _slotManager.StartSlots();
        StopSpinButton.gameObject.SetActive(true);
        SpinButton.gameObject.SetActive(false);
    }

    internal void OnStopSpinButtonPressed()
    {
        _audioController.PlayBetButton();
        _slotManager.RequestInstantStop();

        if (_slotManager._isAutoSpin)
        {
            _slotManager.StopAutoSpin();
            AutoSpinButtonAnimation(false);
            AutoSpinButton.interactable = false;
        }

        // Keep the stop button visible but non-interactable while reels and post-spin
        // logic finish. SetSpinButtonReady() is the only place that switches back to
        // the spin button, and only once everything is truly done.
        StopSpinButton.interactable = false;
        StopSpinButton.gameObject.SetActive(true);
        SpinButton.gameObject.SetActive(false);
    }

    internal void OnStopSpinButtonTrigger()
    {
        OnStopSpinButtonPressed();
    }

    internal void SetSpinButtonReady()
    {
        // Always reset stop button interactability so it works next spin.
        StopSpinButton.interactable = true;

        if (_slotManager._isAutoSpin)
        {
            // Autospin is still active — next spin is about to start.
            // Keep showing the interactable stop button; spin button stays hidden.
            StopSpinButton.gameObject.SetActive(true);
            SpinButton.gameObject.SetActive(false);
        }
        else
        {
            // Truly idle — show the spin button and hide stop.
            SpinButton.interactable = true;
            SpinButton.gameObject.SetActive(true);
            StopSpinButton.gameObject.SetActive(false);
            SetBetButtonsInteractable(true);
            AutoSpinButton.interactable = true;
        }
    }

    private void OnAutoSpinButtonPressed()
    {
        _audioController.PlayBetButton();
        if (!_slotManager._isAutoSpin)
        {
            _slotManager.AutoSpin();
            SetBetButtonsInteractable(false);
            StopSpinButton.gameObject.SetActive(true);
            SpinButton.gameObject.SetActive(false);
            //AutoSpinButton.gameObject.GetComponent<ImageAnimation>().StartAnimation();
            AutoSpinButtonAnimation(true);
        }
        else
        {
            AutoSpinButton.interactable = false;
            _slotManager.StopAutoSpin();
            // Keep stop button visible but non-interactable while the current
            // spin finishes. SetSpinButtonReady() restores the spin button once done.
            StopSpinButton.interactable = false;
            StopSpinButton.gameObject.SetActive(true);
            SpinButton.gameObject.SetActive(false);
            //AutoSpinButton.gameObject.GetComponent<ImageAnimation>().StopAnimation();
            AutoSpinButtonAnimation(false);
        }
    }

    internal void ToggleBackground(bool isDay)
    {
        Bg_Image.sprite = isDay ? Day_Sprite : Night_Sprite;
        BgSide_Image.sprite = isDay ? DaySide_Sprite : NightSide_Sprite;
    }

    internal void ToggleBonusBackground()
    {
        Bg_Image.sprite = Bonus_Sprite;
        BgSide_Image.sprite = BonusSide_Sprite;
    }

    // Ticks the displayed free-spin counter down by 1 at the moment the spin
    // starts. The server-authoritative count is then reconfirmed by
    // ToggleFreeSpinUI at the end of the spin, so it can never drift.
    internal void DecrementFreeSpinCount()
    {
        if (FreeSpinText != null &&
            int.TryParse(FreeSpinText.text, out int current) && current > 0)
        {
            FreeSpinText.text = (current - 1).ToString();
        }
    }

    internal void ToggleFreeSpinUI(bool isFreeSpin, int freeSpinLeft = 0)
    {
        if (isFreeSpin)
        {
            FreeSpinCountPanel.SetActive(true);
            ToggleBackground(false);
            if (FreeSpinText) FreeSpinText.text = freeSpinLeft.ToString();

            SpinButton.interactable = false;
            SpinButton.gameObject.SetActive(false);
            StopSpinButton.interactable = false;
            StopSpinButton.gameObject.SetActive(true);
        }
        else
        {
            FreeSpinCountPanel.SetActive(false);
            ToggleBackground(true);
        }
    }

    private void InitialisePayTable()
    {
        for (int i = 0; i < _socketManager.initUIData.paylines.symbols[12].multiplier.Count; i++)
        {
            ScatterSymbolText[i].text = (_socketManager.initUIData.paylines.symbols[12].multiplier[i] * _socketManager.initialData.bets[betCounter]).ToString() + "X";
        }
        for (int i = 0; i < _socketManager.initUIData.paylines.symbols[8].multiplier.Count; i++)
        {
            WildSymbolText[i].text = (_socketManager.initUIData.paylines.symbols[8].multiplier[i] * _socketManager.initialData.bets[betCounter]).ToString() + "X";
        }
        for (int i = 0; i < _socketManager.initUIData.paylines.symbols[7].multiplier.Count; i++)
        {
            MooseSymbolText[i].text = (_socketManager.initUIData.paylines.symbols[7].multiplier[i] * _socketManager.initialData.bets[betCounter]).ToString() + "X";
        }
        for (int i = 0; i < _socketManager.initUIData.paylines.symbols[5].multiplier.Count; i++)
        {
            LynxSymbolText[i].text = (_socketManager.initUIData.paylines.symbols[5].multiplier[i] * _socketManager.initialData.bets[betCounter]).ToString() + "X";
        }
        for (int i = 0; i < _socketManager.initUIData.paylines.symbols[6].multiplier.Count; i++)
        {
            EagleSymbolText[i].text = (_socketManager.initUIData.paylines.symbols[6].multiplier[i] * _socketManager.initialData.bets[betCounter]).ToString() + "X";
        }
        for (int i = 0; i < _socketManager.initUIData.paylines.symbols[4].multiplier.Count; i++)
        {
            OwlSymbolText[i].text = (_socketManager.initUIData.paylines.symbols[4].multiplier[i] * _socketManager.initialData.bets[betCounter]).ToString() + "X";
        }
        for (int i = 0; i < _socketManager.initUIData.paylines.symbols[0].multiplier.Count; i++)
        {
            ASymbolText[i].text = (_socketManager.initUIData.paylines.symbols[0].multiplier[i] * _socketManager.initialData.bets[betCounter]).ToString() + "X";
        }
        for (int i = 0; i < _socketManager.initUIData.paylines.symbols[1].multiplier.Count; i++)
        {
            KSymbolText[i].text = (_socketManager.initUIData.paylines.symbols[1].multiplier[i] * _socketManager.initialData.bets[betCounter]).ToString() + "X";
        }
        for (int i = 0; i < _socketManager.initUIData.paylines.symbols[2].multiplier.Count; i++)
        {
            QSymbolText[i].text = (_socketManager.initUIData.paylines.symbols[2].multiplier[i] * _socketManager.initialData.bets[betCounter]).ToString() + "X";
        }
        for (int i = 0; i < _socketManager.initUIData.paylines.symbols[3].multiplier.Count; i++)
        {
            JSymbolText[i].text = (_socketManager.initUIData.paylines.symbols[3].multiplier[i] * _socketManager.initialData.bets[betCounter]).ToString() + "X";
        }
    }

    private void AutoSpinButtonAnimation(bool animate)
    {
        var arrowObject = AutoSpinButton.transform.GetChild(0).gameObject;
        //while (isAutoSpinAnimating)
        if (animate)
        {
            //arrowObject.SetActive(true);
            arrowObject.transform.DORotate(new Vector3(0, 0, -360), 3f, RotateMode.LocalAxisAdd)
                       .SetEase(Ease.Linear)
                       .SetLoops(-1);
        }
        else
        {
            arrowObject.transform.DOKill();
            //arrowObject.transform.rotation = new Quaternion(0, 0, 0, 0);
        }
    }
}