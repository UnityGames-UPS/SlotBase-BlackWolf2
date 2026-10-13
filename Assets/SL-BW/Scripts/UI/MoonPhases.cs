using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using DG.Tweening;

public class MoonPhases : MonoBehaviour
{
    [SerializeField] private Image MoonAnimationImage;
    [SerializeField] private GameObject MoonBlastParent;
    [SerializeField] private GameObject MoonBlastPrefab;
    [SerializeField] private Sprite FirstPhaseSprite;
    [SerializeField] private Sprite FullMoonSprites;

    [Header("Blast Sprites")]
    [SerializeField] private List<Sprite> BlastSprites;

    [Header("Transition Sprites")]
    [SerializeField] private List<Sprite> MoonSecondPhaseTransitionSprites;
    [SerializeField] private List<Sprite> MoonThirdPhaseTransitionSprites;
    [SerializeField] private List<Sprite> MoonFourthPhaseTransitionSprites;
    [SerializeField] private List<Sprite> MoonFifthPhaseTransitionSprites;
    [SerializeField] private List<Sprite> MoonSixthPhaseTransitionSprites;
    [SerializeField] private List<Sprite> MoonSeventhPhaseTransitionSprites;

    // Tracks which phase we are currently on (1–7)
    private int _currentPhase = 1;
    private ImageAnimation _moonImageAnimation;

    // Phase N is active when total_progress >= PhaseThresholds[N-1].
    // total_progress goes 0→1 across 7 phases, each step ~1/7 ≈ 0.1428.
    //   Phase 1 : 0.000 – 0.142   (index 0 = 0/7)
    //   Phase 2 : 0.143 – 0.285   (index 1 = 1/7)
    //   Phase 3 : 0.286 – 0.428   (index 2 = 2/7)
    //   Phase 4 : 0.429 – 0.571   (index 3 = 3/7)  ← 0.45 lands here → phase 4
    //   Phase 5 : 0.572 – 0.714   (index 4 = 4/7)
    //   Phase 6 : 0.715 – 0.857   (index 5 = 5/7)
    //   Phase 7 : 0.858 – 1.000   (index 6 = 6/7)
    private static readonly float[] PhaseThresholds = { 0f, 1f / 7f, 2f / 7f, 3f / 7f, 4f / 7f, 5f / 7f, 6f / 7f };

    // Scale per phase: phase 1 = 1.0, +0.05 each step
    private static readonly float[] PhaseScales = { 1.00f, 1.05f, 1.10f, 1.15f, 1.20f, 1.25f, 1.30f };

    private void Awake()
    {
        _moonImageAnimation = MoonAnimationImage.GetComponent<ImageAnimation>();
    }

    private void Start()
    {
        // Phase 1 has no transition — just apply the starting scale and leave the image as-is.
        _currentPhase = 1;
        MoonAnimationImage.transform.localScale = Vector3.one * PhaseScales[0];
    }

    // -------------------------------------------------------------------------
    // Call this after each result arrives.
    // Reads total_progress, determines the new phase, and plays the transition
    // animation once per phase step. Freezes on the last frame — no loop after.
    // -------------------------------------------------------------------------
    internal IEnumerator SetMoonPhase(LevelProgress levelProgress)
    {
        yield return new WaitForSeconds(0.5f);

        int newPhase = GetPhaseFromProgress(levelProgress.total_progress);

        Debug.Log($"[MoonPhases] total_progress={levelProgress.total_progress:F4}  currentPhase={_currentPhase}  newPhase={newPhase}");

        if (newPhase == 1)
        {
            if (_currentPhase == 7)
            {
                // Special case for phase 1: just set the sprite and scale, no animation.
                MoonAnimationImage.sprite = FullMoonSprites;
                MoonAnimationImage.transform.localScale = Vector3.one * 1.4f;
                _currentPhase = 1;
            }
            else if (_currentPhase == 1)
            {
                // Special case for phase 1: just set the sprite and scale, no animation.
                MoonAnimationImage.sprite = FirstPhaseSprite;
                MoonAnimationImage.transform.localScale = Vector3.one * PhaseScales[0];
                _currentPhase = 1;
            }
        }

        // No phase change — nothing to do
        if (newPhase <= _currentPhase)
            yield break;

        // Walk through every phase that was crossed (handles multi-phase jumps)
        while (_currentPhase < newPhase)
        {
            int nextPhase = _currentPhase + 1;

            List<Sprite> transitionSprites = GetTransitionSprites(nextPhase);

            if (transitionSprites != null && transitionSprites.Count > 0)
            {
                // Stop whatever is playing
                _moonImageAnimation.StopAnimation();

                // Play the transition ONCE
                _moonImageAnimation.textureArray = transitionSprites;
                _moonImageAnimation.doLoopAnimation = false;
                _moonImageAnimation.StartAnimation();

                // Wait for it to finish

                // Freeze on the last frame of the transition
                // (ImageAnimation leaves the last sprite displayed when FINISHED — nothing extra needed)
            }

            // Scale up the moon
            float targetScale = PhaseScales[nextPhase - 1];
            MoonAnimationImage.transform.DOScale(Vector3.one * targetScale, 0.3f).SetEase(Ease.OutBack);

            yield return new WaitUntil(() =>
                _moonImageAnimation.currentAnimationState == ImageAnimation.ImageState.FINISHED);

            yield return new WaitForSeconds(0.35f);

            _currentPhase = nextPhase;
            Debug.Log($"[MoonPhases] Phase advanced to {_currentPhase}");
        }
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    internal void ResetMoon()
    {
        _currentPhase = 1;
        _moonImageAnimation.StopAnimation();
        MoonAnimationImage.sprite = FirstPhaseSprite;
        MoonAnimationImage.transform.localScale = Vector3.one * PhaseScales[0];
    }

    /// <summary>Maps total_progress (0–1) to a phase number (1–7).</summary>
    private int GetPhaseFromProgress(double totalProgress)
    {
        float p = (float)totalProgress;
        for (int i = PhaseThresholds.Length - 1; i >= 0; i--)
        {
            if (p >= PhaseThresholds[i])
                return i + 1;
        }
        return 1;
    }

    private List<Sprite> GetTransitionSprites(int toPhase)
    {
        return toPhase switch
        {
            2 => MoonSecondPhaseTransitionSprites,
            3 => MoonThirdPhaseTransitionSprites,
            4 => MoonFourthPhaseTransitionSprites,
            5 => MoonFifthPhaseTransitionSprites,
            6 => MoonSixthPhaseTransitionSprites,
            7 => MoonSeventhPhaseTransitionSprites,
            _ => null
        };
    }

    // -------------------------------------------------------------------------

    internal IEnumerator BlastAnimation()
    {
        GameObject blastObj = Instantiate(MoonBlastPrefab, MoonBlastParent.transform);
        blastObj.GetComponent<Image>().sprite = BlastSprites[0];
        ImageAnimation blastAnim = blastObj.GetComponent<ImageAnimation>();
        blastAnim.textureArray = BlastSprites;
        blastAnim.doLoopAnimation = false;
        blastAnim.StartAnimation();

        yield return new WaitUntil(() =>
            blastAnim.currentAnimationState == ImageAnimation.ImageState.FINISHED);
        Destroy(blastObj);
    }
}