using System.Collections.Generic;
using UnityEngine;
using System.Collections;
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
        //WinSlotObject.SetActive(true);
        StartCoroutine(StartWinningLineAnimation(lineWins));
    }

    private IEnumerator StartWinningLineAnimation(List<LineWin> lineWins)
    {
        // for (int r = 0; r < winSlotImages.Count; r++)
        //     for (int s = 0; s < winSlotImages[r].slotImages.Count; s++)
        //         winSlotImages[r].slotImages[s].GetComponent<Mask>().showMaskGraphic = true;
        for (int r = 0; r < resultSlotImages.Count; r++)
            for (int s = 0; s < resultSlotImages[r].slotImages.Count; s++)
                resultSlotImages[r].slotImages[s].transform.GetChild(5).gameObject.SetActive(true);


        for (int i = 0; i < lineWins.Count; i++)
        {
            List<List<int>> winPositions = lineWins[i].positions;
            for (int j = 0; j < winPositions.Count; j++)
            {
                int reelIndex = winPositions[j][0];
                int positionIndex = winPositions[j][1];

                //winSlotImages[positionIndex].slotImages[reelIndex].GetComponent<Mask>().showMaskGraphic = false;
                resultSlotImages[positionIndex].slotImages[reelIndex].transform.GetChild(5).gameObject.SetActive(false);

                ImageAnimation borderAnim = resultSlotImages[positionIndex].slotImages[reelIndex].transform.GetChild(3).GetComponent<ImageAnimation>();
                resultSlotImages[positionIndex].slotImages[reelIndex].transform.GetChild(3).gameObject.SetActive(true);
                borderAnim.textureArray = BorderAnimation;
                borderAnim.AnimationSpeed = 25f;
                borderAnim.doLoopAnimation = true;
                borderAnim.StartAnimation();

                ImageAnimation anim = resultSlotImages[positionIndex].slotImages[reelIndex].transform.GetChild(2).GetComponent<ImageAnimation>();
                resultSlotImages[positionIndex].slotImages[reelIndex].transform.GetChild(2).gameObject.SetActive(true);
                int symbolID = int.Parse(socketManager.resultData.matrix[reelIndex][positionIndex]);
                //Debug.Log(symbolID);
                anim.textureArray = winLineAnimationImages[symbolID].Images;
                anim.AnimationSpeed = 50f;
                anim.doLoopAnimation = true;
                if (symbolID == 8)
                {
                    resultSlotImages[positionIndex].slotImages[reelIndex].transform.GetChild(3).localScale = new Vector3(1.2f, 1.2f, 1.2f);
                    resultSlotImages[positionIndex].slotImages[reelIndex].transform.GetChild(2).localScale = new Vector3(2.1f, 2.1f, 2.1f);
                    resultSlotImages[positionIndex].slotImages[reelIndex].transform.GetChild(2).GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -10f);
                    anim.AnimationSpeed = 31f;
                }
                anim.StartAnimation();
            }
        }

        yield return new WaitUntil(() => !isDisplayingWinningLines);

        for (int j = 0; j < resultSlotImages.Count; j++)
            for (int k = 0; k < resultSlotImages[j].slotImages.Count; k++)
            {
                resultSlotImages[j].slotImages[k].transform.GetChild(3).localScale = new Vector3(1f, 1f, 1f);
                resultSlotImages[j].slotImages[k].transform.GetChild(3).gameObject.SetActive(false);
                resultSlotImages[j].slotImages[k].transform.GetChild(2).GetComponent<RectTransform>().anchoredPosition = new Vector2(0,0);
                //resultSlotImages[j].slotImages[k].transform.GetChild(2).gameObject.SetActive(false);
            }

        // for (int r = 0; r < winSlotImages.Count; r++)
        //     for (int s = 0; s < winSlotImages[r].slotImages.Count; s++)
        //         winSlotImages[r].slotImages[s].GetComponent<Mask>().showMaskGraphic = true;
        for (int r = 0; r < resultSlotImages.Count; r++)
            for (int s = 0; s < resultSlotImages[r].slotImages.Count; s++)
                resultSlotImages[r].slotImages[s].transform.GetChild(5).gameObject.SetActive(false);

        //WinSlotObject.SetActive(false);
    }
}

[Serializable]
public class AnimationImages
{
    public List<Sprite> Images = new List<Sprite>();
}