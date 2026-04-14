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
    [SerializeField] private List<SlotImage> _resultImages;     //class to store the result matrix
    [SerializeField] private List<SlotImage> _winSlotImages;     //class to store the winning line images

    [Header("Slots Transforms")]
    [SerializeField] private Transform[] _slotTransforms;

    [Header("UI Elements")]
    [SerializeField] private GameObject WinSlotsParent;

    [Header("Managers")]
    [SerializeField] private AudioController _audioController;
    [SerializeField] private UIManager _uiManager;
    [SerializeField] private SocketIOManager _socketManager;
    [SerializeField] private PayLineManager _paylineManager;

    [Header("Auto spin setting")]

    internal bool SocketConnected = false;
    internal bool _isAutoSpin = false;
    internal bool _checkPopups = false;

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
    private bool _isSpinning = false;
    private bool _checkSpinAudio = false;
    internal int _betCounter = 0;
    private double _currentBalance = 0;
    private double _currentLineBet = 0;
    private double _currentTotalBet = 0;
    private int _lines = 15;
    private int _numberOfSlots = 5;          //number of columns
    private bool _stopSpinToggle;
    private float _spinDelay = 0.2f;
    private bool _isTurboOn;
    private bool _winningsAnimation = false;
    internal int freeSpinCount;
    internal double totalFSwin;

    #region Initial Functions
    private void shuffleSlotImages(bool midTween = false)
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

    private void ResultSlotImages(bool midTween = false)
    {
        for (int i = 0; i < _resultImages.Count; i++)
        {
            for (int j = 0; j < _resultImages[i].slotImages.Count; j++)
            {
                Sprite image = _symbolSprites[UnityEngine.Random.Range(0, 10)];

                _resultImages[i].slotImages[j].sprite = image;

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
            // if (_autoSpinStopButton) _autoSpinStopButton.gameObject.SetActive(true);
            // if (_autoSpinButton) _autoSpinButton.gameObject.SetActive(false);

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
            if (!_isFreeSpin && !_socketManager.resultData.isFreeSpinTriggered && _wasAutoSpinOn)
            {
                _wasAutoSpinOn = false;
            }
            // if (_autoSpinStopButton) _autoSpinStopButton.gameObject.SetActive(false);
            // if (_autoSpinButton) _autoSpinButton.gameObject.SetActive(true);
            StartCoroutine(StopAutoSpinCoroutine());
        }
    }

    private IEnumerator AutoSpinCoroutine()
    {
        while (_isAutoSpin)
        {
            StartSlots(_isAutoSpin);
            yield return _tweenRoutine;
            yield return new WaitForSeconds(_spinDelay);
        }
        if (_wasAutoSpinOn)
            _wasAutoSpinOn = false;
    }

    private IEnumerator StopAutoSpinCoroutine()
    {
        yield return new WaitUntil(() => !_isSpinning);
        // ToggleButtonGrp(true);
        if (_autoSpinRoutine != null || _tweenRoutine != null)
        {
            StopCoroutine(_autoSpinRoutine);
            StopCoroutine(_tweenRoutine);
            _tweenRoutine = null;
            _autoSpinRoutine = null;
            StopCoroutine(StopAutoSpinCoroutine());
        }
    }
    #endregion

    #region SlotSpin
    //starts the spin process
    internal void StartSlots(bool autoSpin = false)
    {
        // _totalWinText.text = "0.000";
        // if (_spinButton) _spinButton.interactable = false;
        // if (_audioController) _audioController.PlaySpinButtonAudio();

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
        // StopLoopCoroutine();
        _tweenRoutine = StartCoroutine(TweenRoutine());
    }

    //manage the Routine for spinning of the slots
    private IEnumerator TweenRoutine()
    {
        if (_currentBalance < _currentTotalBet && !_isFreeSpin)
        {
            // CompareBalance();
            StopAutoSpin();
            yield return new WaitForSeconds(1);
            // ToggleButtonGrp(true);
            yield break;
        }

        // win line/slot reset area
        StopCoroutine(_loopLinesCoroutine);
        _loopLinesCoroutine = null;
        WinSlotsParent.SetActive(false);
        //

        if (_audioController) _audioController.PlayWLAudio("spin");

        _checkSpinAudio = true;
        _isSpinning = true;
        // ToggleButtonGrp(false);

        // if (!_isFreeSpin)
        // {
        //     BalanceDeduction();
        // }
        // if (!_isTurboOn && !_isFreeSpin && !_isAutoSpin)
        // {
        //     _stopSpinButton.gameObject.SetActive(true);
        // }

        for (int i = 0; i < _numberOfSlots; i++)
        {
            InitializeTweening(_slotTransforms[i]);
            yield return new WaitForSeconds(0.1f);
        }

        // _socketManager.AccumulateResult(_betCounter);
        // yield return new WaitUntil(() => _socketManager.isResultdone);
        // _currentBalance = _socketManager.playerdata.balance;

        // for (int j = 0; j < _socketManager.resultData.matrix.Count; j++)
        // {
        //     for (int i = 0; i < _socketManager.resultData.matrix[j].Count; i++)
        //     {
        //         if (int.TryParse(_socketManager.resultData.matrix[j][i], out int symbolId))
        //         {
        //             _resultImages[i].slotImages[j].sprite = _symbolSprites[symbolId];
        //         }
        //     }
        // }

        ResultSlotImages();

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
            // _stopSpinButton.gameObject.SetActive(false);
        }

        for (int i = 0; i < _numberOfSlots; i++)
        {
            yield return StopTweening(_slotTransforms[i], i, _stopSpinToggle);
        }
        _stopSpinToggle = false;
        if (_audioController) _audioController.StopWLAaudio();
        yield return _alltweens[^1].WaitForCompletion();
        KillAllTweens();
        //shuffleSlotImages(true);

        if (_socketManager.resultData.payload.wins.Count > 0)
        {
            _loopLinesCoroutine = StartCoroutine(WinLineAnimation(_socketManager.resultData.payload.wins));
        }

        // if (_socketManager.resultData.diamondCount != diamondCount && _isFreeSpin)
        // {
        //     bool found = false;
        //     for (int i = 0; i < _resultImages.Count; i++)
        //     {
        //         for (int j = 0; j < _resultImages[i].slotImages.Count; j++)
        //         {
        //             if (int.TryParse(_socketManager.resultData.matrix[j][i], out int symbolId) && symbolId == 8)
        //             {
        //                 found = true;
        //                 yield return _uiManager.DiamondAnimation(_resultImages[i].slotImages[j].transform.position, _socketManager.resultData.diamondCount, _socketManager.resultData.diamondMultiplier);
        //                 break;
        //             }
        //         }
        //         if (found)
        //         {
        //             break;
        //         }
        //     }

        //     // if (FSpayout != _socketManager.resultData.payload.winAmount)
        //     // {
        //     //     // FSTotalWinnnings_Text.text = "Total Win\n" + totalFSwin.ToString("F3");
        //     //     _totalWinText.text = (_socketManager.resultData.payload.winAmount - FSpayout).ToString("F3");
        //     // }
        //     FSpayout = _socketManager.resultData.payload.winAmount;
        //     diamondCount = _socketManager.resultData.diamondCount;
        //     freeSpinMultiplier = _socketManager.resultData.diamondMultiplier;
        // }

        // if (_socketManager.resultData.payload.wins.Count > 0)
        // {
        //     StartCoroutine(WinningLines(_socketManager.resultData.payload.wins));
        // }
        // else
        // {
        //     _winningsAnimation = true;
        // }

        // yield return new WaitUntil(() => _winningsAnimation);

        // if (_isAutoSpin || _isFreeSpin || _socketManager.resultData.isFreeSpinTriggered || _socketManager.resultData.freeSpinCount > 0)
        // {
        //     StopLoopCoroutine();
        // }


        // if (!_isFreeSpin)
        // {
        //     CheckWinPopups();
        //     yield return new WaitUntil(() => !_checkPopups);
        // }

        // if (_socketManager.resultData.isFreeSpinTriggered)
        // {
        //     // _uiManager.FreeSpins += _socketManager.resultData.freeSpinCount;
        //     // _uiManager.FreeSpins = freeSpinCount;
        //     yield return FreeSpinSymbolLoop();
        //     if (_isAutoSpin)
        //     {
        //         _isAutoSpin = false;
        //         _wasAutoSpinOn = true;
        //         if (_autoSpinStopButton.gameObject.activeSelf)
        //         {
        //             _autoSpinStopButton.gameObject.SetActive(false);
        //             _autoSpinButton.interactable = false;
        //             _autoSpinButton.gameObject.SetActive(true);
        //         }
        //         StopCoroutine(_autoSpinRoutine);
        //         _autoSpinRoutine = null;
        //         yield return new WaitForSeconds(0.1f);
        //     }
        //     if (_isFreeSpin)
        //     {
        //         _isFreeSpin = false;
        //         if (_freeSpinRoutine != null)
        //         {
        //             StopCoroutine(_freeSpinRoutine);
        //             _freeSpinRoutine = null;
        //         }
        //     }
        //     freeSpinMultiplier = _socketManager.resultData.diamondMultiplier;    //////////////
        //     diamondCount = _socketManager.resultData.diamondCount;
        //     FSpayout = _socketManager.resultData.payload.winAmount;      ///////////
        //     _uiManager.FreeSpinProcess((int)_socketManager.resultData.freeSpinCount);
        //     // _uiManager.FreeSpinProcess(freeSpinCount);
        // }

        // if (_socketManager.resultData.payload.winAmount > 0)
        // {
        //     _bottomBarText.text = "YOU WON: " + _socketManager.resultData.payload.winAmount.ToString("F3");
        //     _spinDelay = 1.2f;
        // }
        // else
        // {
        //     _bottomBarText.text = "CLICK PLAY TO START!";
        //     _spinDelay = 0.2f;
        // }
        // if (_totalWinText) _totalWinText.text = _socketManager.resultData.payload.winAmount.ToString("F3");
        // _balanceTween?.Kill();
        // if (_balanceText) _balanceText.text = _socketManager.playerdata.balance.ToString("F3");

        if (!_isAutoSpin && !_isFreeSpin)
        {
            // ToggleButtonGrp(true);
            _isSpinning = false;
            _uiManager.OnStopSpinButtonPressed();
        }
        else
        {
            _isSpinning = false;
        }
    }
    #endregion
    #region TweeningCode
    private void InitializeTweening(Transform slotTransform)
    {
        slotTransform.localPosition = new Vector2(slotTransform.localPosition.x, 1786.5f);
        Tween tween = slotTransform.DOLocalMoveY(-397f, 0.2f).SetLoops(-1, LoopType.Restart);
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
        _alltweens[index] = slotTransform.DOLocalMoveY(0f, 0.5f).SetEase(Ease.OutElastic);
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

    private IEnumerator WinLineAnimation(List<Win> wins)
    {
        WinSlotsParent.SetActive(true);
        while (true)
        {
            for (int i = 0; i < wins.Count; i++)
            {
                for (int j = 0; j < _winSlotImages[wins[i].positions[j]].slotImages.Count; j++)
                {
                    _winSlotImages[wins[i].positions[j]].slotImages[j].gameObject.SetActive(true);
                    //start image animation
                }
                yield return new WaitForSeconds(1f);
                for (int j = 0; j < _winSlotImages[wins[i].positions[j]].slotImages.Count; j++)
                {
                    _winSlotImages[wins[i].positions[j]].slotImages[j].gameObject.SetActive(false);
                    //reset image animation
                }
            }
        }
    }

    #endregion
}

[Serializable]
public class SlotImage
{
    public List<Image> slotImages = new List<Image>(10);
}