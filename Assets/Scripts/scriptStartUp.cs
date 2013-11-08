using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class scriptStartUp : MonoBehaviour {
	private int i;
	// Start is called just before any of the
	// Update methods is called the first time.
	void Start () {
		Screen.orientation = ScreenOrientation.LandscapeLeft; 
		
	}
	void Update () {
		if(i >= 7) {
			//Wait faor a while..
			if(SystemInfo.deviceType == DeviceType.Handheld) {
                //Okey, on a handheld, what now?
				if(iPhone.generation == iPhoneGeneration.iPadMini1Gen || iPhone.generation == iPhoneGeneration.iPad1Gen || iPhone.generation == iPhoneGeneration.iPad2Gen) {
					Application.LoadLevel ("sceneMain_1024x768");
                } else {
                        //We are running on something strange... Die garcefully!
                       
                }
			}	
		}
		i++;
	}
}
