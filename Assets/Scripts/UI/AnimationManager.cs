using Unity;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using DG.Tweening;

public class AnimationManager : MonoBehaviour
{

    [Header("References")]
    [SerializeField] private BonusManger bonusManger;
    [SerializeField] private SlotManager slotManager;

    [SerializeField] private GameObject GameLogo;
    [SerializeField] private GameObject BoostMiltiplierObject;
    [SerializeField] private TMP_Text BoostMultiplierText;
    [SerializeField] private List<GameObject> BoostObjects;
    [SerializeField] private Sprite BoostEnabledSprite;
    [SerializeField] private Sprite BoostDisabledSprite;


    private Tween MultiplierTextTween;
    internal bool isBoostBlastAnimationFinished = false;
    internal bool isBoostAnimationFinished = false;
    internal bool isMultiplierAnimationFinished = false;
    internal bool isLogoBlastAnimationFinished = false;
    internal bool isBonusWolfAnimationFinished = false;

    internal void LogoBlastAnimation()
    {
        Debug.Log("L O G O");
        isLogoBlastAnimationFinished = true;
    }

    internal void BoostBlastAnimation(double boostAmount)
    {
        Debug.Log("Starting Multiplier Animation with amount: " + boostAmount);
        double initAmount = double.Parse(BoostMultiplierText.text);
        double finalAmount = initAmount + boostAmount;
        MultiplierTextTween = DOTween.To(() => initAmount, (val) => initAmount = val, finalAmount, 0.4f)
            .OnUpdate(() =>
            {
                BoostMultiplierText.text = initAmount.ToString("F3");
            })
            .OnComplete(() =>
            {
                isBoostBlastAnimationFinished = true;
            });
    }

    internal void MultiplierAnimation(double boostAmount)
    {
        Debug.Log("Starting Boost Blast Animation with boost amount: " + boostAmount);
        double initAmount = double.Parse(bonusManger.MultiplierText.text);
        double finalAmount = initAmount + boostAmount;
        MultiplierTextTween = DOTween.To(() => initAmount, (val) => initAmount = val, finalAmount, 0.4f)
            .OnUpdate(() =>
            {
                bonusManger.MultiplierText.text = initAmount.ToString("F3");
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
            GameObject boostSymbol = BoostObjects[boostPosition.position];
            boostSymbol.SetActive(true);
            // boostSymbol.GetComponent<ImageAnimation>().StartAnimation();
            // yield return new WaitUntil(() => boostSymbol.GetComponent<ImageAnimation>().currentAnimationState == ImageAnimation.ImageState.FINISHED);
            boostSymbol.GetComponent<Image>().sprite = BoostDisabledSprite;
            yield return new WaitForSeconds(0.3f);
        }
        BoostMiltiplierObject.SetActive(true);
        BoostMultiplierText.text = "0.000";
        isBoostAnimationFinished = true;
    }

    internal void ResetBoostAnimation(List<SlotImage> resultImages)
    {
        Debug.Log("Resetting Boost Animation");
        BoostMiltiplierObject.SetActive(false);
        if (MultiplierTextTween != null && MultiplierTextTween.IsActive())
        {
            MultiplierTextTween.Kill();
        }
        foreach (GameObject boostObj in BoostObjects)
        {
            boostObj.SetActive(false);
            boostObj.GetComponent<Image>().sprite = BoostEnabledSprite;
            boostObj.GetComponent<ImageAnimation>().currentAnimationState = ImageAnimation.ImageState.NONE;
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

    internal void BonusWolfAnimation()
    {
        isBonusWolfAnimationFinished = true;
    }

    internal void SlideSymbolFinishedAnimation(GameObject gameObject)
    {

    }
}