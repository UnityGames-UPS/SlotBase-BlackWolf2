using Unity;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using FluffyUnderware.Curvy;
using FluffyUnderware.Curvy.Controllers;
using UnityEngine.UI;
using TMPro;
using System;
using DG.Tweening;

public class BonusManger : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private SocketIOManager socketManager;
    [SerializeField] private SlotManager slotManager;
    [SerializeField] private UIManager uIManager;
    [SerializeField] private AnimationManager animationManager;
    [SerializeField] private WinPopupAnimation winPopupAnimation;
    [SerializeField] private AudioController audioController;

    [Header("Bonus Intro Animation")]
    [SerializeField] private GameObject MainBG;
    [SerializeField] private GameObject WolfImage;
    [SerializeField] private Vector2 WolfFinalPosition;
    [SerializeField] private GameObject MoonImage;
    [SerializeField] private Vector2 MoonFinalPosition;
    [SerializeField] private GameObject LeftTree;
    [SerializeField] private Vector2 LeftTreeFinalPosition;
    [SerializeField] private GameObject RightTree;
    [SerializeField] private Vector2 RightTreeFinalPosition;

    [Header("Bonus Intro Page")]
    [SerializeField] private GameObject BonusIntroPage;
    [SerializeField] private GameObject BonusGameTextObject;
    [SerializeField] private GameObject BoostSymbol;
    [SerializeField] private GameObject PurpleMoonSymbol;
    [SerializeField] private GameObject TransitionAnimationObject;
    [SerializeField] private TMP_Text TimerText;
    [SerializeField] private Button TimerSkipButton;

    [Header("Prefabs")]
    [SerializeField] private GameObject SlideObject;

    [Header("Slide GameObject Position")]
    [SerializeField] private List<RectTransform> slideObjectPositions;
    [SerializeField] private GameObject SlideParentObject;
    [SerializeField] private GameObject FirstSlideOverlayImage;
    [SerializeField] private GameObject SlideAnimationParent;
    [SerializeField] private Sprite SlideFirstMoonImage;
    [SerializeField] private Sprite SlideSecondMoonImage;
    [SerializeField] private Sprite SlideThirdMoonImage;
    [SerializeField] private Sprite SliteOtherMoonImage;
    [SerializeField] private Sprite MiniMoonImage;
    [SerializeField] private Sprite MinorMoonImage;
    [SerializeField] private Sprite MajorMoonImage;
    [SerializeField] private Sprite BoostMoonImage;

    [Header("Bonus Paths")]
    [SerializeField] private List<BonusPath> BonusPaths;

    [Header("Slot Objects")]
    [SerializeField] private Sprite[] slotImages;
    [SerializeField] private List<SlotImage> totalImages;
    [SerializeField] internal List<SlotImage> resultImages;
    [SerializeField] private List<SlotImage> winSlotImages;
    [SerializeField] private List<SlotImage> MaskImages;
    [SerializeField] private Transform[] _slotTransforms;

    [SerializeField] private GameObject NormalSlotPanel;
    [SerializeField] private GameObject BonusSlotPanel;

    [Header("Other UI Elements")]
    [SerializeField] private GameObject MultiplierPanel;
    [SerializeField] internal TMP_Text MultiplierText;
    [SerializeField] private GameObject SpinIndicatorObject;
    [SerializeField] private List<GameObject> spinIndicators;
    [SerializeField] private Button BonusStartButton;

    private List<GameObject> currentSlideObjects = new List<GameObject>();
    private List<Tween> _alltweens = new List<Tween>();
    private bool _stopSpinToggle = true;

    private int _numberOfSlots = 20;
    internal bool isBonusFinished = false;
    private bool isBonusIntroAnimationFinished = false;
    private bool isSlideAnimationFinished = false;
    private bool isSlideFinished = false;
    private bool isFortuneSlideAnimationFinished = false;
    private bool isBonusMultiplierAnimationFinished = false;
    private bool TimerSkipped = false;

    private int _slideQueueConsumedCount = 0;

    private List<string> _snapshotSlideQueue = new List<string>();

    void Start()
    {
        shuffleSlotImages();
        TimerSkipButton.onClick.AddListener(TimerSkipButtonClicked);
        //SpinIndicatorFrontAnimation(spinIndicators[2]);
    }

    private Sprite GetSpriteForSlideIndex(int index)
    {
        return index switch
        {
            0 => SlideFirstMoonImage,
            1 => SlideSecondMoonImage,
            2 => SlideThirdMoonImage,
            _ => SliteOtherMoonImage
        };
    }

    private Sprite GetJackpotSprite(string queueValue)
    {
        if (string.IsNullOrEmpty(queueValue)) return null;
        return queueValue.ToUpperInvariant() switch
        {
            "MINI" => MiniMoonImage,
            "MINOR" => MinorMoonImage,
            "MAJOR" => MajorMoonImage,
            "BOOST" => BoostMoonImage,
            _ => null
        };
    }

    private void ConfigureSlide(GameObject slideObj, int stackIndex, int queueIndex)
    {
        string queueValue = (queueIndex >= 0 && queueIndex < _snapshotSlideQueue.Count)
                            ? _snapshotSlideQueue[queueIndex] : null;


        Image slideImage = slideObj.GetComponent<Image>();
        if (slideImage != null)
        {
            Sprite jackpotSprite = GetJackpotSprite(queueValue);
            slideImage.sprite = jackpotSprite != null ? jackpotSprite : GetSpriteForSlideIndex(stackIndex);
        }

        TMP_Text valueText = slideObj.GetComponentInChildren<TMP_Text>(true);
        if (valueText != null)
        {
            if (!string.IsNullOrEmpty(queueValue))
            {
                Sprite jp = GetJackpotSprite(queueValue);
                valueText.text = jp != null ? "" : UIManager.ToSpriteString(queueValue);
            }
            else
            {
                valueText.text = "";
            }
        }

        TrailRenderer tr = slideObj.GetComponentInChildren<TrailRenderer>(true);
        if (tr != null)
            tr.enabled = false;
    }

    private void RefreshSlideSprites()
    {
        for (int i = 0; i < currentSlideObjects.Count; i++)
        {
            Image img = currentSlideObjects[i].GetComponent<Image>();
            if (img == null) continue;

            int queueIndex = _slideQueueConsumedCount + i;
            string queueValue = (queueIndex >= 0 && queueIndex < _snapshotSlideQueue.Count)
                                ? _snapshotSlideQueue[queueIndex] : null;

            Sprite jackpotSprite = GetJackpotSprite(queueValue);
            img.sprite = jackpotSprite != null ? jackpotSprite : GetSpriteForSlideIndex(i);
        }
    }

    private IEnumerator BonusIntroAnimation()
    {
        // Reset starting state
        MoonImage.GetComponent<RectTransform>().localPosition = new Vector2(540f, 1000f);
        WolfImage.GetComponent<RectTransform>().localPosition = new Vector2(46f, -100f);
        LeftTree.GetComponent<RectTransform>().localPosition = new Vector2(-900f, 100f);
        RightTree.GetComponent<RectTransform>().localPosition = new Vector2(900f, 100f);
        MoonImage.GetComponent<RectTransform>().localScale = new Vector2(0.6f, 0.6f);
        WolfImage.GetComponent<RectTransform>().localScale = new Vector2(0.8f, 0.8f);
        WolfImage.GetComponent<CanvasGroup>().alpha = 0f;
        MoonImage.GetComponent<CanvasGroup>().alpha = 0f;
        LeftTree.GetComponent<CanvasGroup>().alpha = 0f;
        RightTree.GetComponent<CanvasGroup>().alpha = 0f;

        Sequence seq = DOTween.Sequence();

        seq.Append(MainBG.transform.DOLocalMoveY(-1080f, 2.2f).SetEase(Ease.InOutSine));
        audioController.PlayWolfAppear();

        seq.Insert(0.8f, WolfImage.GetComponent<CanvasGroup>().DOFade(1f, 1.2f).SetEase(Ease.InOutSine));
        seq.Insert(0.8f, MoonImage.GetComponent<CanvasGroup>().DOFade(1f, 1.2f).SetEase(Ease.InOutSine));
        seq.Insert(0.8f, LeftTree.GetComponent<CanvasGroup>().DOFade(1f, 1.2f).SetEase(Ease.InOutSine));
        seq.Insert(0.8f, RightTree.GetComponent<CanvasGroup>().DOFade(1f, 1.2f).SetEase(Ease.InOutSine));
        seq.Insert(0.8f, WolfImage.transform.DOScale(1f, 1.2f).SetEase(Ease.OutCubic));
        seq.Insert(0.8f, MoonImage.transform.DOScale(1f, 1.2f).SetEase(Ease.OutCubic));
        seq.Insert(0.8f, WolfImage.transform.DOLocalMove(WolfFinalPosition, 1.2f).SetEase(Ease.InOutSine));
        seq.Insert(0.8f, MoonImage.transform.DOLocalMove(MoonFinalPosition, 1.2f).SetEase(Ease.InOutSine));
        seq.Insert(0.8f, LeftTree.transform.DOLocalMove(LeftTreeFinalPosition, 1.2f).SetEase(Ease.InOutSine));
        seq.Insert(0.8f, RightTree.transform.DOLocalMove(RightTreeFinalPosition, 1.2f).SetEase(Ease.InOutSine));

        yield return seq.WaitForCompletion();
        uIManager.ToggleFreeSpinUI(false);
        yield return new WaitForSeconds(2f);

        BonusIntroPage.SetActive(true);
        BonusIntroPage.GetComponent<CanvasGroup>().DOFade(1f, 1f).SetEase(Ease.InOutSine);
        //BonusGameTextObject.GetComponent<ImageAnimation>().StartAnimation();
        //PurpleMoonSymbol.GetComponent<ImageAnimation>().StartAnimation();
        BoostSymbol.GetComponent<ImageAnimation>().StartAnimation();
        TransitionAnimationObject.GetComponent<ImageAnimation>().StartAnimation();

        int timer = 5;
        while (!TimerSkipped)
        {
            TimerText.text = timer.ToString();
            yield return new WaitForSeconds(1f);
            timer--;
            if (timer < 0) break;
        }

        BonusIntroPage.GetComponent<CanvasGroup>().DOFade(0f, 0.5f).SetEase(Ease.InOutSine);
        yield return new WaitForSeconds(0.3f);
        audioController.PlayBonusBackground();
        MainBG.GetComponent<RectTransform>().localPosition = new Vector2(0f, 0f);
        BonusIntroPage.SetActive(false);
        isBonusIntroAnimationFinished = true;
    }

    private void TimerSkipButtonClicked()
    {
        TimerSkipped = true;
    }

    internal IEnumerator StartBonus()
    {
        isBonusIntroAnimationFinished = false;
        StartCoroutine(BonusIntroAnimation());

        yield return new WaitForSeconds(3f);
        InitializeBonusUI();
        uIManager.ToggleBonusBackground();
        yield return new WaitUntil(() => isBonusIntroAnimationFinished);
        
        yield return new WaitForSeconds(1f);

        int currentSpinCount = socketManager.resultData.payload.state.respinsLeft;
        while (socketManager.resultData.payload.state.respinsLeft > 0)
        {
            for (int i = 0; i < _numberOfSlots; i++)
            {
                InitializeTweening(_slotTransforms[i]);
                yield return new WaitForSeconds(0.1f);
            }

            if (currentSpinCount > 0)
            {
                SpinIndicatorBackAnimation(spinIndicators[currentSpinCount - 1]);
                currentSpinCount--;
                yield return new WaitForSeconds(0.5f);
            }

            socketManager.AccumulateResult(uIManager.betCounter);
            yield return new WaitUntil(() => socketManager.isResultdone);

            for (int j = 0; j < socketManager.resultData.matrix.Count; j++)
            {
                for (int i = 0; i < socketManager.resultData.matrix[j].Count; i++)
                {
                    if (int.TryParse(socketManager.resultData.matrix[j][i], out int symbolId))
                    {
                        resultImages[i].slotImages[j].sprite = slotImages[symbolId];
                        resultImages[i].slotImages[j].GetComponentInChildren<TMP_Text>().text = "";
                    }
                    if (socketManager.resultData.matrix[j][i] == "Blank")
                    {
                        resultImages[i].slotImages[j].sprite = slotImages[slotImages.Length - 1];
                    }
                }
            }

            if (socketManager.resultData.payload.bs_count > 0)
            {
                foreach (var bs in socketManager.resultData.payload.bs)
                {
                    int reel = bs.reel;
                    int position = bs.position;
                    resultImages[reel].slotImages[position].GetComponentInChildren<TMP_Text>().text =
                        UIManager.ToSpriteString(bs.value);
                }
            }

            for (int i = 0; i < _numberOfSlots; i++)
            {
                yield return StopTweening(_slotTransforms[i], i, _stopSpinToggle);
            }
            audioController.PlayReelHit();

            _stopSpinToggle = true;

            int spinsAfter = socketManager.resultData.payload.state.respinsLeft;

            if (spinsAfter > currentSpinCount)
            {
                for (int i = currentSpinCount; i < spinsAfter && i < spinIndicators.Count; i++)
                {
                    SpinIndicatorFrontAnimation(spinIndicators[i]);
                    yield return new WaitForSeconds(0.6f);
                }
                currentSpinCount = spinsAfter;
            }

            yield return _alltweens[^1].WaitForCompletion();
            KillAllTweens();

            isFortuneSlideAnimationFinished = false;
            StartCoroutine(FortuneSlideAnimation());
            yield return new WaitUntil(() => isFortuneSlideAnimationFinished);

            {
                var newQueue = socketManager.resultData.payload.state.fortuneSlideQueue;
                if (newQueue != null)
                {
                    for (int ni = 0; ni < newQueue.Count; ni++)
                    {
                        int snapIdx = _slideQueueConsumedCount + ni;
                        if (snapIdx < _snapshotSlideQueue.Count)
                        {
                            // Overwrite existing unconsumed entry
                            _snapshotSlideQueue[snapIdx] = newQueue[ni];
                        }
                        else
                        {
                            // Brand-new entry — append it
                            _snapshotSlideQueue.Add(newQueue[ni]);
                        }
                    }
                    // Refresh visible slides so sprites/text reflect synced snapshot
                    RefreshSlideSprites();
                }
            }
            // ─────────────────────────────────────────────────────────────────

            RefreshLockedSymbols();

            if (socketManager.resultData.payload.is_boost)
            {
                yield return StartCoroutine(BonusBoostSequence());
            }
            if (socketManager.resultData.payload.is_grand)
            {
                break;
            }
        }

        if (!socketManager.resultData.payload.is_grand)
        {
            // --- Bonus round finished ---
            animationManager.isBonusWolfAnimationFinished = false;
            animationManager.BonusWolfAnimation();
            yield return new WaitUntil(() => animationManager.isBonusWolfAnimationFinished);

            isBonusMultiplierAnimationFinished = false;
            StartCoroutine(MultiplierAnimation());
            yield return new WaitUntil(() => isBonusMultiplierAnimationFinished);
        }
        else
        {
            // --- Grand jackpot flow ---
            double grandWinAmount = socketManager.resultData.payload.grand_win;
            //winPopupAnimation.ShowGrandWinPopup(grandWinAmount);
            winPopupAnimation.ShowBonusWinPopup(grandWinAmount);
            yield return new WaitUntil(() => winPopupAnimation.popupDone);
        }

        foreach (GameObject slide in currentSlideObjects)
            Destroy(slide);
        currentSlideObjects.Clear();

        for (int i = 0; i < winSlotImages.Count; i++)
        {
            for (int j = 0; j < winSlotImages[i].slotImages.Count; j++)
            {
                winSlotImages[i].slotImages[j].gameObject.SetActive(false);
                winSlotImages[i].slotImages[j].GetComponentInChildren<TMP_Text>().text = "";
            }
        }

        BonusSlotPanel.SetActive(false);
        NormalSlotPanel.SetActive(true);
        MultiplierPanel.SetActive(false);
        SpinIndicatorObject.SetActive(false);
        uIManager.ToggleBonusBackground();

        isBonusFinished = true;
    }

    private IEnumerator BonusBoostSequence()
    {

        animationManager.isBonusBoostAnimationFinished = false;
        StartCoroutine(animationManager.BonusBoostAnimation(socketManager.resultData.payload.boost_positions));
        yield return new WaitUntil(() => animationManager.isBonusBoostAnimationFinished);


        for (int i = 0; i < socketManager.resultData.payload.boost_positions.Count; i++)
        {
            int reel = socketManager.resultData.payload.boost_positions[i].reel;
            int position = socketManager.resultData.payload.boost_positions[i].position;
            resultImages[reel].slotImages[position].sprite = slotImages[slotImages.Length - 1];
            winSlotImages[reel].slotImages[position].sprite = slotImages[slotImages.Length - 1];
            winSlotImages[reel].slotImages[position].GetComponentInChildren<TMP_Text>().text = "";
        }

        for (int i = 0; i < socketManager.resultData.payload.state.lockedSymbols.Count; i++)
        {
            if (!socketManager.resultData.payload.state.lockedSymbols[i].isBoost)
            {
                var sym = socketManager.resultData.payload.state.lockedSymbols[i];
                int reel = sym.reel;
                int position = sym.position;

                MaskImages[reel].slotImages[position].gameObject.SetActive(true);

                TrailObject trail = winSlotImages[reel].slotImages[position].GetComponentInChildren<TrailObject>(true);
                if (trail != null)
                {
                    trail.ResetTrail();
                    animationManager.isMultiplierAnimationFinished = false;
                    StartCoroutine(trail.StartMultiplierAnimation(sym.value, winSlotImages[reel].slotImages[position].gameObject));
                    audioController.PlayLightSound();
                    yield return new WaitUntil(() => animationManager.isMultiplierAnimationFinished);
                }
            }
        }

        double boostTotal = UIManager.FromSpriteString(animationManager.GetBonusBoostMultiplierText());
        winPopupAnimation.ShowBoostWinPopup(boostTotal, false);
        yield return new WaitUntil(() => winPopupAnimation.popupDone);

        MultiplierPanel.SetActive(false); // Hide the normal multiplier panel if it's still active

        for (int k = 0; k < MaskImages.Count; k++)
        {
            for (int j = 0; j < MaskImages[k].slotImages.Count; j++)
            {
                MaskImages[k].slotImages[j].gameObject.SetActive(false);
            }
        }

        animationManager.ResetBonusBoostAnimation();
    }

    private IEnumerator MultiplierAnimation()
    {
        MultiplierPanel.SetActive(true);
        MultiplierPanel.GetComponent<ImageAnimation>().StartAnimation();

        MultiplierText.text = UIManager.ToSpriteString(0.0, "F3");

        for (int i = 0; i < socketManager.resultData.payload.state.lockedSymbols.Count; i++)
        {
            int reel = socketManager.resultData.payload.state.lockedSymbols[i].reel;
            int position = socketManager.resultData.payload.state.lockedSymbols[i].position;
            TrailObject trail = winSlotImages[reel].slotImages[position].GetComponentInChildren<TrailObject>(true);

            trail.ResetTrail();

            MaskImages[reel].slotImages[position].gameObject.SetActive(true);

            animationManager.isMultiplierAnimationFinished = false;
            StartCoroutine(trail.StartMultiplierAnimation(socketManager.resultData.payload.state.lockedSymbols[i].value, winSlotImages[reel].slotImages[position].gameObject));
            audioController.PlayLightSound();
            yield return new WaitUntil(() => animationManager.isMultiplierAnimationFinished);
        }
        isBonusMultiplierAnimationFinished = true;

        double result = UIManager.FromSpriteString(MultiplierText.text);
        winPopupAnimation.ShowBonusWinPopup(result);
    }

    private IEnumerator FortuneSlideAnimation()
    {
        for (int i = 0; i < resultImages.Count; i++)
        {
            for (int j = 0; j < resultImages[i].slotImages.Count; j++)
            {
                if (resultImages[i].slotImages[j].sprite == slotImages[11] ||
                    resultImages[i].slotImages[j].sprite == slotImages[10])
                {
                    yield return new WaitForSeconds(0.4f);

                    isSlideFinished = false;

                    // Remove the front slide from the tracked list BEFORE the spline plays
                    GameObject frontSlide = currentSlideObjects[0];
                    currentSlideObjects.RemoveAt(0);

                    // Re-parent so it can travel freely above everything else
                    frontSlide.transform.SetParent(SlideAnimationParent.transform, true);
                    frontSlide.transform.SetAsLastSibling();

                    SplineController trail = frontSlide.GetComponent<SplineController>();

                    BonusPaths[i].splines[j].Refresh();
                    trail.Spline = BonusPaths[i].splines[j];
                    trail.AbsolutePosition = 0f;
                    trail.Speed = 2f;
                    trail.Clamping = CurvyClamping.Clamp;

                    trail.OnPositionReachedList.Clear();

                    int ci = i, cj = j;
                    GameObject capturedSlide = frontSlide;

                    var onReachedSettings = new OnPositionReachedSettings();
                    onReachedSettings.Position = 1f;
                    onReachedSettings.PositionMode = CurvyPositionMode.Relative;

                    onReachedSettings.Event.AddListener((CurvySplineMoveEventArgs args) =>
                    {
                        StartCoroutine(SlideMoveAnimation(capturedSlide, ci, cj));
                    });

                    trail.OnPositionReachedList.Add(onReachedSettings);

                    TrailRenderer tr = frontSlide.GetComponentInChildren<TrailRenderer>(true);
                    if (tr != null)
                        tr.enabled = true;

                    Image capturedImg = frontSlide.GetComponent<Image>();
                    TMP_Text capturedTxt = frontSlide.GetComponentInChildren<TMP_Text>(true);
                    Sprite slideSprite = capturedImg != null ? capturedImg.sprite : null;
                    string slideText = capturedTxt != null ? capturedTxt.text : "";

                    yield return new WaitForSeconds(1f);
                    Debug.Log("Start Playing");
                    trail.Play();
                    Debug.Log("Started Playing....");

                    yield return new WaitUntil(() => isSlideFinished);

                    if (slideSprite != null)
                    {
                        resultImages[ci].slotImages[cj].sprite = slideSprite;
                        TMP_Text resultText = resultImages[ci].slotImages[cj].GetComponentInChildren<TMP_Text>(true);
                        if (resultText != null)
                            resultText.text = slideText;
                    }

                    UpdateWinSlotImageFromSlide(ci, cj);

                    if (frontSlide != null)
                    {
                        TrailRenderer trAfter = frontSlide.GetComponentInChildren<TrailRenderer>(true);
                        if (trAfter != null)
                            trAfter.enabled = false;
                    }
                }
            }
        }
        isFortuneSlideAnimationFinished = true;
    }

    private void UpdateWinSlotImageFromSlide(int reelIndex, int posIndex)
    {
        int consumedQueueIndex = _slideQueueConsumedCount - 1;
        string queueValue = (consumedQueueIndex >= 0 && consumedQueueIndex < _snapshotSlideQueue.Count)
                            ? _snapshotSlideQueue[consumedQueueIndex] : null;

        Image winImg = winSlotImages[reelIndex].slotImages[posIndex];
        TMP_Text winText = winImg.GetComponentInChildren<TMP_Text>(true);

        Sprite jackpotSprite = GetJackpotSprite(queueValue);
        if (jackpotSprite != null)
        {
            winImg.sprite = jackpotSprite;
            if (winText != null) winText.text = "";
        }
        else
        {
            var lockedList = socketManager.resultData.payload.state.lockedSymbols;
            for (int k = 0; k < lockedList.Count; k++)
            {
                if (lockedList[k].reel == reelIndex && lockedList[k].position == posIndex)
                {
                    if (winText != null)
                        winText.text = UIManager.ToSpriteString(lockedList[k].value);
                    break;
                }
            }
        }
    }

    private IEnumerator SlideMoveAnimation(GameObject landedSlide, int reelIndex, int posIndex)
    {
        const float shiftDuration = 0.3f;

        animationManager.SlideSymbolFinishedAnimation(
            resultImages[reelIndex].slotImages[posIndex].gameObject);

        for (int i = 0; i < currentSlideObjects.Count; i++)
        {
            RectTransform rt = currentSlideObjects[i].GetComponent<RectTransform>();
            rt.DOAnchorPos(slideObjectPositions[i].anchoredPosition, shiftDuration).SetEase(Ease.OutQuad);
            rt.DOScale(ScaleForIndex(i), shiftDuration).SetEase(Ease.OutQuad);

            Image img = currentSlideObjects[i].GetComponent<Image>();
            TMP_Text txt = currentSlideObjects[i].GetComponentInChildren<TMP_Text>(true);

            int qi = _slideQueueConsumedCount + 1 + i; // +1 because front slide was already consumed
            string qv = (qi >= 0 && qi < _snapshotSlideQueue.Count) ? _snapshotSlideQueue[qi] : null;
            Sprite jp = GetJackpotSprite(qv);

            if (img != null)
                img.sprite = jp != null ? jp : GetSpriteForSlideIndex(i);

            if (txt != null)
            {
                if (!string.IsNullOrEmpty(qv))
                    txt.text = jp != null ? "" : UIManager.ToSpriteString(qv);
                else
                    txt.text = "";
            }
        }

        int newIndex = currentSlideObjects.Count;
        if (newIndex < slideObjectPositions.Count)
        {
            int queueIndex = _slideQueueConsumedCount + newIndex;

            GameObject newSlide = Instantiate(SlideObject, SlideParentObject.transform);
            newSlide.transform.SetSiblingIndex(0);
            RectTransform newRt = newSlide.GetComponent<RectTransform>();
            newRt.anchoredPosition = slideObjectPositions[newIndex].anchoredPosition;
            newRt.localScale = Vector3.one * ScaleForIndex(newIndex);

            ConfigureSlide(newSlide, newIndex, queueIndex);

            currentSlideObjects.Add(newSlide);
        }

        _slideQueueConsumedCount++;

        yield return null;
        Destroy(landedSlide);

        yield return new WaitForSeconds(shiftDuration);
        isSlideFinished = true;
    }

    private void SpinIndicatorBackAnimation(GameObject indicator)
    {
        ImageAnimation imgAnim = indicator.GetComponent<ImageAnimation>();
        GameObject numObj = indicator.transform.GetChild(0).gameObject;
        Sequence seq = DOTween.Sequence();
        imgAnim.StartAnimation();
        //seq.Insert(0f , numObj.transform.DOScale(1.6f, 0.5f).SetEase(Ease.InOutSine));
        seq.Insert(0.2f, numObj.GetComponent<CanvasGroup>().DOFade(0f, 0.5f).SetEase(Ease.InOutSine));
    }

    private void SpinIndicatorFrontAnimation(GameObject indicator)
    {
        ImageAnimation imgAnim = indicator.GetComponent<ImageAnimation>();
        GameObject numObj = indicator.transform.GetChild(0).gameObject;
        numObj.transform.localScale = Vector3.one * 2.5f;
        numObj.GetComponent<CanvasGroup>().alpha = 0f;
        Sequence seq = DOTween.Sequence();
        imgAnim.StartReverseAnimation();
        seq.Insert(0f, numObj.transform.DOScale(1f, 0.5f).SetEase(Ease.InOutSine));
        seq.Insert(0f, numObj.GetComponent<CanvasGroup>().DOFade(1f, 0.2f).SetEase(Ease.InOutSine));
    }

    private void InitializeBonusUI()
    {
        RefreshLockedSymbols();
        NormalSlotPanel.SetActive(false);
        BonusSlotPanel.SetActive(true);
        MultiplierPanel.SetActive(false);
        SpinIndicatorObject.SetActive(true);
        InitializeSlide();
        for (int i = 0; i < spinIndicators.Count; i++)
            spinIndicators[i].SetActive(true);
    }

    private float ScaleForIndex(int i) => Mathf.Max(0f, 1f - i * 0.1f);

    private void InitializeSlide()
    {
        foreach (GameObject old in currentSlideObjects)
            if (old != null) Destroy(old);
        currentSlideObjects.Clear();

        _slideQueueConsumedCount = 0;

        _snapshotSlideQueue = new List<string>(
            socketManager.resultData.payload.state.fortuneSlideQueue ?? new List<string>());

        int count = _snapshotSlideQueue.Count;

        for (int i = 0; i < count; i++)
        {
            GameObject slideObj = Instantiate(SlideObject, SlideParentObject.transform);
            slideObj.transform.SetSiblingIndex(0);
            RectTransform rt = slideObj.GetComponent<RectTransform>();
            rt.anchoredPosition = slideObjectPositions[i].anchoredPosition;
            rt.localScale = Vector3.one * ScaleForIndex(i);

            ConfigureSlide(slideObj, i, i);

            currentSlideObjects.Add(slideObj);
        }
    }

    private void RefreshLockedSymbols()
    {
        for (int i = 0; i < socketManager.resultData.payload.state.lockedSymbols.Count; i++)
        {
            var sym = socketManager.resultData.payload.state.lockedSymbols[i];
            int reel = sym.reel;
            int position = sym.position;

            Image slotImg = winSlotImages[reel].slotImages[position];
            slotImg.color = new Color(255, 255, 255);
            slotImg.gameObject.SetActive(true);

            if (sym.isJackpot && !string.IsNullOrEmpty(sym.jackpotName))
            {
                Sprite jp = GetJackpotSprite(sym.jackpotName);
                if (jp != null)
                {
                    slotImg.sprite = jp;
                    slotImg.GetComponentInChildren<TMP_Text>().text = "";
                    continue; // skip the numeric text assignment below
                }
            }

            slotImg.GetComponentInChildren<TMP_Text>().text =
                UIManager.ToSpriteString(sym.value);
        }
    }

    private void shuffleSlotImages(bool midTween = false)
    {
        for (int i = 0; i < totalImages.Count; i++)
        {
            for (int j = 0; j < totalImages[i].slotImages.Count; j++)
            {
                Sprite image = slotImages[slotImages.Length - 1];
                if (!midTween)
                    totalImages[i].slotImages[j].sprite = image;
            }
        }
    }

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
        slotTransform.localPosition = new Vector2(slotTransform.localPosition.x, 600f);
        _alltweens[index] = slotTransform.DOLocalMoveY(397f, 0.5f).SetEase(Ease.OutElastic);
        if (!isStop)
            yield return new WaitForSeconds(0.2f);
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
}

[Serializable]
public class BonusPath
{
    public List<CurvySpline> splines = new List<CurvySpline>(10);
}