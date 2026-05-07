using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using UnityEngine.UI.Extensions;
using UnityEngine.UI;
using System;

public class PayLineManager : MonoBehaviour
{
    [SerializeField] private GameObject WinSlotObject;
    [SerializeField] private List<SlotImage> winSlotImages;
    [SerializeField] private List<AnimationImages> winLineAnimationImages;

    [Header("Animation")]
    [SerializeField] private List<Sprite> BorderAnimation;

    [SerializeField] private SlotManager slotManager;
    [SerializeField] private SocketIOManager socketManager;

    internal List<SlotImage> resultSlotImages;
    internal bool isDisplayingWinningLines = false;

    private void Start()
    {
        resultSlotImages = slotManager._resultImages;
    }

    internal void DisplayWinningLines(List<LineWin> lineWins)
    {
        isDisplayingWinningLines = true;
        WinSlotObject.SetActive(true);
        StartCoroutine(StartWinningLineAnimation(lineWins));
    }

    private IEnumerator StartWinningLineAnimation(List<LineWin> lineWins)
    {
        // Darken everything first
        for (int r = 0; r < winSlotImages.Count; r++)
            for (int s = 0; s < winSlotImages[r].slotImages.Count; s++)
                winSlotImages[r].slotImages[s].GetComponent<Mask>().showMaskGraphic = true;

        // Start ALL win animations across ALL lines simultaneously
        for (int i = 0; i < lineWins.Count; i++)
        {
            List<List<int>> winPositions = lineWins[i].positions;
            for (int j = 0; j < winPositions.Count; j++)
            {
                int reelIndex     = winPositions[j][0];
                int positionIndex = winPositions[j][1];

                winSlotImages[positionIndex].slotImages[reelIndex].GetComponent<Mask>().showMaskGraphic = false;

                // Border — loops until stopped
                ImageAnimation borderAnim = resultSlotImages[positionIndex].slotImages[reelIndex].transform.GetChild(2).GetComponent<ImageAnimation>();
                resultSlotImages[positionIndex].slotImages[reelIndex].transform.GetChild(2).gameObject.SetActive(true);
                borderAnim.textureArray = BorderAnimation;
                borderAnim.AnimationSpeed = 45f;
                borderAnim.doLoopAnimation = true;
                borderAnim.StartAnimation();

                // Symbol — loops until next spin
                ImageAnimation anim = resultSlotImages[positionIndex].slotImages[reelIndex].transform.GetChild(3).GetComponent<ImageAnimation>();
                resultSlotImages[positionIndex].slotImages[reelIndex].transform.GetChild(3).gameObject.SetActive(true);
                int symbolID = int.Parse(socketManager.resultData.matrix[reelIndex][positionIndex]);
                Debug.Log(symbolID);
                anim.textureArray = winLineAnimationImages[symbolID].Images;
                anim.AnimationSpeed = 45f;
                anim.doLoopAnimation = true;
                anim.StartAnimation();
            }
        }

        // Hold until next spin kills the flag
        yield return new WaitUntil(() => !isDisplayingWinningLines);

        // Clean up
        for (int j = 0; j < resultSlotImages.Count; j++)
            for (int k = 0; k < resultSlotImages[j].slotImages.Count; k++)
            {
                resultSlotImages[j].slotImages[k].transform.GetChild(2).gameObject.SetActive(false);
                resultSlotImages[j].slotImages[k].transform.GetChild(3).gameObject.SetActive(false);
            }

        for (int r = 0; r < winSlotImages.Count; r++)
            for (int s = 0; s < winSlotImages[r].slotImages.Count; s++)
                winSlotImages[r].slotImages[s].GetComponent<Mask>().showMaskGraphic = true;

        WinSlotObject.SetActive(false);
    }
}

[Serializable]
public class AnimationImages
{
    public List<Sprite> Images = new List<Sprite>();
}