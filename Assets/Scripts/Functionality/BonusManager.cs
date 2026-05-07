using Unity;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using FluffyUnderware.Curvy;
using FluffyUnderware.Curvy.Controllers;
using Microsoft.Unity.VisualStudio.Editor;
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
  
    [Header("Prefabs")]
    [SerializeField] private GameObject SlideObject;

    [Header("Slide GameObject Position")]
    [SerializeField] private List<RectTransform> slideObjectPositions;
    [SerializeField] private GameObject SlideParentObject;
    [SerializeField] private GameObject FirstSlideOverlayImage;
    [SerializeField] private GameObject SlideAnimationParent;

    [Header("Bonus Paths")]
    [SerializeField] private List<BonusPath> BonusPaths;

    [Header("Slot Objects")]
    [SerializeField] private Sprite[] slotImages;
    [SerializeField] private List<SlotImage> totalImages;     //class to store total images
    [SerializeField] internal List<SlotImage> resultImages;     //class to store the result matrix
    [SerializeField] private List<SlotImage> winSlotImages;     //class to store the winning line images
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

    private int _numberOfSlots = 5;
    internal bool isBonusFinished = false;
    private bool isBonusIntroAnimationFinished = false;
    private bool isSlideAnimationFinished = false;
    private bool isSlideFinished = false;
    private bool isFortuneSlideAnimationFinished = false;
    private bool isBonusMultiplierAnimationFinished = false;

    void Start()
    {
        //BonusStartButton.onClick.AddListener(delegate () { StartCoroutine(StartBonus()); });
        
        shuffleSlotImages();
        //StartCoroutine(BonusIntroAnimation());
    }

    private IEnumerator BonusIntroAnimation()
    {
        // Reset starting state
        MoonImage.GetComponent<RectTransform>().localPosition = new Vector2(540f, 1000f);
        WolfImage.GetComponent<RectTransform>().localPosition = new Vector2(46f, -100f);
        LeftTree.GetComponent<RectTransform>().localPosition = new Vector2(-900f, 100f);
        RightTree.GetComponent<RectTransform>().localPosition = new Vector2(900f, 100f);
        MoonImage.GetComponent<RectTransform>().localScale    = new Vector2(0.6f, 0.6f);
        WolfImage.GetComponent<RectTransform>().localScale    = new Vector2(0.8f, 0.8f);
        WolfImage.GetComponent<CanvasGroup>().alpha = 0f;
        MoonImage.GetComponent<CanvasGroup>().alpha = 0f;
        LeftTree.GetComponent<CanvasGroup>().alpha = 0f;
        RightTree.GetComponent<CanvasGroup>().alpha = 0f;

        Sequence seq = DOTween.Sequence();

        // 0.0s — MainBG slides down
        seq.Append(MainBG.transform.DOLocalMoveY(-1080f, 2.2f).SetEase(Ease.InOutSine));

        // 0.8s — All 6 tweens fire at the exact same time:
        //        fade in + scale up + move to final position for both Wolf and Moon
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
        yield return new WaitForSeconds(2f);
        isBonusIntroAnimationFinished = true;
    }

    internal IEnumerator StartBonus()
    {
        isBonusIntroAnimationFinished = false;
        StartCoroutine(BonusIntroAnimation());

        // Wait for the full intro before showing bonus UI
        yield return new WaitUntil(() => isBonusIntroAnimationFinished);

        uIManager.ToggleBonusBackground();
        InitializeBonusUI();

        // Reset MainBG back to its resting position so it doesn't stay off-screen
        MainBG.GetComponent<RectTransform>().localPosition = new Vector2(0f, 0f);

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
                spinIndicators[currentSpinCount - 1].SetActive(false);
                currentSpinCount--;
            }

            socketManager.AccumulateResult(slotManager._betCounter);
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
                    resultImages[reel].slotImages[position].GetComponentInChildren<TMP_Text>().text = bs.value.ToString();
                }
            }

            for (int i = 0; i < _numberOfSlots; i++)
            {
                yield return StopTweening(_slotTransforms[i], i, _stopSpinToggle);
            }

            _stopSpinToggle = true;

            int spinsAfter = socketManager.resultData.payload.state.respinsLeft;

            if (spinsAfter > currentSpinCount)
            {
                for (int i = currentSpinCount; i < spinsAfter && i < spinIndicators.Count; i++)
                {
                    spinIndicators[i].SetActive(true);
                    yield return new WaitForSeconds(0.5f);
                }
                currentSpinCount = spinsAfter;
            }

            yield return _alltweens[^1].WaitForCompletion();
            KillAllTweens();

            isFortuneSlideAnimationFinished = false;
            StartCoroutine(FortuneSlideAnimation());
            yield return new WaitUntil(() => isFortuneSlideAnimationFinished);

            RefreshLockedSymbols();
        }

        // --- Bonus round finished ---
        animationManager.isBonusWolfAnimationFinished = false;
        animationManager.BonusWolfAnimation();
        yield return new WaitUntil(() => animationManager.isBonusWolfAnimationFinished);

        isBonusMultiplierAnimationFinished = false;
        StartCoroutine(MultiplierAnimation());
        yield return new WaitUntil(() => isBonusMultiplierAnimationFinished);

        // Clean up all spawned slide objects
        foreach (GameObject slide in currentSlideObjects)
            Destroy(slide);
        currentSlideObjects.Clear();

        // Fix 3: Reset all win slot images so they don't bleed into the next spin
        for (int i = 0; i < winSlotImages.Count; i++)
        {
            for (int j = 0; j < winSlotImages[i].slotImages.Count; j++)
            {
                winSlotImages[i].slotImages[j].gameObject.SetActive(false);
                winSlotImages[i].slotImages[j].GetComponentInChildren<TMP_Text>().text = "";
            }
        }

        // Reset bonus UI back to normal slot view
        BonusSlotPanel.SetActive(false);
        NormalSlotPanel.SetActive(true);
        MultiplierPanel.SetActive(false);
        SpinIndicatorObject.SetActive(false);
        uIManager.ToggleBonusBackground();

        isBonusFinished = true;
    }

    private IEnumerator MultiplierAnimation()
    {
        MultiplierPanel.SetActive(true);
        for (int i = 0; i < socketManager.resultData.payload.state.lockedSymbols.Count; i++)
        {
            int reel     = socketManager.resultData.payload.state.lockedSymbols[i].reel;
            int position = socketManager.resultData.payload.state.lockedSymbols[i].position;
            TrailObject trail = winSlotImages[reel].slotImages[position].GetComponentInChildren<TrailObject>(true);
            animationManager.isMultiplierAnimationFinished = false;  // reset BEFORE starting each trail
            StartCoroutine(trail.StartMultiplierAnimation(socketManager.resultData.payload.state.lockedSymbols[i].value , winSlotImages[reel].slotImages[position].gameObject));
            yield return new WaitUntil(() => animationManager.isMultiplierAnimationFinished);
        }
        isBonusMultiplierAnimationFinished = true;
    }

    private IEnumerator FortuneSlideAnimation()
    {
        for (int i = 0; i < _numberOfSlots; i++)
        {
            for (int j = 0; j < resultImages[i].slotImages.Count; j++)
            {
                if (resultImages[i].slotImages[j].sprite == slotImages[11] ||
                    resultImages[i].slotImages[j].sprite == slotImages[10])
                {
                    // Safety: nothing to send if queue is empty
                    //if (currentSlideObjects.Count == 0) continue;

                    yield return new WaitForSeconds(0.4f);

                    isSlideFinished = false;

                    // Remove the front slide from the tracked list BEFORE the spline
                    // plays, so SlideMoveAnimation receives the already-updated list.
                    GameObject frontSlide = currentSlideObjects[0];
                    currentSlideObjects.RemoveAt(0);

                    // Re-parent so it can travel freely above everything else
                    frontSlide.transform.SetParent(SlideAnimationParent.transform, true);
                    frontSlide.transform.SetAsLastSibling();

                    SplineController trail = frontSlide.GetComponent<SplineController>();

                    // Refresh BEFORE assigning — Curvy needs a clean spline first
                    BonusPaths[i].splines[j].Refresh();
                    trail.Spline = BonusPaths[i].splines[j];

                    // Reset controller state so it starts from position 0
                    trail.AbsolutePosition = 0f;
                    trail.Speed            = 2f;   // MUST be > 0 or Play() does nothing
                    trail.Clamping         = CurvyClamping.Clamp;

                    // Wipe stale listeners every time
                    trail.OnPositionReachedList.Clear();

                    // Capture loop vars so the closure is correct
                    int ci = i, cj = j;
                    GameObject capturedSlide = frontSlide;

                    var onReachedSettings = new OnPositionReachedSettings();
                    onReachedSettings.Position     = 1f;
                    onReachedSettings.PositionMode = CurvyPositionMode.Relative;

                    onReachedSettings.Event.AddListener((CurvySplineMoveEventArgs args) =>
                    {
                        // Coroutine so we wait for shift tweens before signalling done
                        StartCoroutine(SlideMoveAnimation(capturedSlide, ci, cj));
                    });

                    trail.OnPositionReachedList.Add(onReachedSettings);
                    yield return new WaitForSeconds(1f);
                    Debug.Log("Start Playing");
                    trail.Play();
                    Debug.Log("Started Playing....");

                    yield return new WaitUntil(() => isSlideFinished);
                }
            }
        }
        isFortuneSlideAnimationFinished = true;
    }

    // Converted to a coroutine so isSlideFinished is only set after
    // the shift tweens have actually finished playing.
    private IEnumerator SlideMoveAnimation(GameObject landedSlide, int reelIndex, int posIndex)
    {
        const float shiftDuration = 0.3f;

        // 1. Notify the game that this symbol has been filled
        animationManager.SlideSymbolFinishedAnimation(
            resultImages[reelIndex].slotImages[posIndex].gameObject);

        // 2. Shift every remaining slide forward and scale it up to its new position.
        //    currentSlideObjects already has the landed slide removed.
        for (int i = 0; i < currentSlideObjects.Count; i++)
        {
            RectTransform rt    = currentSlideObjects[i].GetComponent<RectTransform>();
            rt.DOAnchorPos(slideObjectPositions[i].anchoredPosition, shiftDuration).SetEase(Ease.OutQuad);
            rt.DOScale(ScaleForIndex(i),                             shiftDuration).SetEase(Ease.OutQuad);
        }

        // 3. Spawn a fresh slide at the back — sibling index 0 so it is behind
        //    all the slides that just shifted forward.
        int newIndex = currentSlideObjects.Count;
        if (newIndex < slideObjectPositions.Count)
        {
            GameObject newSlide = Instantiate(SlideObject, SlideParentObject.transform);
            newSlide.transform.SetSiblingIndex(0);       // behind all existing slides
            RectTransform newRt = newSlide.GetComponent<RectTransform>();
            newRt.anchoredPosition = slideObjectPositions[newIndex].anchoredPosition;
            newRt.localScale       = Vector3.one * ScaleForIndex(newIndex);
            currentSlideObjects.Add(newSlide);           // appended to back of list
        }

        // 4. Wait one frame so Curvy's event callback has fully returned before
        //    we destroy the object it fired from — avoids a use-after-free crash.
        yield return null;
        Destroy(landedSlide);

        // 5. Wait for the shift tweens, then signal the outer coroutine.
        yield return new WaitForSeconds(shiftDuration);
        isSlideFinished = true;
    }

    private void InitializeBonusUI()
    {
        NormalSlotPanel.SetActive(false);
        BonusSlotPanel.SetActive(true);
        MultiplierPanel.SetActive(false);
        SpinIndicatorObject.SetActive(true);
        InitializeSlide();
        for (int i = 0; i < spinIndicators.Count; i++)
        {
            spinIndicators[i].SetActive(true);
        }
        RefreshLockedSymbols();
    }

    // Returns the scale a slide should have when it occupies slot index i.
    // Index 0 = front (scale 1.0), index 1 = 0.9, index 2 = 0.8, …
    private float ScaleForIndex(int i) => Mathf.Max(0f, 1f - i * 0.1f);

    private void InitializeSlide()
    {
        // Destroy any leftover slides from a previous bonus round
        foreach (GameObject old in currentSlideObjects)
            if (old != null) Destroy(old);
        currentSlideObjects.Clear();

        int count = socketManager.resultData.payload.state.fortuneSlideQueue.Count;

        // Forward loop: i=0 is the FRONT slide (scale 1.0, renders on top).
        // Each new slide is inserted at sibling index 0 (behind everything already spawned),
        // so the first-spawned (front) keeps getting pushed UP the hierarchy and ends up
        // at the highest sibling index = drawn on top. List order: Add() keeps list[0]=front.
        for (int i = 0; i < count; i++)
        {
            GameObject slideObj = Instantiate(SlideObject, SlideParentObject.transform);
            slideObj.transform.SetSiblingIndex(0);       // push to bottom; earlier slides move up
            RectTransform rt = slideObj.GetComponent<RectTransform>();
            rt.anchoredPosition = slideObjectPositions[i].anchoredPosition;
            rt.localScale       = Vector3.one * ScaleForIndex(i);
            currentSlideObjects.Add(slideObj);           // list[0]=front, list[count-1]=back
        }
    }

    private void RefreshLockedSymbols()
    {
        for (int i = 0; i < socketManager.resultData.payload.state.lockedSymbols.Count; i++)
        {
            int reel = socketManager.resultData.payload.state.lockedSymbols[i].reel;
            int position = socketManager.resultData.payload.state.lockedSymbols[i].position;
            winSlotImages[reel].slotImages[position].color = new Color(255,255,255);
            winSlotImages[reel].slotImages[position].GetComponentInChildren<TMP_Text>().text = socketManager.resultData.payload.state.lockedSymbols[i].value.ToString();

            winSlotImages[reel].slotImages[position].gameObject.SetActive(true);
        }
    }

    private void shuffleSlotImages(bool midTween = false)
    {
        for (int i = 0; i < totalImages.Count; i++)
        {
            for (int j = 0; j < totalImages[i].slotImages.Count; j++)
            {
                Sprite image = slotImages[slotImages.Length-1];
                if (!midTween)
                {
                    totalImages[i].slotImages[j].sprite = image;
                }
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
        slotTransform.localPosition = new Vector2(slotTransform.localPosition.x, 101f);
        _alltweens[index] = slotTransform.DOLocalMoveY(397f, 0.5f).SetEase(Ease.OutElastic);
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
}

[Serializable]
public class BonusPath
{
    public List<CurvySpline> splines = new List<CurvySpline>(10);
}