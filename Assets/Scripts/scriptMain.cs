using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class ScriptMain : MonoBehaviour {
	//Constants
	private const double  dMS2KN = 1.943844d;
	
	//General variables
	private int iCurrentScreen = 0;
	private tk2dSprite spritePaginationMarked;
	private ScreenOrientation orientationOld = ScreenOrientation.LandscapeLeft;
	private int iWaitForGPS = 0; //The first updates tend to be wrong...
	private double dLastTimestamp = 0.0d; //Updates when we move
	private double dLastLat = 0.0d;
	private double dLastLon = 0.0d;
	private double[] adAvgSpeed;	
	private double dBearing = 0.0d;
	private double dCurSpeed = -1.0d;
	private double dAccDist = 0.0d;
	
	//Text Meshes
	private tk2dTextMesh tmCurrentSpeed;
	private tk2dTextMesh tmCurrentSpeedDecimal;
	private int iLocationStatus = -1; //-1=startup, 0=ok, 1=something wrong
	
	//GameObjects
	private GameObject goWarning;
	
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
		spritePaginationMarked = GameObject.Find ("spritePaginationMarked").GetComponent<tk2dSprite>();
		tmCurrentSpeed = GameObject.Find ("tmCurrentSpeed").GetComponent<tk2dTextMesh>();
		tmCurrentSpeedDecimal = GameObject.Find ("tmCurrentSpeedDecimal").GetComponent<tk2dTextMesh>();
		goWarning = GameObject.Find ("/goWarning");
		
		//And start GPS
		dLastTimestamp = dNowInEpoch();
		Input.location.Start (1.0f, 0.1f);
	}
	
	// Update is called every frame, if the
	// MonoBehaviour is enabled.
	void Update () {
		//Simple state machine
		if(Input.location.status == LocationServiceStatus.Running ) {
			if(iLocationStatus != 0){
				ShowWarningPSR (false);
			}
			iLocationStatus = 0;
		} else if(iLocationStatus == -1) {
			//Start up phase
			
		} else {
			//Something is not working
			if(iLocationStatus != 1) {
				ShowWarningPSR (true);	
			}
			iLocationStatus = 1;
		}
		
		//Save latest screen orientation
		if(orientationOld != Screen.orientation) {
			//We have changed orientation, save it please!
			if(Screen.orientation == ScreenOrientation.LandscapeLeft ) {
				PlayerPrefs.SetInt("Screen.orientation", 0);	
			} else if (Screen.orientation == ScreenOrientation.LandscapeRight  ) {
				PlayerPrefs.SetInt("Screen.orientation", 1);
			}
			orientationOld = Screen.orientation;
		}
		
		//Okey, we got some kind of position
		if(iLocationStatus == 0) {
			if(Input.location.lastData.timestamp != dLastTimestamp ) {
				//New data from gps!
				CalculateMovement();
				UpdateSpeed(dCurSpeed);
			} else if((dNowInEpoch() - Input.location.lastData.timestamp) > 3.0d) {
				//We are not moving?? Handle speed in a nice way, not a real error!
				UpdateSpeed(-1.0d);
			}
		}
		
		
		
		
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
	
	private void UpdateSpeed(double dSpeed) { 
		if(dSpeed >= 0.0d) {
			double dIntegerPart = Math.Floor(Math.Round (dSpeed, 1));
			double dDecimalPart = Math.Round (dSpeed, 1) - dIntegerPart;
			tmCurrentSpeed.text = dIntegerPart.ToString ("#0");
			tmCurrentSpeedDecimal.text = dDecimalPart.ToString("0.0").Remove (0,1);	
		} else {
			tmCurrentSpeed.text = "--";
			tmCurrentSpeedDecimal.text = ".-";
		}
		
		tmCurrentSpeed.Commit ();
		tmCurrentSpeedDecimal.Commit ();
	}
	
	
	private void CalculateMovement() {
        LocationInfo liTmp = Input.location.lastData;
		
		if(iWaitForGPS  >= 4) {
			//Not the first run
			//Start by calulating the bearing
			dBearing = CalculateBearingTo(dLastLon, dLastLat, (double)liTmp.longitude , (double)liTmp.latitude );
			
			//Okey, try to calculate speed
			double dDist = CalculateDistanceBetweenGPSCoordinates (dLastLon, dLastLat, (double)liTmp.longitude , (double)liTmp.latitude );
			dAccDist += dDist;
			double dTimeDif = Math.Abs(liTmp.timestamp - dLastTimestamp );
			double dMperSec = (dDist / Math.Abs(liTmp.timestamp - dLastTimestamp ));
			double dSpeed = (dMperSec * dMS2KN);
			
			//move around speed
			adAvgSpeed[2] = adAvgSpeed[1];
			adAvgSpeed[1] = adAvgSpeed[0];
			adAvgSpeed[0] = dSpeed;
			
			double dDeltaSpeed = adAvgSpeed[0] - adAvgSpeed[1];
			if(dDeltaSpeed > 5.0d) {
				adAvgSpeed[0] = adAvgSpeed[1]  + 5.0d;
			} else if(dDeltaSpeed <  -5.0d) {
				adAvgSpeed[0] = adAvgSpeed[1]  - 5.0d;
			}
			
			//AVG calculation here
			double dAvgSpeed = adAvgSpeed[0] + adAvgSpeed[1] + adAvgSpeed[2];
			dAvgSpeed /= 3.0d;
			dCurSpeed = dAvgSpeed;
			
		} else {
			//Okey, this is the first run, dont calculate speed
			iWaitForGPS ++;
			dCurSpeed = 0.0;
		}	
		//Save current pos as last pos
		dLastLat = (double)liTmp.latitude;
		dLastLon = (double)liTmp.longitude;
		dLastTimestamp = liTmp.timestamp;   
	}
	
	void ShowScreen(int iScreenId) {
		//Handels showing of the correct screen when swiping and so on...
		if(iScreenId == 2) {
			iTween.MoveTo(gameObject, new Vector3(-102.4f, 0.0f, 0.0f), 1.0f);
			spritePaginationMarked.transform.position = new Vector3(1.0f, -17.8f, -1.0f);
		} else if(iScreenId == 1){
			iTween.MoveTo(gameObject, new Vector3(-51.2f, 0.0f, 0.0f), 1.0f);
			spritePaginationMarked.transform.position = new Vector3(0.0f, -17.8f, -1.0f);
		} else { //Asume 0
			iTween.MoveTo(gameObject, new Vector3(0.0f, 0.0f, 0.0f), 1.0f);
			spritePaginationMarked.transform.position = new Vector3(-1.0f, -17.8f, -1.0f);
		}
	}
	
	void ShowWarningPSR(bool bShow) {
		if(bShow == true){
			//goWarning
			iTween.MoveTo(goWarning, new Vector3(0.0f, -38.4f, -2.2f), 1.0f);
		} else {
			//
			iTween.MoveTo(goWarning, new Vector3(0.0f, 0.0f, -2.2f), 1.0f);
		}
	}
	
	public double CalculateDistanceBetweenGPSCoordinates(double lon1, double lat1, double lon2, double lat2) {
		//Returns in meters
	    const double R = 6378137; 
	    const double degreesToRadians = Math.PI / 180.0d; 
	
	    //convert from fractional degrees (GPS) to radians 
	    lon1 *= degreesToRadians; 
	    lat1 *= degreesToRadians; 
	    lon2 *= degreesToRadians; 
	    lat2 *= degreesToRadians; 
	
	    double dlon = lon2 - lon1; 
	    double dlat = lat2 - lat1; 
	    double a = Math.Pow(Math.Sin(dlat / 2), 2) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin(dlon / 2), 2);
	    double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a)); 
	    double d = R * c; 
	
	    return d; 
	}
	
	public double CalculateBearingTo(double lon1, double lat1, double lon2, double lat2) {
		//BearingTo(double lat, double lng)
		const double degreesToRadians = Math.PI / 180.0d;
		const double RadiansToDegrees = 180.0d / Math.PI;
		
		//convert from fractional degrees (GPS) to radians 
	    lon1 *= degreesToRadians; 
	    lat1 *= degreesToRadians; 
	    lon2 *= degreesToRadians; 
	    lat2 *= degreesToRadians;
	    
		double dLon = lon2 - lon1;
	    double y = Math.Sin(dLon) * Math.Cos(lat2);
	    double x = Math.Cos(lat1) * Math.Sin(lat2) - Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(dLon);
	    double brng = Math.Atan2(y, x);
	    return ((brng * RadiansToDegrees) + 360) % 360;
	}
	
	double dNowInEpoch() {
		TimeSpan span = DateTime.UtcNow.Subtract (new DateTime(1970,1,1,0,0,0));
		return (double)span.TotalSeconds;
	}
	
}
