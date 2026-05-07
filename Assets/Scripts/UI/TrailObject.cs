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
        yield return new WaitForSeconds(2f);
        gameObject.SetActive(false);
        AnimationManager.LogoBlastAnimation();
        onComplete?.Invoke();
    }

    internal IEnumerator StartBoostAnimation(double bonusAmont , GameObject slotObject)
    {
        TrailController.Spline = MultiplierPathCurvySpline;
        MultiplierPathCurvySpline.Refresh();

        yield return new WaitForSeconds(0.5f); // Wait one frame for TrailController to register the new spline

        var onBoostAnimationFinishedSettings = new OnPositionReachedSettings();
        onBoostAnimationFinishedSettings.Position = 1f;                              // end of spline (TF = 1)
        onBoostAnimationFinishedSettings.PositionMode = CurvyPositionMode.Relative;

        onBoostAnimationFinishedSettings.Event.AddListener((CurvySplineMoveEventArgs args) =>
        {
            StartCoroutine(OnBoostFinished(bonusAmont));
            //ResetTrail();
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
        yield return new WaitForSeconds(1f);
        gameObject.SetActive(false);
        AnimationManager.BoostBlastAnimation(bonusAmont);
    }

    internal IEnumerator StartMultiplierAnimation(double bonusAmont , GameObject slotObject)
    {
        TrailController.Spline = MultiplierPathCurvySpline;
        MultiplierPathCurvySpline.Refresh();

        yield return new WaitForSeconds(0.5f); // Wait one frame for TrailController to register the new spline

        var onBoostAnimationFinishedSettings = new OnPositionReachedSettings();
        onBoostAnimationFinishedSettings.Position = 1f;                              // end of spline (TF = 1)
        onBoostAnimationFinishedSettings.PositionMode = CurvyPositionMode.Relative;

        onBoostAnimationFinishedSettings.Event.AddListener((CurvySplineMoveEventArgs args) =>
        {
            StartCoroutine(OnMultiplierFinished(bonusAmont));
            //ResetTrail();
        });

        TrailController.OnPositionReachedList.Add(onBoostAnimationFinishedSettings);

        TrailController.Clamping = CurvyClamping.Clamp;

        gameObject.SetActive(true);

        TrailController.Play();
        //slotObject.GetComponent<Image>().color = new Color();
        Debug.Log("Color changed");
        yield return null;
    }

    private IEnumerator OnMultiplierFinished(double bonusAmont)
    {
        yield return new WaitForSeconds(1f);
        gameObject.SetActive(false);
        AnimationManager.MultiplierAnimation(bonusAmont);
    }

    internal void ResetTrail()
    {
        TrailController.Stop();
        gameObject.SetActive(false);
        TrailController.OnPositionReachedList.Clear();
    }
}