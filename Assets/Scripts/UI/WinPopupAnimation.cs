using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using DG.Tweening;

public class WinPopupAnimation : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private SlotManager slotManager;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private AudioController audioController;

    [Header("UI References")]
    [SerializeField] private TMP_Text WinAmountText;
    [SerializeField] private TMP_Text BalanceText;

    // ── Popup structure (mirrors UIManager's WinPopupParent / WinPopup_Object) ──
    [Header("Win Popup Structure")]
    [SerializeField] private GameObject WinPopup_Object;        // root that gets shown/hidden
    [SerializeField] private Transform  WinPopupParent;         // the transform that scales in/out
    [SerializeField] private TMP_Text   Win_Text;               // sprite-font text (uses ToSpriteString)
    [SerializeField] private Button     SkipWinAnimation;

    // ── Extra cinematic objects (total / bonus win only) ──
    [Header("Cinematic Objects")]
    [SerializeField] private GameObject WinPopupGlowObject;     // has CanvasGroup for fade
    [SerializeField] private GameObject WinPopupCoinObject;     // has ImageAnimation
    [SerializeField] private GameObject WinBlastObject;         // one-shot blast effect
    [SerializeField] private GameObject WinTextObject;

    [Header("Coin Animation Sprites")]
    [SerializeField] private List<Sprite> CoinStartingSprites;
    [SerializeField] private List<Sprite> CoinLoopSprites;
    [SerializeField] private List<Sprite> TotalWinSprites;
    [SerializeField] private List<Sprite>BoostWinSprites;
    [SerializeField] private List<Sprite>BonusWinSprites;

    // ── Private state ──
    private double _targetAmount;
    private double _targetBalance;
    private bool   _isSkipped;
    internal bool popupDone;

    private Tween _amountCountTween;
    private Tween _balanceCountTween;
    private Tween _closeDelayTween;
    private Tween _textScaleTween;
    private Tween _glowFadeTween;
    private Coroutine   _activeRoutine;
    private CanvasGroup _glowCanvasGroup;

    private void Awake()
    {
        _glowCanvasGroup = GetOrAddCanvasGroup(WinPopupGlowObject);
        if (SkipWinAnimation) SkipWinAnimation.onClick.AddListener(SkipWin);
    }

    internal void ShowNormalWinPopup(double winAmount, bool animateBalance = true)
    {
        audioController.PlayNormalWin();
        popupDone = false;
        StopActiveRoutine();
        _activeRoutine = StartCoroutine(NormalWinRoutine(winAmount, animateBalance));
    }

    internal void ShowBoostWinPopup(double winAmount, bool animateBalance = true)
    {
        audioController.PlayBoostWin();
        popupDone = false;
        StopActiveRoutine();
        SetWinTextSprites(BoostWinSprites);
        _activeRoutine = StartCoroutine(CinematicWinRoutine(winAmount, animateBalance));
    }

    internal void ShowBonusWinPopup(double winAmount)
    {
        audioController.PlayBonusWin();
        popupDone = false;
        StopActiveRoutine();
        SetWinTextSprites(BonusWinSprites);
        _activeRoutine = StartCoroutine(CinematicWinRoutine(winAmount, true));
    }

    internal void ShowTotalWinPopup(double winAmount)
    {
        audioController.PlayAnotherWin();
        popupDone = false;
        StopActiveRoutine();
        SetWinTextSprites(TotalWinSprites);
        _activeRoutine = StartCoroutine(CinematicWinRoutine(winAmount, true));
    }

    private IEnumerator NormalWinRoutine(double winAmount, bool animateBalance)
    {
        _isSkipped    = false;
        _targetAmount = winAmount;

        double startBalance = 0;
        if (animateBalance && BalanceText != null)
            startBalance = UIManager.FromSpriteString(BalanceText.text);
        _targetBalance = startBalance + winAmount;

        ResetPopupState();

        if (WinPopup_Object) WinPopup_Object.SetActive(true);

        WinPopupParent.localScale = Vector3.zero;
        WinPopupParent.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);

        if (WinPopupGlowObject)
        {
            WinPopupGlowObject.SetActive(true);
            _glowCanvasGroup.alpha = 0f;
            _glowFadeTween = _glowCanvasGroup.DOFade(1f, 0.5f).OnComplete(() =>
            {
                ImageAnimation glowAnim = WinPopupGlowObject.GetComponent<ImageAnimation>();
                if (glowAnim != null)
                {
                    glowAnim.doLoopAnimation = true;
                    glowAnim.StartAnimation();
                }
            });
        }

        if (WinAmountText) WinAmountText.text = UIManager.ToSpriteString(0.0, "F2");

        if (Win_Text) Win_Text.text = UIManager.ToSpriteString(0.0, "F2");

        double displayAmount = 0;
        _amountCountTween = DOTween.To(
            ()  => displayAmount,
            val =>
            {
                displayAmount = val;
                if (Win_Text)      Win_Text.text      = UIManager.ToSpriteString(val, "F2");
                if (WinAmountText) WinAmountText.text = UIManager.ToSpriteString(val, "F2");
            },
            winAmount, 1.5f
        );

        if (animateBalance && BalanceText != null)
        {
            double displayBalance = startBalance;
            _balanceCountTween = DOTween.To(
                ()  => displayBalance,
                val => { displayBalance = val; BalanceText.text = UIManager.ToSpriteString(val, "F2"); },
                _targetBalance, 1.5f
            );
        }

        yield return _amountCountTween.WaitForCompletion();

        if (Win_Text)      Win_Text.text      = UIManager.ToSpriteString(winAmount, "F2");
        if (WinAmountText) WinAmountText.text = UIManager.ToSpriteString(winAmount, "F2");
        if (animateBalance && BalanceText != null)
            BalanceText.text = UIManager.ToSpriteString(_targetBalance, "F2");

        if (!_isSkipped)
        {
            bool holdDone = false;
            _closeDelayTween = DOVirtual.DelayedCall(2f, () => holdDone = true);
            yield return new WaitUntil(() => holdDone || _isSkipped);
        }

        popupDone = true;
        yield return CloseMainPopup();
    }


    private IEnumerator CinematicWinRoutine(double winAmount, bool animateBalance)
    {
        _isSkipped    = false;
        _targetAmount = winAmount;

        double startBalance = 0;
        if (animateBalance && BalanceText != null)
            startBalance = UIManager.FromSpriteString(BalanceText.text);
        _targetBalance = startBalance + winAmount;

        ResetPopupState();

        if (WinPopup_Object) WinPopup_Object.SetActive(true);

        if (WinTextObject)
        {
            WinTextObject.SetActive(true);
            ImageAnimation winTextAnim = WinTextObject.GetComponent<ImageAnimation>();
            if (winTextAnim != null && winTextAnim.textureArray != null && winTextAnim.textureArray.Count > 0)
            {
                winTextAnim.doLoopAnimation = false;
                winTextAnim.StartAnimation();
            }
        }

        WinPopupParent.localScale = Vector3.one * 3f;
        bool textLanded = false;
        _textScaleTween = WinPopupParent
            .DOScale(Vector3.one, 0.45f)
            .SetEase(Ease.OutBack)
            .OnComplete(() => textLanded = true);

        yield return new WaitUntil(() => textLanded);

        if (WinBlastObject)
        {
            WinBlastObject.SetActive(true);
            ImageAnimation blastAnim = WinBlastObject.GetComponent<ImageAnimation>();
            if (blastAnim != null)
            {
                blastAnim.doLoopAnimation = false;
                blastAnim.StartAnimation();
                StartCoroutine(DeactivateWhenFinished(WinBlastObject, blastAnim));
            }
        }

        if (Win_Text)
        {
            Win_Text.gameObject.SetActive(true);
            Win_Text.text = UIManager.ToSpriteString(0.0, "F2");
        }
        if (WinAmountText) WinAmountText.text = UIManager.ToSpriteString(0.0, "F2");

        double displayAmount = 0;
        _amountCountTween = DOTween.To(
            ()  => displayAmount,
            val =>
            {
                displayAmount = val;
                if (Win_Text)      Win_Text.text      = UIManager.ToSpriteString(val, "F2");
                if (WinAmountText) WinAmountText.text = UIManager.ToSpriteString(val, "F2");
            },
            winAmount, 2f
        );

        if (animateBalance && BalanceText != null)
        {
            double displayBalance = startBalance;
            _balanceCountTween = DOTween.To(
                ()  => displayBalance,
                val => { displayBalance = val; BalanceText.text = UIManager.ToSpriteString(val, "F2"); },
                _targetBalance, 2f
            );
        }

        if (WinPopupGlowObject)
        {
            WinPopupGlowObject.SetActive(true);
            _glowCanvasGroup.alpha = 0f;
            _glowFadeTween = _glowCanvasGroup.DOFade(1f, 0.5f).OnComplete(() =>
            {
                ImageAnimation glowAnim = WinPopupGlowObject.GetComponent<ImageAnimation>();
                if (glowAnim != null)
                {
                    glowAnim.doLoopAnimation = true;
                    glowAnim.StartAnimation();
                }
            });
        }

        if (WinPopupCoinObject)
        {
            WinPopupCoinObject.SetActive(true);
            ImageAnimation coinAnim = WinPopupCoinObject.GetComponent<ImageAnimation>();
            if (coinAnim != null && CoinStartingSprites.Count > 0)
            {
                coinAnim.textureArray    = CoinStartingSprites;
                coinAnim.doLoopAnimation = false;
                coinAnim.StartAnimation();

                yield return new WaitUntil(() =>
                    coinAnim.currentAnimationState == ImageAnimation.ImageState.FINISHED);

                if (CoinLoopSprites.Count > 0)
                {
                    coinAnim.textureArray    = CoinLoopSprites;
                    coinAnim.doLoopAnimation = true;
                    coinAnim.StartAnimation();
                }
            }
        }

        yield return _amountCountTween.WaitForCompletion();
        if (Win_Text)      Win_Text.text      = UIManager.ToSpriteString(winAmount, "F2");
        if (WinAmountText) WinAmountText.text = UIManager.ToSpriteString(winAmount, "F2");
        if (animateBalance && BalanceText != null)
            BalanceText.text = UIManager.ToSpriteString(_targetBalance, "F2");

        if (!_isSkipped)
        {
            bool holdDone = false;
            _closeDelayTween = DOVirtual.DelayedCall(2f, () => holdDone = true);
            yield return new WaitUntil(() => holdDone || _isSkipped);
        }

        yield return CloseMainPopup();
        popupDone = true;
    }


    private void SkipWin()
    {
        _isSkipped = true;

        _amountCountTween?.Kill();
        _balanceCountTween?.Kill();
        _closeDelayTween?.Kill();
        _textScaleTween?.Kill();
        _glowFadeTween?.Kill();

        // Snap to final values immediately
        if (Win_Text)      Win_Text.text      = UIManager.ToSpriteString(_targetAmount, "F2");
        if (WinAmountText) WinAmountText.text = UIManager.ToSpriteString(_targetAmount, "F2");
        if (BalanceText)   BalanceText.text   = UIManager.ToSpriteString(_targetBalance, "F2");
    }


    private IEnumerator CloseMainPopup()
    {
        bool closeDone = false;

        CanvasGroup mainCG = GetOrAddCanvasGroup(WinPopup_Object);

        WinPopupParent.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack);
        mainCG.DOFade(0f, 0.5f).OnComplete(() => closeDone = true);

        yield return new WaitUntil(() => closeDone);

        // Hide everything and restore state for next use
        HideAllPopupObjects();
        WinPopupParent.localScale = Vector3.one;
        mainCG.alpha = 1f;

        if (slotManager != null) slotManager._checkPopups = false;
    }

    private void ResetPopupState()
    {
        HideAllPopupObjects();
        KillAllTweens();
        WinPopupParent.localScale = Vector3.one;

        CanvasGroup mainCG = GetOrAddCanvasGroup(WinPopup_Object);
        if (mainCG) mainCG.alpha = 1f;
        if (_glowCanvasGroup) _glowCanvasGroup.alpha = 0f;

        if (Win_Text) Win_Text.text = UIManager.ToSpriteString(0.0, "F2");
    }

    private void HideAllPopupObjects()
    {
        if (WinPopup_Object)    WinPopup_Object.SetActive(false);
        if (WinPopupGlowObject) WinPopupGlowObject.SetActive(false);
        if (WinPopupCoinObject) WinPopupCoinObject.SetActive(false);
        if (WinBlastObject)     WinBlastObject.SetActive(false);
        if (WinTextObject)      WinTextObject.SetActive(false);
    }

    private void SetWinTextSprites(List<Sprite> sprites)
    {
        if (WinTextObject == null || sprites == null || sprites.Count == 0) return;
        ImageAnimation anim = WinTextObject.GetComponent<ImageAnimation>();
        if (anim != null) anim.textureArray = sprites;
    }

    private void KillAllTweens()
    {
        _amountCountTween?.Kill();
        _balanceCountTween?.Kill();
        _closeDelayTween?.Kill();
        _textScaleTween?.Kill();
        _glowFadeTween?.Kill();
    }

    private void StopActiveRoutine()
    {
        if (_activeRoutine != null)
        {
            StopCoroutine(_activeRoutine);
            _activeRoutine = null;
        }
        KillAllTweens();
        HideAllPopupObjects();
    }

    private IEnumerator DeactivateWhenFinished(GameObject go, ImageAnimation anim)
    {
        yield return new WaitUntil(() =>
            anim.currentAnimationState == ImageAnimation.ImageState.FINISHED ||
            anim.currentAnimationState == ImageAnimation.ImageState.NONE);
        if (go != null) go.SetActive(false);
    }

    private static CanvasGroup GetOrAddCanvasGroup(GameObject go)
    {
        if (go == null) return null;
        CanvasGroup cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();
        return cg;
    }
}