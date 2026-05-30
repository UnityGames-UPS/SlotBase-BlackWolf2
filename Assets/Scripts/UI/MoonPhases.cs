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

    [Header("Blast Sprites")]
    [SerializeField] private List<Sprite> BlastSprites;

    [Header("Loop Sprites")]
    [SerializeField] private List<Sprite> MoonFirstPhaseLoopSprites;
    [SerializeField] private List<Sprite> MoonSecondPhaseLoopSprites;
    [SerializeField] private List<Sprite> MoonThirdPhaseLoopSprites;
    [SerializeField] private List<Sprite> MoonFourthPhaseLoopSprites;
    [SerializeField] private List<Sprite> MoonFifthPhaseLoopSprites;
    [SerializeField] private List<Sprite> MoonSixthPhaseLoopSprites;
    [SerializeField] private List<Sprite> MoonSeventhPhaseLoopSprites;

    [Header("Transition Sprites")]
    [SerializeField] private List<Sprite> MoonSecondPhaseTransitionSprites;
    [SerializeField] private List<Sprite> MoonThirdPhaseTransitionSprites;
    [SerializeField] private List<Sprite> MoonFourthPhaseTransitionSprites;
    [SerializeField] private List<Sprite> MoonFifthPhaseTransitionSprites;
    [SerializeField] private List<Sprite> MoonSixthPhaseTransitionSprites;
    [SerializeField] private List<Sprite> MoonSeventhPhaseTransitionSprites;

    // Tracks which phase is currently looping (1–7), so we know when a phase change occurs
    private int _currentPhase = 0;
    private ImageAnimation _moonImageAnimation;

    // Each phase threshold: phase N becomes active once total_progress >= phaseThresholds[N-1]
    // total_progress goes 0 → 1 across 7 phases, so each step is ~1/7 ≈ 0.1428
    private static readonly float[] PhaseThresholds = { 0f, 1f/7f, 2f/7f, 3f/7f, 4f/7f, 5f/7f, 6f/7f };

    // Scale per phase: phase 1 = 1.0, +0.05 each step
    private static readonly float[] PhaseScales = { 1.00f, 1.05f, 1.10f, 1.15f, 1.20f, 1.25f, 1.30f };

    private void Awake()
    {
        _moonImageAnimation = MoonAnimationImage.GetComponent<ImageAnimation>();
    }

    private void Start()
    {
        // Start the first-phase loop immediately when the game loads
        StartCoroutine(StartFirstPhaseLoop());
    }

    private IEnumerator StartFirstPhaseLoop()
    {
        _currentPhase = 1;
        MoonAnimationImage.transform.localScale = Vector3.one * PhaseScales[0];
        PlayLoop(GetLoopSprites(1));
        yield return null;
    }

    /// <summary>
    /// Call this after each result arrives. Reads total_progress to determine the new phase,
    /// plays the transition (if phase changed) then kicks off the loop for that phase.
    /// </summary>
    internal IEnumerator SetMoonPhase(LevelProgress levelProgress)
    {
        yield return new WaitForSeconds(0.5f);

        int newPhase = GetPhaseFromProgress(levelProgress.total_progress);

        // No phase change — nothing to do
        if (newPhase == _currentPhase)
            yield break;

        // Walk through every phase that was crossed (handles multi-phase jumps gracefully)
        while (_currentPhase < newPhase)
        {
            int nextPhase = _currentPhase + 1;

            // --- Stop current loop ---
            _moonImageAnimation.StopAnimation();

            // --- Play transition animation (plays ONCE) ---
            List<Sprite> transitionSprites = GetTransitionSprites(nextPhase);
            if (transitionSprites != null && transitionSprites.Count > 0)
            {
                _moonImageAnimation.textureArray = transitionSprites;
                _moonImageAnimation.doLoopAnimation = false;
                _moonImageAnimation.StartAnimation();
                yield return new WaitUntil(() =>
                    _moonImageAnimation.currentAnimationState == ImageAnimation.ImageState.FINISHED);
            }

            // --- Scale up the moon object ---
            float targetScale = PhaseScales[nextPhase - 1];
            MoonAnimationImage.transform.DOScale(Vector3.one * targetScale, 0.3f).SetEase(Ease.OutBack);
            yield return new WaitForSeconds(0.35f); // let scale finish before loop starts

            // --- Switch immediately to loop for the new phase ---
            _currentPhase = nextPhase;
            PlayLoop(GetLoopSprites(_currentPhase));
        }
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

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

    private void PlayLoop(List<Sprite> sprites)
    {
        if (sprites == null || sprites.Count == 0) return;
        _moonImageAnimation.textureArray = sprites;
        _moonImageAnimation.doLoopAnimation = true;
        _moonImageAnimation.StartAnimation();
    }

    private List<Sprite> GetLoopSprites(int phase)
    {
        return phase switch
        {
            1 => MoonFirstPhaseLoopSprites,
            2 => MoonSecondPhaseLoopSprites,
            3 => MoonThirdPhaseLoopSprites,
            4 => MoonFourthPhaseLoopSprites,
            5 => MoonFifthPhaseLoopSprites,
            6 => MoonSixthPhaseLoopSprites,
            7 => MoonSeventhPhaseLoopSprites,
            _ => MoonFirstPhaseLoopSprites
        };
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
        blastObj.GetComponent<ImageAnimation>().textureArray = BlastSprites;
        blastObj.GetComponent<ImageAnimation>().doLoopAnimation = false;
        blastObj.GetComponent<ImageAnimation>().StartAnimation();

        yield return new WaitUntil(() =>
            blastObj.GetComponent<ImageAnimation>().currentAnimationState == ImageAnimation.ImageState.FINISHED);
        Destroy(blastObj);
    }
}