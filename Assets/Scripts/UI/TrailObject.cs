using Unity;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Collections;
using FluffyUnderware.Curvy;
using FluffyUnderware.Curvy.Controllers;

public class TrailObject : MonoBehaviour
{
    [SerializeField] private SplineController TrailController;
    [SerializeField] private CurvySpline LogoPathCurvySpline;
    [SerializeField] private CurvySpline MultiplierPathCurvySpline;

    [Header("Managers")]
    [SerializeField] private AnimationManager AnimationManager;

    // ── Logo trail ─────────────────────────────────────────────────────────────
    internal IEnumerator StartLogoAnimation(Action onComplete = null)
    {
        TrailController.Spline = LogoPathCurvySpline;
        LogoPathCurvySpline.Refresh();

        yield return new WaitForSeconds(0.5f);

        var onLogoAnimationFinishedSettings = new OnPositionReachedSettings();
        onLogoAnimationFinishedSettings.Position = 1f;
        onLogoAnimationFinishedSettings.PositionMode = CurvyPositionMode.Relative;

        onLogoAnimationFinishedSettings.Event.AddListener((CurvySplineMoveEventArgs args) =>
        {
            StartCoroutine(OnLogoFinished(onComplete));
        });

        TrailController.OnPositionReachedList.Add(onLogoAnimationFinishedSettings);
        TrailController.Clamping = CurvyClamping.Clamp;

        gameObject.SetActive(true);
        TrailController.Play();

        yield return null;
    }

    private IEnumerator OnLogoFinished(Action onComplete)
    {
        yield return new WaitForSeconds(0.5f);
        AnimationManager.LogoBlastAnimation();
        yield return new WaitForSeconds(1.5f);
        gameObject.SetActive(false);
        onComplete?.Invoke();
    }

    // ── Boost trail ────────────────────────────────────────────────────────────
    internal IEnumerator StartBoostAnimation(double bonusAmont, GameObject slotObject)
    {
        TrailController.Spline = MultiplierPathCurvySpline;
        MultiplierPathCurvySpline.Refresh();

        yield return new WaitForSeconds(0.5f);

        var onBoostAnimationFinishedSettings = new OnPositionReachedSettings();
        onBoostAnimationFinishedSettings.Position = 1f;
        onBoostAnimationFinishedSettings.PositionMode = CurvyPositionMode.Relative;

        onBoostAnimationFinishedSettings.Event.AddListener((CurvySplineMoveEventArgs args) =>
        {
            StartCoroutine(OnBoostFinished(bonusAmont));
        });

        TrailController.OnPositionReachedList.Add(onBoostAnimationFinishedSettings);
        TrailController.Clamping = CurvyClamping.Clamp;

        gameObject.SetActive(true);
        TrailController.Play();
        slotObject.SetActive(true);

        yield return null;
    }

    private IEnumerator OnBoostFinished(double bonusAmont)
    {
        //yield return new WaitForSeconds(1f);
        AnimationManager.BoostBlastAnimation(bonusAmont);
        gameObject.SetActive(false);
        yield return null;
    }

    internal IEnumerator StartMultiplierAnimation(double bonusAmont, GameObject slotObject)
    {
        TrailController.OnPositionReachedList.Clear();

        TrailController.Spline = MultiplierPathCurvySpline;
        MultiplierPathCurvySpline.Refresh();

        //yield return new WaitForSeconds(0.5f);

        var onMultiplierAnimationFinishedSettings = new OnPositionReachedSettings();
        onMultiplierAnimationFinishedSettings.Position = 1f;
        onMultiplierAnimationFinishedSettings.PositionMode = CurvyPositionMode.Relative;

        onMultiplierAnimationFinishedSettings.Event.AddListener((CurvySplineMoveEventArgs args) =>
        {
            StartCoroutine(OnMultiplierFinished(bonusAmont));
        });

        TrailController.OnPositionReachedList.Add(onMultiplierAnimationFinishedSettings);
        TrailController.Speed = 2f;
        TrailController.Clamping = CurvyClamping.Clamp;

        gameObject.SetActive(true);
        TrailController.Play();

        // Activate the win-slot overlay so it's visible when the trail lands
        slotObject.SetActive(true);
        Debug.Log("Multiplier trail started for amount: " + bonusAmont);

        yield return null;
    }

    private IEnumerator OnMultiplierFinished(double bonusAmont)
    {
        //yield return new WaitForSeconds(1f);
        AnimationManager.MultiplierAnimation(bonusAmont);
        yield return new WaitForSeconds(0.3f);
        gameObject.SetActive(false);
        yield return null;
    }

    // ── Utility ────────────────────────────────────────────────────────────────
    internal void ResetTrail()
    {
        TrailController.Stop();
        gameObject.SetActive(false);
        TrailController.OnPositionReachedList.Clear();
    }
}