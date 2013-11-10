using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class scriptMain : MonoBehaviour {
	
	private int i;
	private int iCurrentScreen = 0;
	
	// Start is called just before any of the
	// Update methods is called the first time.
	void Start () {
		//Set auto rotate and so on
		Screen.autorotateToPortrait = false;
		Screen.autorotateToPortraitUpsideDown = false;
		Screen.autorotateToLandscapeLeft = true;
		Screen.autorotateToLandscapeRight = true;
		Screen.orientation = ScreenOrientation.AutoRotation;
		//Grab objects

	}
	
	// Update is called every frame, if the
	// MonoBehaviour is enabled.
	void Update () {
		i++;
	}
	
	void OnSwipe(SwipeGesture gesture) {
		Debug.Log ("OnSwipe: " + gesture.Direction );
		if(gesture.Direction == FingerGestures.SwipeDirection.Right) {
			iCurrentScreen --;
			if(iCurrentScreen < 0) iCurrentScreen = 0;
			ShowScreen (iCurrentScreen);
		} else if(gesture.Direction == FingerGestures.SwipeDirection.Left) {
			iCurrentScreen ++;
			if(iCurrentScreen > 2) iCurrentScreen = 2;
			ShowScreen (iCurrentScreen);
		}
	}
	
	void ShowScreen(int iScreenId) {
		if(iScreenId == 2) {
			iTween.MoveTo(gameObject, new Vector3(102.4f, 0.0f, -10.0f), 1.0f);
		} else if(iScreenId == 1){
			iTween.MoveTo(gameObject, new Vector3(51.2f, 0.0f, -10.0f), 1.0f);
		} else { //Asume 0
			iTween.MoveTo(gameObject, new Vector3(0.0f, 0.0f, -10.0f), 1.0f);
		}
	}
	
}
