using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ImageAnimation : MonoBehaviour
{
	public enum ImageState
	{
		NONE,
		PLAYING,
		PAUSED,
		FINISHED
	}
	[SerializeField] internal List<Sprite> textureArray;
	[SerializeField] private Image rendererDelegate;
	[SerializeField] private bool useSharedMaterial = true;
	[SerializeField] internal bool doLoopAnimation = true;
	[SerializeField] private bool StartOnAwake;
	[SerializeField] internal float AnimationSpeed = 5f;
	[SerializeField] private float delayBetweenLoop;
	[HideInInspector] public ImageState currentAnimationState;
	private int indexOfTexture;
	private float idealFrameRate = 0.0416666679f;
	private float delayBetweenAnimation;

	private void Awake()
	{
		if(StartOnAwake){
			StartAnimation();
		}
	}

	private void OnDisable()
	{
		StopAnimation();
	}

	private void AnimationProcess()
	{
		SetTextureOfIndex();
		indexOfTexture++;
		if (indexOfTexture == textureArray.Count)
		{
			indexOfTexture = 0;
			if (doLoopAnimation)
			{
				Invoke("AnimationProcess", delayBetweenAnimation + delayBetweenLoop);
			}
			else
			{
				currentAnimationState = ImageState.FINISHED;
			}
		}
		else
		{
			Invoke("AnimationProcess", delayBetweenAnimation);
		}
	}

	internal void StartAnimation()
	{
		// Force state to NONE so this always runs even if StopAnimation wasn't called first
		CancelInvoke("AnimationProcess");
		currentAnimationState = ImageState.NONE;

		indexOfTexture = 0;
		RevertToInitialState();
		delayBetweenAnimation = idealFrameRate * (float)textureArray.Count / AnimationSpeed;
		currentAnimationState = ImageState.PLAYING;
		Invoke("AnimationProcess", delayBetweenAnimation);
	}

	internal void PauseAnimation()
	{
		if (currentAnimationState == ImageState.PLAYING)
		{
			CancelInvoke("AnimationProcess");
			currentAnimationState = ImageState.PAUSED;
		}
	}

	internal void ResumeAnimation()
	{
		if (currentAnimationState == ImageState.PAUSED && !IsInvoking("AnimationProcess"))
		{
			Invoke("AnimationProcess", delayBetweenAnimation);
			currentAnimationState = ImageState.PLAYING;
		}
	}

	internal void StopAnimation()
	{
		CancelInvoke("AnimationProcess");
		if (textureArray != null && textureArray.Count > 0)
			rendererDelegate.sprite = textureArray[0];
		currentAnimationState = ImageState.NONE;
	}

	private void RevertToInitialState()
	{
		indexOfTexture = 0;
		SetTextureOfIndex();
	}

	private void SetTextureOfIndex()
	{
		if (useSharedMaterial)
		{
			rendererDelegate.sprite = textureArray[indexOfTexture];
		}
		else
		{
			rendererDelegate.sprite = textureArray[indexOfTexture];
		}
	}
}