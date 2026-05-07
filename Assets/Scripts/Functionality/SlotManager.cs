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
    [SerializeField] private Sprite[] _symbolSprites;  //images taken initially

    [Header("Slot Images")]
    [SerializeField] private List<SlotImage> _totalImages;     //class to store total images
    [SerializeField] internal List<SlotImage> _resultImages;     //class to store the result matrix
    [SerializeField] private List<SlotImage> _winSlotImages;     //class to store the winning line images

    [Header("Slots Transforms")]
    [SerializeField] private Transform[] _slotTransforms;

    [Header("Image Animation")]
    [SerializeField] private List<Sprite> freeSpinAnimation;

    [Header("UI Elements")]
    [SerializeField] private GameObject WinSlotsParent;

    [Header("Managers")]
    [SerializeField] private AudioController _audioController;
    [SerializeField] private UIManager _uiManager;
    [SerializeField] private SocketIOManager _socketManager;
    [SerializeField] private PayLineManager _paylineManager;
    [SerializeField] private AnimationManager animationManager;
    [SerializeField] private BonusManger bonusManger;

    [Header("Auto spin setting")]

    internal bool SocketConnected = false;
    internal bool _isAutoSpin = false;
    internal bool _checkPopups = false;

    private bool isMystryRevealed = false;
    private int freeSpinMultiplier;
    private int diamondCount;
    private double FSpayout;
    private Dictionary<int, List<int>> _winningLines = new Dictionary<int, List<int>>();
    private bool _wasAutoSpinOn;
    private List<Tween> _alltweens = new List<Tween>();
    private Coroutine _autoSpinRoutine = null;
    private Coroutine _freeSpinRoutine = null;
    private Coroutine _tweenRoutine;
    private Coroutine _loopLinesCoroutine;
    private Tween _balanceTween;
    private bool _isFreeSpin = false;
    private bool isFirstFreeSpin = false;
    private bool _isSpinning = false;
    private bool _checkSpinAudio = false;
    internal int _betCounter = 0;
    private double _currentBalance = 0;
    private double _currentLineBet = 0;
    private double _currentTotalBet = 0;
    private int _lines = 15;
    internal int _numberOfSlots = 5;          //number of columns
    private bool _stopSpinToggle;
    private float _spinDelay = 0.2f;
    private bool _isTurboOn;
    private bool _winningsAnimation = false;
    internal int freeSpinCount;
    internal double totalFSwin;

    #region Initial Functions

    private void Start()
    {
        RefreshTrails();
        shuffleSlotImages();
    }

    private void RefreshTrails()
    {
        // Pre-warm all TrailObjects so Unity fully initializes them before first use.
        // Without this, trails that were never active before fail silently on the first spin.
        for (int i = 0; i < _resultImages.Count; i++)
        {
            for (int j = 0; j < _resultImages[i].slotImages.Count; j++)
            {
                TrailObject trail = _resultImages[i].slotImages[j].gameObject.GetComponentInChildren<TrailObject>(true);
                if (trail != null)
                {
                    trail.gameObject.SetActive(true);   // wake Unity so Awake/Start run on the TrailObject
                    trail.gameObject.SetActive(false);  // immediately hide again
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
                Sprite image = _symbolSprites[UnityEngine.Random.Range(0, 10)];
                if (!midTween)
                {
                    _totalImages[i].slotImages[j].sprite = image;
                }
                else
                {
                    if (j == 10 || j == 11 || j == 12)
                    {
                        continue;
                    }
                    else
                    {
                        _totalImages[i].slotImages[j].sprite = image;
                    }
                }
            }
        }
    }

    // Resets all trail objects across all result slots before a new spin
    private void ResetAllTrails()
    {
        for (int i = 0; i < _resultImages.Count; i++)
        {
            for (int j = 0; j < _resultImages[i].slotImages.Count; j++)
            {
                TrailObject trail = _resultImages[i].slotImages[j].gameObject.GetComponentInChildren<TrailObject>(true);
                if (trail != null)
                {
                    trail.ResetTrail();
                }
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
        _audioController.PlayButtonAudio();
        if (_isAutoSpin)
        {
            _isAutoSpin = false;
            //if (!_isFreeSpin && !_socketManager.resultData.isFreeSpinTriggered && _wasAutoSpinOn)
            {
                _wasAutoSpinOn = false;
            }
            StartCoroutine(StopAutoSpinCoroutine());
        }
    }

    private IEnumerator AutoSpinCoroutine()
    {
        while (_isAutoSpin)
        {
            StartSlots(_isAutoSpin);
            // _tweenRoutine is the tracked handle — yield on it so we always wait
            // for the full spin, including any free spin or bonus chains it starts
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
        if (_autoSpinRoutine != null)
        {
            StopCoroutine(_autoSpinRoutine);
            _autoSpinRoutine = null;
        }
    }
    #endregion

    #region SlotSpin
    internal void StartSlots(bool autoSpin = false)
    {
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
        _isSpinning = true;     // set immediately so StopAutoSpinCoroutine and AutoSpinCoroutine don't unblock prematurely

        if (_currentBalance < _currentTotalBet && !_isFreeSpin)
        {
            StopAutoSpin();
            _isSpinning = false;
            yield return new WaitForSeconds(1);
            yield break;
        }

        _paylineManager.isDisplayingWinningLines = false;
        _uiManager.UpdateWin(0.00);
        // Reset all trails at the START of each spin.
        // yield return null gives Unity one frame to fully process SetActive(false)
        // before any trail gets activated again.
        ResetAllTrails();
        yield return null;

        if (_loopLinesCoroutine != null)
        {
            StopCoroutine(_loopLinesCoroutine);
            _loopLinesCoroutine = null;
        }
        WinSlotsParent.SetActive(false);

        if (_audioController) _audioController.PlayWLAudio("spin");

        _checkSpinAudio = true;
        _isSpinning = true;

        // Start spinning all slots and populate _alltweens
        for (int i = 0; i < _numberOfSlots; i++)
        {
            InitializeTweening(_slotTransforms[i]);
            yield return new WaitForSeconds(0.1f);
        }

        if (isFirstFreeSpin)
        {
            // first free spin animation
        }

        _socketManager.AccumulateResult(_betCounter);
        yield return new WaitUntil(() => _socketManager.isResultdone);
        _currentBalance = _socketManager.playerdata.balance;

        for (int j = 0; j < _socketManager.resultData.matrix.Count; j++)
        {
            for (int i = 0; i < _socketManager.resultData.matrix[j].Count; i++)
            {
                if (int.TryParse(_socketManager.resultData.matrix[j][i], out int symbolId))
                {
                    _resultImages[i].slotImages[j].sprite = _symbolSprites[symbolId];
                    _resultImages[i].slotImages[j].GetComponentInChildren<TMP_Text>().text = ""; // Clear any previous text (e.g. from mystery revealed)
                }
            }
        }

        if (_socketManager.resultData.payload.bs_count > 0)
        {
            foreach (var bs in _socketManager.resultData.payload.bs)
            {
                int reel = bs.reel;
                int position = bs.position;
                // if (bs.isJackpot)
                // {
                //     _resultImages[reel].slotImages[position].GetComponentInChildren<TMP_Text>().text = bs.jackpotName;
                // }
                // else
                if(_resultImages[reel].slotImages[position].sprite != _symbolSprites[11])
                {
                    _resultImages[reel].slotImages[position].GetComponentInChildren<TMP_Text>().text = bs.value.ToString();
                }
            }
        }

        if (_isTurboOn)
        {
            _stopSpinToggle = true;
        }
        else
        {
            for (int i = 0; i < 5; i++)
            {
                yield return new WaitForSeconds(0.1f);
                if (_stopSpinToggle)
                {
                    break;
                }
            }
        }

        int pendingTrails = 0;
        int finishedTrails = 0;

        for (int i = 0; i < _numberOfSlots; i++)
        {
            yield return StopTweening(_slotTransforms[i], i, _stopSpinToggle);
            for (int j = 0; j < _resultImages[i].slotImages.Count; j++)
            {
                if (_resultImages[i].slotImages[j].sprite == _symbolSprites[9] || _resultImages[i].slotImages[j].sprite == _symbolSprites[10]
                    || _resultImages[i].slotImages[j].sprite == _symbolSprites[11])
                {
                    TrailObject trail = _resultImages[i].slotImages[j].gameObject.GetComponentInChildren<TrailObject>(true);
                    if (trail != null)
                    {
                        pendingTrails++;
                        StartCoroutine(trail.StartLogoAnimation(() => finishedTrails++));
                    }
                }
            }
        }

        _stopSpinToggle = false;
        if (_audioController) _audioController.StopWLAaudio();
        yield return _alltweens[^1].WaitForCompletion();
        KillAllTweens();

        // Wait for every logo trail to complete before moving on
        if (pendingTrails > 0)
        {
            yield return new WaitUntil(() => finishedTrails >= pendingTrails);
        }



        if (_socketManager.resultData.payload.is_boost)
        {
            ResetAllTrails();
            //yield return new WaitForSeconds(0.5f);
            animationManager.isBoostAnimationFinished = false;
            StartCoroutine(animationManager.BoostAnimation(_socketManager.resultData.payload.boost_positions));
            yield return new WaitUntil(() => animationManager.isBoostAnimationFinished);
            Debug.Log("Boost Animation Finished");
            if (_socketManager.resultData.payload.mysteryRevealed != null && _socketManager.resultData.payload.mysteryRevealed.Count > 0)
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
                    if (_resultImages[i].slotImages[j].sprite == _symbolSprites[9])
                    {
                        animationManager.isBoostBlastAnimationFinished = false;
                        double boostAmount = double.Parse(_resultImages[i].slotImages[j].gameObject.GetComponentInChildren<TMP_Text>().text);
                        // Get the TrailObject (searches inactive children too)
                        TrailObject trail = _resultImages[i].slotImages[j].gameObject.GetComponentInChildren<TrailObject>(true);
                        if (trail != null)
                        {
                            StartCoroutine(trail.StartBoostAnimation(boostAmount , _resultImages[i].slotImages[j].transform.GetChild(4).gameObject));
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
            //Boost Winnings shown
        }

        if (_socketManager.resultData.payload.state.mode == "HOLD_AND_WIN")
        {
            _isFreeSpin = false;
            bonusManger.isBonusFinished = false;
            StartCoroutine(bonusManger.StartBonus());
            yield return new WaitUntil(() => bonusManger.isBonusFinished);

            if (_socketManager.resultData.payload.state.freeSpinsLeft > 0)
            {
                _isFreeSpin = true;
                _uiManager.ToggleFreeSpinUI(true, _socketManager.resultData.payload.state.freeSpinsLeft);
                _tweenRoutine = StartCoroutine(TweenRoutine());
            }
            else
            {
                _isFreeSpin = false;
                _uiManager.ToggleFreeSpinUI(false, 0);
                _isSpinning = false;
                _uiManager.OnStopSpinButtonPressed();
            }
            yield break;
        }

        if (_socketManager.resultData.payload.lineWins.Count > 0)
        {
            _paylineManager.DisplayWinningLines(_socketManager.resultData.payload.lineWins);
            _uiManager.UpdateWin(_socketManager.resultData.payload.winAmount);
        }

        if (_socketManager.resultData.payload.freeSpinTriggered && !_isFreeSpin)
        {
            FreeSpinStartAnimation();
        }

        if (_socketManager.resultData.payload.state.mode == "FREE_SPINS" && _socketManager.resultData.payload.state.freeSpinsLeft > 0)
        {
            isFirstFreeSpin = !_isFreeSpin;
            _isFreeSpin = true;
            _uiManager.ToggleFreeSpinUI(true, _socketManager.resultData.payload.state.freeSpinsLeft);
            // Chain through _tweenRoutine so AutoSpinCoroutine's WaitUntil(!_isSpinning)
            // stays blocked for the entire free spin sequence
            _tweenRoutine = StartCoroutine(TweenRoutine());
            yield break;    // this coroutine hands off to the new one — return now
        }
        else if (_isFreeSpin && _socketManager.resultData.payload.state.freeSpinsLeft == 0)
        {
            _isFreeSpin = false;
            isFirstFreeSpin = false;
            _uiManager.ToggleFreeSpinUI(false, 0);
        }

        // All paths that reach here are truly done spinning
        _isSpinning = false;

        if (!_isAutoSpin && !_isFreeSpin)
        {
            _uiManager.OnStopSpinButtonPressed();
        }
    }
    #endregion

    #region TweeningCode
    private void InitializeTweening(Transform slotTransform)
    {
        slotTransform.localPosition = new Vector2(slotTransform.localPosition.x, 1786.5f);
        Tween tween = slotTransform.DOLocalMoveY(0f, 0.5f).SetLoops(-1, LoopType.Restart).SetEase(Ease.Linear);
        _alltweens.Add(tween);
    }

    private IEnumerator StopTweening(Transform slotTransform, int index, bool isStop)
    {
        if (!isStop)
        {
            bool isComplete = false;
            _alltweens[index].OnStepComplete(() => isComplete = true);
            yield return new WaitUntil(() => isComplete);
        }
        _alltweens[index].Kill();
        slotTransform.localPosition = new Vector2(slotTransform.localPosition.x, 101f);
        _alltweens[index] = slotTransform.DOLocalMoveY(397f, 1f).SetEase(Ease.OutElastic);
        if (!isStop)
        {
            yield return new WaitForSeconds(0.2f);
        }
        else
        {
            yield return null;
        }
    }

    private void KillAllTweens()
    {
        if (_alltweens.Count > 0)
        {
            for (int i = 0; i < _alltweens.Count; i++)
            {
                _alltweens[i].Kill();
            }
            _alltweens.Clear();
        }
    }
    #endregion

    #region WinningLines

    // private IEnumerator WinLineAnimation(List<Win> wins)
    // {
    //     WinSlotsParent.SetActive(true);
    //     while (true)
    //     {
    //         for (int i = 0; i < wins.Count; i++)
    //         {
    //             for (int j = 0; j < _winSlotImages[wins[i].positions[j]].slotImages.Count; j++)
    //             {
    //                 _winSlotImages[wins[i].positions[j]].slotImages[j].gameObject.SetActive(true);
    //             }
    //             yield return new WaitForSeconds(1f);
    //             for (int j = 0; j < _winSlotImages[wins[i].positions[j]].slotImages.Count; j++)
    //             {
    //                 _winSlotImages[wins[i].positions[j]].slotImages[j].gameObject.SetActive(false);
    //             }
    //         }
    //     }
    // }

    #endregion

    #region MystryRevealedAnimation
    private IEnumerator MysteryRevealedAnimation(List<MysteryRevealed> mysteries)
    {
        for (int i = 0; i < mysteries.Count; i++)
        {
            int reelIndex = mysteries[i].reel;
            int positionIndex = mysteries[i].position;
            _resultImages[reelIndex].slotImages[positionIndex].sprite = _symbolSprites[9];
            _resultImages[reelIndex].slotImages[positionIndex].GetComponentInChildren<TMP_Text>().text = mysteries[i].value.ToString();
            yield return new WaitForSeconds(0.7f);
        }
        isMystryRevealed = true;
    }
    #endregion

    #region FreeSpinAnimation

    private void FreeSpinStartAnimation()
    {
        // Implement any specific animations or UI changes that should occur when free spins start.
        // This could include things like showing a "Free Spins Activated!" banner, playing a sound effect, etc.
    }

    #endregion
}

[Serializable]
public class SlotImage
{
    public List<Image> slotImages = new List<Image>(10);
}