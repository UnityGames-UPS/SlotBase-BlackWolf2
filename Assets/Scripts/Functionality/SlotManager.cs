using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using DG.Tweening;
using System;
using System.Collections;

public class SlotManager : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField] private Sprite[] _symbolSprites;
    [SerializeField] private Sprite BlankSprite;
    [SerializeField] private Sprite MiniTextSprite;
    [SerializeField] private Sprite MinorTextSprite;
    [SerializeField] private Sprite MajorTextSprite;

    [Header("Slot Images")]
    [SerializeField] private List<SlotImage> _totalImages;
    [SerializeField] internal List<SlotImage> _resultImages;
    [SerializeField] private List<GameObject> AnimationSlots;

    [Header("Slots Transforms")]
    [SerializeField] private Transform[] _slotTransforms;

    [Header("Image Animation")]
    [SerializeField] private List<Sprite> freeSpinStartAnimation;
    [SerializeField] private List<Sprite> freeSpinLoopAnimation;
    [SerializeField] private List<Sprite> freeSpinTriggerAnimation;
    [SerializeField] private List<Sprite> YellowMoonAnimation;
    [SerializeField] private List<Sprite> YellowMoonLoopAnimation;
    [SerializeField] private List<Sprite> PurpleMoonAnimation;
    [SerializeField] private List<Sprite> PurpleMoonLoopAnimation;
    [SerializeField] private List<Sprite> BoostSymbolAnimation;
    [SerializeField] private List<Sprite> BoostSymbolLoopAnimation;

    [SerializeField] private List<Sprite> PurpleToYellowTransitionAnimation;

    [Header("UI Elements")]
    [SerializeField] private GameObject WinSlotsParent;

    [Header("FreeSpin UI")]
    [SerializeField] private GameObject FreeSpinIntroPage;
    [SerializeField] private GameObject FreeSpinTextAnimationObject;
    [SerializeField] private GameObject FreeSpinMoonAnimationObject;
    [SerializeField] private TMP_Text TimerText;
    [SerializeField] private Button TimerSkipButton;
    private bool TimerSkipped = false;

    [SerializeField] private GameObject FreeSpinStartAnimationObject;

    [Header("Managers")]
    [SerializeField] private AudioController _audioController;
    [SerializeField] private UIManager _uiManager;
    [SerializeField] private SocketIOManager _socketManager;
    [SerializeField] private PayLineManager _paylineManager;
    [SerializeField] private AnimationManager animationManager;
    [SerializeField] private BonusManger bonusManger;
    [SerializeField] private WinPopupAnimation winPopupAnimation;
    [SerializeField] private MoonPhases moonPhases;

    [Header("Auto spin setting")]

    internal bool SocketConnected = false;
    internal bool _isAutoSpin = false;
    internal bool _checkPopups = false;

    private bool FreeSpinIntroFinished = false;
    private bool isMystryRevealed = false;
    private bool _wasAutoSpinOn;
    private List<Tween> _alltweens = new List<Tween>();
    private Coroutine _autoSpinRoutine = null;
    private Coroutine _tweenRoutine;
    private Coroutine _loopLinesCoroutine;
    private bool _isFreeSpin = false;
    private bool isFirstFreeSpin = false;
    private bool _isSpinning = false;
    private bool _checkSpinAudio = false;
    internal double _currentBalance = 0;
    internal int _numberOfSlots = 5;
    private bool _stopSpinToggle;
    private float _spinDelay = 0.2f;
    private bool _isTurboOn;
    private bool isTweening = false;

    #region Initial Functions

    private void Start()
    {
        RefreshTrails();
        shuffleSlotImages();
        if (TimerSkipButton)
        {
            TimerSkipButton.onClick.RemoveAllListeners();
            TimerSkipButton.onClick.AddListener(TimerSkipButtonClicked);
        }
    }

    private void RefreshTrails()
    {
        for (int i = 0; i < _resultImages.Count; i++)
        {
            for (int j = 0; j < _resultImages[i].slotImages.Count; j++)
            {
                TrailObject trail = _resultImages[i].slotImages[j].gameObject.GetComponentInChildren<TrailObject>(true);
                if (trail != null)
                {
                    trail.gameObject.SetActive(true);
                    trail.gameObject.SetActive(false);
                }
            }
        }
    }

    internal void shuffleSlotImages(bool midTween = false)
    {
        for (int i = 0; i < _totalImages.Count; i++)
        {
            for (int j = 0; j < _totalImages[i].slotImages.Count; j++)
            {
                Sprite image = _symbolSprites[UnityEngine.Random.Range(0, 8)];
                if (!midTween)
                    _totalImages[i].slotImages[j].sprite = image;
            }
        }
        for (int i = 0; i < _resultImages.Count; i++)
        {
            for (int j = 0; j < _resultImages[i].slotImages.Count; j++)
            {
                _resultImages[i].slotImages[j].transform.GetChild(2).GetComponent<RectTransform>().localScale = Vector3.one;
                Sprite image = _symbolSprites[UnityEngine.Random.Range(0, 9)];
                if (!midTween)
                {
                    _resultImages[i].slotImages[j].sprite = BlankSprite;
                    _resultImages[i].slotImages[j].transform.GetChild(2).gameObject.SetActive(true);
                    if (image == _symbolSprites[8])
                    {
                        _resultImages[i].slotImages[j].transform.GetChild(2).GetComponent<RectTransform>().localScale = Vector3.one * 1.4f;
                    }
                    _resultImages[i].slotImages[j].transform.GetChild(2).GetComponent<Image>().preserveAspect = true;
                    _resultImages[i].slotImages[j].transform.GetChild(2).GetComponent<Image>().sprite = image;
                }
            }
        }
    }

    private void ResetAllTrails()
    {
        for (int i = 0; i < _resultImages.Count; i++)
        {
            for (int j = 0; j < _resultImages[i].slotImages.Count; j++)
            {
                TrailObject trail = _resultImages[i].slotImages[j].gameObject.GetComponentInChildren<TrailObject>(true);
                if (trail != null)
                    trail.ResetTrail();
            }
        }
    }

    #endregion

    #region Autospin
    internal void AutoSpin()
    {
        if (!_isAutoSpin)
        {
            _isAutoSpin = true;

            if (_autoSpinRoutine != null)
            {
                StopCoroutine(_autoSpinRoutine);
                _autoSpinRoutine = null;
            }
            _autoSpinRoutine = StartCoroutine(AutoSpinCoroutine());
        }
    }

    internal void StopAutoSpin()
    {
        //_audioController.PlayButtonAudio();
        if (_isAutoSpin)
        {
            _isAutoSpin = false;
            _wasAutoSpinOn = false;
            StartCoroutine(StopAutoSpinCoroutine());
        }
    }

    private IEnumerator AutoSpinCoroutine()
    {
        while (_isAutoSpin)
        {
            StartSlots(_isAutoSpin);
            yield return new WaitUntil(() => !_isSpinning);
            yield return new WaitForSeconds(_spinDelay);
        }
        if (_wasAutoSpinOn)
            _wasAutoSpinOn = false;
    }

    private IEnumerator StopAutoSpinCoroutine()
    {
        yield return new WaitUntil(() => !_isSpinning);
        _isAutoSpin = false;
        _uiManager.AutoSpinButton.interactable = true;
        if (_autoSpinRoutine != null)
        {
            StopCoroutine(_autoSpinRoutine);
            _autoSpinRoutine = null;
        }
        // The current spin just finished and autospin is now fully cancelled.
        // Tell UIManager to restore the spin button now that we are truly idle.
        _uiManager.SetSpinButtonReady();
    }
    #endregion

    #region SlotSpin

    internal void RequestInstantStop()
    {
        if (isTweening)
        {
            _stopSpinToggle = true;
        }
    }

    internal void StartSlots(bool autoSpin = false)
    {
        if (_isSpinning) return;

        if (!autoSpin)
        {
            if (_autoSpinRoutine != null)
            {
                StopCoroutine(_autoSpinRoutine);
                StopCoroutine(_tweenRoutine);
                _tweenRoutine = null;
                _autoSpinRoutine = null;
            }
        }
        _tweenRoutine = StartCoroutine(TweenRoutine());
    }

    private IEnumerator TweenRoutine()
    {
        _isSpinning = true;

        if (_currentBalance < _uiManager.currentTotalBet && !_isFreeSpin)
        {
            StopAutoSpin();
            _isSpinning = false;
            yield return new WaitForSeconds(1);
            _uiManager.LowBalPopup();
            _uiManager.SetBetButtonsInteractable(true);
            yield break;
        }
        if (!_isFreeSpin)
        {
            _uiManager.UpdateBalance(_currentBalance - _uiManager.currentTotalBet);
        }
        else
        {
            // Decrement the displayed free-spin counter NOW — before the reels
            // start moving — so the count visually drops at the start of the spin.
            // ToggleFreeSpinUI at the end of each spin will then confirm the
            // authoritative server value, keeping it accurate.
            _uiManager.DecrementFreeSpinCount();
        }
        _paylineManager.isDisplayingWinningLines = false;
        _uiManager.UpdateWin(0.00);
        ResetAllTrails();
        //shuffleSlotImages();
        ResetAllSymbolAnimations();
        yield return null;

        if (_loopLinesCoroutine != null)
        {
            StopCoroutine(_loopLinesCoroutine);
            _loopLinesCoroutine = null;
        }
        WinSlotsParent.SetActive(false);

        //if (_audioController) _audioController.PlayWLAudio("spin");

        _checkSpinAudio = true;
        _isSpinning = true;
        isTweening = true;

        for (int i = 0; i < _numberOfSlots; i++)
        {
            InitializeTweening(_slotTransforms[i]);
            yield return new WaitForSeconds(0.1f);
        }


        if (isFirstFreeSpin)
        {
            // first free spin animation placeholder
            FreeSpinStartAnimationObject.SetActive(true);
            var imgAnim = FreeSpinStartAnimationObject.GetComponent<ImageAnimation>();
            imgAnim.StartAnimation();
            yield return new WaitUntil(() => imgAnim.currentAnimationState == ImageAnimation.ImageState.FINISHED);
            FreeSpinStartAnimationObject.SetActive(false);
        }

        _socketManager.AccumulateResult(_uiManager.betCounter);
        yield return new WaitUntil(() => _socketManager.isResultdone);
        _currentBalance = _socketManager.resultData.player.balance;

        StartCoroutine(moonPhases.SetMoonPhase(_socketManager.resultData.payload.levelProgress));

        // Load result matrix into result images
        for (int j = 0; j < _socketManager.resultData.matrix.Count; j++)
        {
            for (int i = 0; i < _socketManager.resultData.matrix[j].Count; i++)
            {
                if (int.TryParse(_socketManager.resultData.matrix[j][i], out int symbolId))
                {
                    //_resultImages[i].slotImages[j].sprite = _symbolSprites[symbolId];
                    _resultImages[i].slotImages[j].GetComponentInChildren<TMP_Text>().text = "";
                    _resultImages[i].slotImages[j].GetComponentInChildren<TMP_Text>().GetComponentInChildren<Image>(true).gameObject.SetActive(false);
                    StartCoroutine(SymbolSize(_resultImages[i].slotImages[j], _symbolSprites[symbolId]));
                }
            }
        }

        if (_socketManager.resultData.payload.bs_count > 0)
        {
            foreach (var bs in _socketManager.resultData.payload.bs)
            {
                int reel = bs.reel;
                int position = bs.position;
                if (_resultImages[reel].slotImages[position].transform.GetChild(2).GetComponent<Image>().sprite != _symbolSprites[11])
                {
                    if (bs.isJackpot)
                    {
                        if (bs.jackpotName.ToUpper() == "MINI")
                        {
                            _resultImages[reel].slotImages[position].GetComponentInChildren<TMP_Text>().GetComponentInChildren<Image>(true).gameObject.SetActive(true);
                            _resultImages[reel].slotImages[position].GetComponentInChildren<TMP_Text>().GetComponentInChildren<Image>().sprite = MiniTextSprite;
                        }
                        else if (bs.jackpotName.ToUpper() == "MINOR")
                        {
                            _resultImages[reel].slotImages[position].GetComponentInChildren<TMP_Text>().GetComponentInChildren<Image>(true).gameObject.SetActive(true);
                            _resultImages[reel].slotImages[position].GetComponentInChildren<TMP_Text>().GetComponentInChildren<Image>().sprite = MinorTextSprite;
                        }
                        else if (bs.jackpotName.ToUpper() == "MAJOR")
                        {
                            _resultImages[reel].slotImages[position].GetComponentInChildren<TMP_Text>().GetComponentInChildren<Image>(true).gameObject.SetActive(true);
                            _resultImages[reel].slotImages[position].GetComponentInChildren<TMP_Text>().GetComponentInChildren<Image>().sprite = MajorTextSprite;
                        }
                    }
                    else
                    {
                        _resultImages[reel].slotImages[position].GetComponentInChildren<TMP_Text>().text =
                            UIManager.ToSpriteString(bs.value);
                    }
                }
            }
        }


        if (_isTurboOn)
        {
            _stopSpinToggle = true;
        }
        if (!_stopSpinToggle)
        {
            for (int i = 0; i < 5; i++)
            {
                yield return new WaitForSeconds(0.1f);
                if (_stopSpinToggle) break;
            }
        }

        int pendingTrails = 0;
        int finishedTrails = 0;
        int MoonImageCount = 0;

        for (int i = 0; i < _numberOfSlots; i++)
        {
            if (MoonImageCount >= 4 && !_stopSpinToggle)
            {
                GameObject slotImage = AnimationSlots[i].transform.GetChild(0).gameObject;
                slotImage.SetActive(true);
                slotImage.GetComponent<ImageAnimation>().StartAnimation();
                yield return new WaitForSeconds(1f);
            }

            yield return StopTweening(_slotTransforms[i], i, _stopSpinToggle);
            _audioController.PlayReelHit();

            for (int j = 0; j < _resultImages[i].slotImages.Count; j++)
            {
                if (_resultImages[i].slotImages[j].transform.GetChild(2).GetComponent<Image>().sprite == _symbolSprites[9] ||
                    _resultImages[i].slotImages[j].transform.GetChild(2).GetComponent<Image>().sprite == _symbolSprites[11])
                {
                    MoonImageCount++;
                }
            }
            for (int j = 0; j < _resultImages[i].slotImages.Count; j++)
            {
                if (_resultImages[i].slotImages[j].transform.GetChild(2).GetComponent<Image>().sprite == _symbolSprites[12] && _socketManager.resultData.payload.freeSpinTriggered)
                {
                    StartCoroutine(MoonSymbolAnimation(_resultImages[i].slotImages[j]));
                }
                if (_resultImages[i].slotImages[j].transform.GetChild(2).GetComponent<Image>().sprite == _symbolSprites[9] ||
                    _resultImages[i].slotImages[j].transform.GetChild(2).GetComponent<Image>().sprite == _symbolSprites[10] ||
                    _resultImages[i].slotImages[j].transform.GetChild(2).GetComponent<Image>().sprite == _symbolSprites[11])
                {
                    StartCoroutine(MoonSymbolAnimation(_resultImages[i].slotImages[j]));
                    TrailObject trail = _resultImages[i].slotImages[j].gameObject.GetComponentInChildren<TrailObject>(true);
                    if (trail != null)
                    {
                        pendingTrails++;
                        StartCoroutine(trail.StartLogoAnimation(() => finishedTrails++));
                        _audioController.PlayLightSound();
                    }
                }
            }

            AnimationSlots[i].transform.GetChild(0).gameObject.SetActive(false);
        }
        isTweening = false;


        // // Trigger symbol animations for every reel now that all have landed
        // for (int i = 0; i < _numberOfSlots; i++)
        // {
        //     for (int j = 0; j < _resultImages[i].slotImages.Count; j++)
        //     {
        //         if (_resultImages[i].slotImages[j].sprite == _symbolSprites[12] && _socketManager.resultData.payload.freeSpinTriggered)
        //         {
        //             StartCoroutine(MoonSymbolAnimation(_resultImages[i].slotImages[j]));
        //         }
        //         if (_resultImages[i].slotImages[j].sprite == _symbolSprites[9] ||
        //             _resultImages[i].slotImages[j].sprite == _symbolSprites[10] ||
        //             _resultImages[i].slotImages[j].sprite == _symbolSprites[11])
        //         {
        //             StartCoroutine(MoonSymbolAnimation(_resultImages[i].slotImages[j]));
        //             TrailObject trail = _resultImages[i].slotImages[j].gameObject.GetComponentInChildren<TrailObject>(true);
        //             if (trail != null)
        //             {
        //                 pendingTrails++;
        //                 StartCoroutine(trail.StartLogoAnimation(() => finishedTrails++));
        //                 _audioController.PlayLightSound();
        //             }
        //         }
        //     }
        //     if (!_stopSpinToggle)
        //         AnimationSlots[i].transform.GetChild(0).gameObject.SetActive(false);
        // }

        _stopSpinToggle = false;

        //if (_audioController) _audioController.StopWLAaudio();
        yield return _alltweens[^1].WaitForCompletion();
        KillAllTweens();

        if (pendingTrails > 0)
            yield return new WaitUntil(() => finishedTrails >= pendingTrails);

        if (_socketManager.resultData.payload.is_boost)
        {
            ResetAllTrails();
            animationManager.isBoostAnimationFinished = false;
            StartCoroutine(animationManager.BoostAnimation(_socketManager.resultData.payload.boost_positions));
            yield return new WaitUntil(() => animationManager.isBoostAnimationFinished);
            Debug.Log("Boost Animation Finished");

            if (_socketManager.resultData.payload.mysteryRevealed != null &&
                _socketManager.resultData.payload.mysteryRevealed.Count > 0)
            {
                isMystryRevealed = false;
                StartCoroutine(MysteryRevealedAnimation(_socketManager.resultData.payload.mysteryRevealed));
                yield return new WaitUntil(() => isMystryRevealed);
            }

            Debug.Log("Starting to check for boost blast animations");
            for (int i = 0; i < _numberOfSlots; i++)
            {
                for (int j = 0; j < _resultImages[i].slotImages.Count; j++)
                {
                    if (_resultImages[i].slotImages[j].sprite == _symbolSprites[9] || _resultImages[i].slotImages[j].transform.GetChild(2).GetComponent<Image>().sprite == _symbolSprites[9])
                    //foreach (var bs in _socketManager.resultData.payload.bs)
                    {
                        animationManager.isBoostBlastAnimationFinished = false;
                        double boostAmount = UIManager.FromSpriteString(_resultImages[i].slotImages[j].gameObject.GetComponentInChildren<TMP_Text>().text);
                        //double boostAmount = bs.value;
                        if (boostAmount == 0)
                        {
                            foreach (var bs in _socketManager.resultData.payload.bs)
                            {
                                if (bs.reel == i && bs.position == j)
                                {
                                    boostAmount = bs.value;
                                    break;
                                }
                            }
                        }
                        Debug.Log($"Starting Boost Blast Animation for symbol at reel {i} position {j} with boost amount {boostAmount}");
                        TrailObject trail = _resultImages[i].slotImages[j].gameObject.GetComponentInChildren<TrailObject>(true);
                        if (trail != null)
                        {
                            StartCoroutine(trail.StartBoostAnimation(boostAmount, _resultImages[i].slotImages[j].transform.GetChild(5).gameObject));
                            _audioController.PlayLightSound();
                        }
                        yield return new WaitUntil(() => animationManager.isBoostBlastAnimationFinished);
                    }
                }
            }
            Debug.Log("Trail Animation Finished");
            animationManager.ResetBoostAnimation(_resultImages);
        }

        if (_socketManager.resultData.payload.boostWin > 0)
        {
            // Boost winnings shown — placeholder
            winPopupAnimation.ShowBoostWinPopup(_socketManager.resultData.payload.boostWin);
            _uiManager.UpdateBalance(_currentBalance);
            yield return new WaitUntil(() => winPopupAnimation.popupDone);
        }

        if (_socketManager.resultData.payload.lineWins.Count > 0)
        {
            _paylineManager.DisplayWinningLines(_socketManager.resultData.payload.lineWins);
            //_uiManager.UpdateWin(_socketManager.resultData.payload.winAmount);
            winPopupAnimation.ShowNormalWinPopup(_socketManager.resultData.payload.winAmount, !_isFreeSpin);
            _uiManager.UpdateBalance(_currentBalance);
            yield return new WaitUntil(() => winPopupAnimation.popupDone);
        }

        if (_socketManager.resultData.payload.state.mode == "HOLD_AND_WIN")
        {
            _isFreeSpin = false;
            bonusManger.isBonusFinished = false;
            StartCoroutine(bonusManger.StartBonus());
            yield return new WaitUntil(() => bonusManger.isBonusFinished);

            if (_socketManager.resultData.payload.levelProgress.total_progress == 0)
            {
                moonPhases.ResetMoon();
            }

            _audioController.PlayBackground();

            if (_socketManager.resultData.payload.state.freeSpinsLeft > 0 && !isFirstFreeSpin)
            {
                _isFreeSpin = true;
                _uiManager.ToggleFreeSpinUI(true, _socketManager.resultData.payload.state.freeSpinsLeft);
                _tweenRoutine = StartCoroutine(TweenRoutine());
            }
            else
            {
                _isFreeSpin = false;
                _uiManager.ToggleFreeSpinUI(false, 0);
                StartCoroutine(moonPhases.SetMoonPhase(_socketManager.resultData.payload.levelProgress));
                _isSpinning = false;
                _uiManager.SetSpinButtonReady();
            }
            yield break;
        }


        if (_socketManager.resultData.payload.freeSpinTriggered && !_isFreeSpin)
        {
            FreeSpinIntroFinished = false;
            StartCoroutine(FreeSpinStartAnimation());
            yield return new WaitUntil(() => FreeSpinIntroFinished);
        }

        if (_socketManager.resultData.payload.state.mode == "FREE_SPINS" &&
            _socketManager.resultData.payload.state.freeSpinsLeft > 0)
        {
            isFirstFreeSpin = !_isFreeSpin;
            _isFreeSpin = true;
            _uiManager.ToggleFreeSpinUI(true, _socketManager.resultData.payload.state.freeSpinsLeft);
            _tweenRoutine = StartCoroutine(TweenRoutine());
            yield break;
        }
        else if (_isFreeSpin && _socketManager.resultData.payload.state.freeSpinsLeft == 0)
        {
            _isFreeSpin = false;
            isFirstFreeSpin = false;
            _uiManager.ToggleFreeSpinUI(false, 0);
            winPopupAnimation.ShowTotalWinPopup(_socketManager.resultData.payload.state.totalFreeSpinWin);
            _uiManager.UpdateBalance(_currentBalance);
            yield return new WaitUntil(() => winPopupAnimation.popupDone);
        }

        // All paths that reach here are truly done spinning
        _isSpinning = false;
        _currentBalance = _socketManager.resultData.player.balance;

        if (!_isAutoSpin && !_isFreeSpin)
        {
            _uiManager.SetSpinButtonReady();
        }
    }
    #endregion

    #region TweeningCode

    private void InitializeTweening(Transform slotTransform)
    {
        // Snap to top off-screen
        slotTransform.localPosition = new Vector2(slotTransform.localPosition.x, 1586.5f);

        Tween tween = slotTransform
            .DOLocalMoveY(0f, 0.3f)
            .SetLoops(-1, LoopType.Restart)
            .SetEase(Ease.Flash);           // accelerate in, decelerate out → feels weighted
                                            // .OnStepComplete(() =>
                                            // {
                                            //     // Called at the invisible teleport moment (top of each loop restart)
                                            //     // Safe to swap sprites here — the strip is off-screen at y=1786.5
                                            //     shuffleSlotImages(midTween: true);
                                            // });

        _alltweens.Add(tween);
    }

    private IEnumerator StopTweening(Transform slotTransform, int index, bool isStop)
    {
        if (!isStop)
        {
            // Wait for the current loop cycle to naturally complete
            // (the reel is at the top — off screen — at this moment)
            bool isComplete = false;
            _alltweens[index].OnStepComplete(() => isComplete = true);
            yield return new WaitUntil(() => isComplete);
        }

        _alltweens[index].Kill();

        // Start from just above the visible area, not from 600
        // This makes the landing feel like a natural continuation of the scroll
        slotTransform.localPosition = new Vector2(slotTransform.localPosition.x, 800.5f);

        // Smooth slide down with a gentle overshoot — reduce elastic strength
        _alltweens[index] = slotTransform
            .DOLocalMoveY(397f, 0.7f)        // SlotManager landing y (use 400f for BonusManager)
            .SetEase(Ease.OutBack)            // subtle overshoot, not a full elastic bounce
            .SetSpeedBased(false);

        if (!isStop)
            yield return new WaitForSeconds(0.15f);
        else
            yield return null;
    }

    private void KillAllTweens()
    {
        if (_alltweens.Count > 0)
        {
            for (int i = 0; i < _alltweens.Count; i++)
                _alltweens[i].Kill();
            _alltweens.Clear();
        }
    }

    #endregion

    #region MoonSymbolAnimation

    private IEnumerator MoonSymbolAnimation(Image slotImage)
    {
        _audioController.PlayMoonIconPop();
        Transform child = slotImage.transform.GetChild(2);
        Image img = child.GetComponent<Image>();
        img.preserveAspect = true;
        RectTransform childRect = child.GetComponent<RectTransform>();
        ImageAnimation imgAnim = child.GetComponent<ImageAnimation>();

        if (imgAnim == null) yield break;

        List<Sprite> introSprites;
        List<Sprite> loopSprites;
        float entryScale;
        Sprite currentSprite = img.sprite;

        if (img.sprite == _symbolSprites[9])          // Yellow Moon
        {
            introSprites = YellowMoonAnimation;
            loopSprites = YellowMoonLoopAnimation;
            entryScale = 1.6f;
        }
        else if (img.sprite == _symbolSprites[11])    // Purple Moon
        {
            introSprites = PurpleMoonAnimation;
            loopSprites = PurpleMoonLoopAnimation;
            entryScale = 1.73f;
        }
        else if (img.sprite == _symbolSprites[10])    // Boost Symbol
        {
            introSprites = BoostSymbolAnimation;
            loopSprites = BoostSymbolLoopAnimation;
            entryScale = 3f;
        }
        else if (img.sprite == _symbolSprites[12])    // Free Spin
        {
            introSprites = freeSpinStartAnimation;
            loopSprites = freeSpinLoopAnimation;
            entryScale = 1.15f;
        }
        else yield break;

        child.gameObject.SetActive(true);
        childRect.localScale = new Vector3(entryScale, entryScale, entryScale);

        // ── Play intro animation (one-shot) ──
        imgAnim.textureArray = introSprites;
        imgAnim.doLoopAnimation = false;
        imgAnim.AnimationSpeed = 9f;

        if (img.sprite == _symbolSprites[9])    // Yellow Moon
        {
            imgAnim.AnimationSpeed = 15f;
        }
        if (img.sprite == _symbolSprites[11])    // Purple Moon
        {
            imgAnim.AnimationSpeed = 15f;
        }
        if (img.sprite == _symbolSprites[12])    // Free Spin
        {
            imgAnim.AnimationSpeed = 33f;
        }
        if (img.sprite == _symbolSprites[10])    // Boost Spin
        {
            imgAnim.AnimationSpeed = 21f;
        }
        imgAnim.StartAnimation();

        // Wait for intro to finish
        yield return new WaitUntil(() =>
            imgAnim.currentAnimationState == ImageAnimation.ImageState.FINISHED);

        // ── Switch to loop animation ──
        if (currentSprite == _symbolSprites[10])
        {
            childRect.localScale = new Vector3(0.97f, 0.97f, 0.97f);
            imgAnim.textureArray = loopSprites;
            imgAnim.doLoopAnimation = true;
            imgAnim.StartAnimation();
        }
        if (currentSprite == _symbolSprites[11])
        {
            childRect.localScale = new Vector3(1f, 1f, 1f);
            img.sprite = currentSprite;
        }
        if (currentSprite == _symbolSprites[9])
        {
            childRect.localScale = new Vector3(1f, 1f, 1f);
            img.sprite = currentSprite;
        }

        if (currentSprite == _symbolSprites[12])      //Free Spin 
        {
            childRect.localScale = new Vector3(1f, 1f, 1f);
            // imgAnim.textureArray = freeSpinTriggerAnimation;
            // imgAnim.doLoopAnimation = false;
            // imgAnim.AnimationSpeed = 35f;
            img.sprite = _symbolSprites[12];
            //imgAnim.StartAnimation();
        }
    }

    private IEnumerator SymbolSize(Image slotImage, Sprite symbol)
    {
        //_audioController.PlayMoonIconPop();
        Transform child = slotImage.transform.GetChild(2);
        Image img = child.GetComponent<Image>();
        RectTransform childRect = child.GetComponent<RectTransform>();
        ImageAnimation imgAnim = child.GetComponent<ImageAnimation>();

        if (imgAnim == null) yield break;

        float entryScale;

        if (symbol == _symbolSprites[0])         // A
        {
            img.sprite = _symbolSprites[0];
            entryScale = 1f;
        }
        else if (symbol == _symbolSprites[1])     // K
        {
            img.sprite = _symbolSprites[1];
            entryScale = 1f;
        }
        else if (symbol == _symbolSprites[2])       //Q
        {
            img.sprite = _symbolSprites[2];
            entryScale = 1f;
        }
        else if (symbol == _symbolSprites[3])     // J
        {
            img.sprite = _symbolSprites[3];
            entryScale = 1f;
        }
        else if (symbol == _symbolSprites[4])     // Owl
        {
            img.sprite = _symbolSprites[4];
            entryScale = 1f;
        }
        else if (symbol == _symbolSprites[5])     // Cat
        {
            img.sprite = _symbolSprites[5];
            entryScale = 1f;
        }
        else if (symbol == _symbolSprites[6])     // Eagle
        {
            img.sprite = _symbolSprites[6];
            entryScale = 1f;
        }
        else if (symbol == _symbolSprites[7])     // Reindeer
        {
            img.sprite = _symbolSprites[7];
            entryScale = 1f;
        }
        else if (symbol == _symbolSprites[8])     // Wolf
        {
            img.sprite = _symbolSprites[8];
            entryScale = 1.4f;
        }
        else if (symbol == _symbolSprites[9])          // Yellow Moon
        {
            img.sprite = _symbolSprites[9];
            entryScale = 1f;
        }
        else if (symbol == _symbolSprites[11])    // Purple Moon
        {
            img.sprite = _symbolSprites[11];
            entryScale = 1f;
        }
        else if (symbol == _symbolSprites[10])    // Boost Symbol
        {
            img.sprite = _symbolSprites[10];
            entryScale = 1f;
        }
        else if (symbol == _symbolSprites[12])    // Free Spin
        {
            img.sprite = _symbolSprites[12];
            entryScale = 1f;
        }
        else yield break;


        slotImage.sprite = BlankSprite;
        child.gameObject.SetActive(true);
        img.preserveAspect = true;
        childRect.localScale = new Vector3(entryScale, entryScale, entryScale);

    }

    private void ResetAllSymbolAnimations()
    {
        for (int i = 0; i < _resultImages.Count; i++)
        {
            for (int j = 0; j < _resultImages[i].slotImages.Count; j++)
            {
                Transform child = _resultImages[i].slotImages[j].transform.GetChild(2);
                //child.GetComponent<RectTransform>().localScale = Vector3.one;

                Sprite img = child.GetComponent<Image>().sprite;

                ImageAnimation anim = child.GetComponent<ImageAnimation>();
                if (anim != null) anim.StopAnimation();
                child.GetComponent<Image>().sprite = img;

                //child.gameObject.SetActive(false);
            }
        }
    }

    #endregion

    #region MystryRevealedAnimation
    private IEnumerator MysteryRevealedAnimation(List<MysteryRevealed> mysteries)
    {
        for (int i = 0; i < mysteries.Count; i++)
        {
            int reelIndex = mysteries[i].reel;
            int positionIndex = mysteries[i].position;
            //_resultImages[reelIndex].slotImages[positionIndex].sprite = _symbolSprites[9];
            if (_resultImages[reelIndex].slotImages[positionIndex].transform.GetChild(2).GetComponent<Image>().sprite == _symbolSprites[11])
            {
                _resultImages[reelIndex].slotImages[positionIndex].transform.GetChild(2).GetComponent<RectTransform>().localScale = new Vector3(1.1f, 1.1f, 1.1f);
                ImageAnimation imgAnim = _resultImages[reelIndex].slotImages[positionIndex].transform.GetChild(2).GetComponent<ImageAnimation>();
                imgAnim.textureArray = PurpleToYellowTransitionAnimation;
                imgAnim.doLoopAnimation = false;
                imgAnim.AnimationSpeed = 25f;
                imgAnim.StartAnimation();
                yield return new WaitUntil(() => imgAnim.currentAnimationState == ImageAnimation.ImageState.FINISHED);
            }
            _resultImages[reelIndex].slotImages[positionIndex].sprite = _symbolSprites[9];
            _resultImages[reelIndex].slotImages[positionIndex].GetComponentInChildren<TMP_Text>().text =
                UIManager.ToSpriteString(mysteries[i].value, "F2");
            yield return new WaitForSeconds(0.7f);
        }
        isMystryRevealed = true;
    }
    #endregion

    #region FreeSpinAnimation

    private IEnumerator FreeSpinStartAnimation()
    {
        int totalSymbols = 0;
        int finishedSymbols = 0;

        // Pass 1 — start ALL trigger animations simultaneously
        for (int i = 0; i < _resultImages.Count; i++)
        {
            for (int j = 0; j < _resultImages[i].slotImages.Count; j++)
            {
                Transform child = _resultImages[i].slotImages[j].transform.GetChild(2);
                if (child.GetComponent<Image>().sprite != _symbolSprites[12]) continue;

                child.gameObject.SetActive(true);

                RectTransform childRect = child.GetComponent<RectTransform>();
                Image img = child.GetComponent<Image>();
                ImageAnimation imgAnim = child.GetComponent<ImageAnimation>();

                childRect.localScale = new Vector3(2.1f, 2.1f, 2.1f);
                imgAnim.textureArray = freeSpinTriggerAnimation;
                imgAnim.doLoopAnimation = false;
                imgAnim.AnimationSpeed = 35f;
                img.sprite = imgAnim.textureArray[0];
                imgAnim.StartAnimation();

                totalSymbols++;

                // Capture for the lambda — avoids the classic loop-variable closure bug
                ImageAnimation captured = imgAnim;
                StartCoroutine(WaitForFreeSpinAnim(captured, () => finishedSymbols++));
            }
        }

        // Wait for every symbol to finish before continuing
        if (totalSymbols > 0)
            yield return new WaitUntil(() => finishedSymbols >= totalSymbols);

        yield return new WaitForSeconds(2f);

        // Intro page
        TimerSkipped = false;   // reset in case it was set from a previous round
        FreeSpinIntroPage.SetActive(true);
        FreeSpinIntroPage.GetComponent<CanvasGroup>().DOFade(1f, 0.7f).SetEase(Ease.InOutSine);
        FreeSpinTextAnimationObject.GetComponent<ImageAnimation>().StartAnimation();
        FreeSpinMoonAnimationObject.GetComponent<ImageAnimation>().StartAnimation();

        int timer = 5;
        while (!TimerSkipped)
        {
            TimerText.text = timer.ToString();
            yield return new WaitForSeconds(1f);
            timer--;
            if (timer < 0) break;
        }

        FreeSpinIntroPage.GetComponent<CanvasGroup>().DOFade(0f, 0.5f).SetEase(Ease.InOutSine);
        FreeSpinIntroFinished = true;
        FreeSpinIntroPage.SetActive(false);
    }

    private IEnumerator WaitForFreeSpinAnim(ImageAnimation anim, System.Action onFinished)
    {
        yield return new WaitUntil(() => anim.currentAnimationState == ImageAnimation.ImageState.FINISHED);
        onFinished?.Invoke();
    }

    private void TimerSkipButtonClicked()
    {
        TimerSkipped = true;
    }

    #endregion
}

[Serializable]
public class SlotImage
{
    public List<Image> slotImages = new List<Image>(10);
}