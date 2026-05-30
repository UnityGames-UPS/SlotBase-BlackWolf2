using Unity;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using DG.Tweening;

public class AnimationManager : MonoBehaviour
{

    [Header("References")]
    [SerializeField] private BonusManger bonusManger;
    [SerializeField] private SlotManager slotManager;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private MoonPhases moonPhases;
    [SerializeField] private AudioController audioController;

    [SerializeField] private GameObject GameLogo;
    [SerializeField] private GameObject BoostMiltiplierObject;
    [SerializeField] private GameObject BoostAnimationObject;
    [SerializeField] private TMP_Text BoostMultiplierText;
    [SerializeField] private List<BoostObject> BoostObjects;
    [SerializeField] private List<BoostObject> BonusBoostObjects;
    [SerializeField] private Sprite BoostEnabledSprite;
    [SerializeField] private Sprite BoostDisabledSprite;
    [SerializeField] private GameObject WolfAnimationObject;

    [Header("Bonus References")]
    [SerializeField] private GameObject BonusBoostMiltiplierObject;
    [SerializeField] private GameObject BonusBoostAnimationObject;
    [SerializeField] private TMP_Text BonusBoostMultiplierText;




    private Tween MultiplierTextTween;
    internal bool isBoostBlastAnimationFinished = false;
    internal bool isBonusBoostAnimationFinished = false;
    internal bool isBoostAnimationFinished = false;
    internal bool isMultiplierAnimationFinished = false;
    internal bool isLogoBlastAnimationFinished = false;
    internal bool isBonusWolfAnimationFinished = false;

    void Start()
    {
        //BoostMiltiplierObject.GetComponent<ImageAnimation>().StartAnimation();
        // WolfAnimationObject.SetActive(true);
        // WolfAnimationObject.GetComponent<ImageAnimation>().StartAnimation();
        //yield return new WaitUntil(() => WolfAnimationObject.GetComponent<ImageAnimation>().currentAnimationState == ImageAnimation.ImageState.FINISHED);
    }

    internal void LogoBlastAnimation()
    {
        Debug.Log("L O G O");
        StartCoroutine(moonPhases.BlastAnimation());
        isLogoBlastAnimationFinished = true;
    }

    internal void BoostBlastAnimation(double boostAmount)
    {
        Debug.Log("Starting Multiplier Animation with amount: " + boostAmount);
        BoostAnimationObject.GetComponent<ImageAnimation>().StartAnimation();
        double initAmount = UIManager.FromSpriteString(BoostMultiplierText.text);
        double finalAmount = initAmount + boostAmount;
        MultiplierTextTween = DOTween.To(() => initAmount, (val) => initAmount = val, finalAmount, 0.2f)
            .OnUpdate(() =>
            {
                BoostMultiplierText.text = UIManager.ToSpriteString(initAmount, "F3");
            })
            .OnComplete(() =>
            {
                isBoostBlastAnimationFinished = true;
            });
    }

    internal void MultiplierAnimation(double boostAmount)
    {

        Debug.Log("Starting Multiplier Animation with boost amount: " + boostAmount);
        BonusBoostAnimationObject.GetComponent<ImageAnimation>().StartAnimation();
        double initAmount = UIManager.FromSpriteString(bonusManger.MultiplierText.text);
        double finalAmount = initAmount + boostAmount;

        // FIX: Guard against a zero-delta tween.
        if (System.Math.Abs(boostAmount) < 0.0001)
        {
            bonusManger.MultiplierText.text = UIManager.ToSpriteString(finalAmount, "F3");
            isMultiplierAnimationFinished = true;
            return;
        }

        MultiplierTextTween = DOTween.To(() => initAmount, (val) => initAmount = val, finalAmount, 0.4f)
            .OnUpdate(() =>
            {
                bonusManger.MultiplierText.text = UIManager.ToSpriteString(initAmount, "F3");
            })
            .OnComplete(() =>
            {
                isMultiplierAnimationFinished = true;
            });
    }

    internal IEnumerator BoostAnimation(List<BoostPosition> boostPositions)
    {
        foreach (BoostPosition boostPosition in boostPositions)
        {
            GameObject boostSymbol = BoostObjects[boostPosition.reel].boostObject[boostPosition.position].gameObject;
            boostSymbol.SetActive(true);
            boostSymbol.GetComponent<ImageAnimation>().StartAnimation();
            yield return new WaitUntil(()=> boostSymbol.GetComponent<ImageAnimation>().currentAnimationState == ImageAnimation.ImageState.FINISHED);
        }
        BoostMiltiplierObject.SetActive(true);
        BoostMiltiplierObject.GetComponent<ImageAnimation>().StartAnimation();
        BoostMultiplierText.text = UIManager.ToSpriteString(0.0, "F3");
        isBoostAnimationFinished = true;
    }

    // ── CHANGE 4: Bonus-scene boost animation.
    // Shows the BonusBoostMultiplierObject panel (not the normal BoostMiltiplierObject)
    // and highlights the boost positions, then signals completion.
    internal IEnumerator BonusBoostAnimation(List<BoostPosition> boostPositions)
    {
        foreach (BoostPosition boostPosition in boostPositions)
        {
            GameObject boostSymbol = BonusBoostObjects[boostPosition.reel].boostObject[boostPosition.position];
            boostSymbol.SetActive(true);
            boostSymbol.GetComponent<Image>().sprite = BoostDisabledSprite;
            yield return new WaitForSeconds(0.3f);
        }
        BonusBoostMiltiplierObject.SetActive(true);
        BonusBoostMiltiplierObject.GetComponent<ImageAnimation>().StartAnimation();
        BonusBoostMultiplierText.text = UIManager.ToSpriteString(0.0, "F3");
        isBonusBoostAnimationFinished = true;
    }

    // ── CHANGE 4: Helper so BonusManager can read the accumulated bonus-boost total
    // without needing a direct serialized reference to BonusBoostMultiplierText.
    internal string GetBonusBoostMultiplierText()
    {
        return BonusBoostMultiplierText != null ? BonusBoostMultiplierText.text : "0";
    }

    internal void ResetBoostAnimation(List<SlotImage> resultImages)
    {
        Debug.Log("Resetting Boost Animation");
        BoostMiltiplierObject.SetActive(false);
        if (MultiplierTextTween != null && MultiplierTextTween.IsActive())
        {
            MultiplierTextTween.Kill();
        }
        foreach (BoostObject boostObjList in BoostObjects)
        {
            foreach (GameObject boostObj in boostObjList.boostObject)
            {
                boostObj.SetActive(false);
                boostObj.GetComponent<Image>().sprite = BoostEnabledSprite;
                boostObj.GetComponent<ImageAnimation>().currentAnimationState = ImageAnimation.ImageState.NONE;
            }
        }
        for (int i = 0; i < slotManager._numberOfSlots; i++)
        {
            for (int j = 0; j < resultImages[i].slotImages.Count; j++)
            {
                resultImages[i].slotImages[j].transform.GetChild(4).gameObject.SetActive(false);
            }
        }
        isBoostBlastAnimationFinished = false;
        isBoostAnimationFinished = false;
        isLogoBlastAnimationFinished = false;
    }

    internal void ResetBonusBoostAnimation()
    {
        Debug.Log("Resetting Bonus Boost Animation");
        BonusBoostMiltiplierObject.SetActive(false);
        if (MultiplierTextTween != null && MultiplierTextTween.IsActive())
        {
            MultiplierTextTween.Kill();
        }
        foreach (BoostObject boostObjList in BonusBoostObjects)
        {
            foreach (GameObject boostObj in boostObjList.boostObject)
            {
                boostObj.SetActive(false);
                boostObj.GetComponent<Image>().sprite = BoostEnabledSprite;
                boostObj.GetComponent<ImageAnimation>().currentAnimationState = ImageAnimation.ImageState.NONE;
            }
        }
        isBonusBoostAnimationFinished = false;
    }

    internal IEnumerator BonusWolfAnimation()
    {
        WolfAnimationObject.SetActive(true);
        audioController.PlayWolfAppear();
        WolfAnimationObject.GetComponent<ImageAnimation>().StartAnimation();
        yield return new WaitUntil(() => WolfAnimationObject.GetComponent<ImageAnimation>().currentAnimationState == ImageAnimation.ImageState.FINISHED);
        isBonusWolfAnimationFinished = true;
    }

    internal void SlideSymbolFinishedAnimation(GameObject gameObject)
    {

    }
}

[Serializable]
public class BoostObject
{
    public List<GameObject> boostObject = new List<GameObject>(10);
}