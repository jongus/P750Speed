using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class scriptMain : MonoBehaviour {
	
	private int i;
	
	// Start is called just before any of the
	// Update methods is called the first time.
	void Start () {
		//Set auto rotate and so on
		Screen.autorotateToPortrait = false;
		Screen.autorotateToPortraitUpsideDown = false;
		Screen.autorotateToLandscapeLeft = true;
		Screen.autorotateToLandscapeRight = true;
		Screen.orientation = ScreenOrientation.AutoRotation;
		
	}
	
	// Update is called every frame, if the
	// MonoBehaviour is enabled.
	void Update () {
		i++;
	}
	
}
