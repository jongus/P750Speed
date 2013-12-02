using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class ScriptMain : MonoBehaviour {
	//Constants
	private const double  dMS2KN = 1.943844d;
	
	//General variables
	private int iCurrentScreen = 0;
	private int iLocationStatus = -1; //-1=startup, 0=ok, 1=something wrong
	private tk2dSprite spritePaginationMarked;
	private ScreenOrientation orientationOld = ScreenOrientation.LandscapeLeft;
	private int iWaitForGPS = 0; //The first updates tend to be wrong...
	private double dGPSLastTimestamp = 0.0d; //Updates when we move
	private double dCompassLastTimestamp = 0.0d; //Updates when we move
	private double dLastLat = 0.0d;
	private double dLastLon = 0.0d;
	private double[] adAvgSpeed;	
	private double dCurSpeed = -1.0d;
	private double dAccDist = 0.0d;
	private float fAvgHeading = 0.0f;
	private float fHeadingTime = 0.0f;
	private int iHeadingValues = 0;

	
	//Text Meshes
	private tk2dTextMesh tmDebug; //BUG
	private tk2dTextMesh tmCurrentSpeed;
	private tk2dTextMesh tmCurrentSpeedDecimal;
	private tk2dTextMesh tmHeading;
	
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

		//Get data from playerprefs
		iCurrentScreen = PlayerPrefs.GetInt("iCurrentScreen", 0);
		
		//Grab objects
		tmDebug = GameObject.Find ("tmDebug").GetComponent<tk2dTextMesh>(); //BUG
		spritePaginationMarked = GameObject.Find ("spritePaginationMarked").GetComponent<tk2dSprite>();
		tmCurrentSpeed = GameObject.Find ("tmCurrentSpeed").GetComponent<tk2dTextMesh>();
		tmCurrentSpeedDecimal = GameObject.Find ("tmCurrentSpeedDecimal").GetComponent<tk2dTextMesh>();
		tmHeading = GameObject.Find ("tmHeading").GetComponent<tk2dTextMesh>();
		goWarning = GameObject.Find ("/goWarning");
		
		//And start GPS + compass
		adAvgSpeed = new double[3];
		//dGPSLastTimestamp = dNowInEpoch();
		Input.location.Start (1.0f, 0.1f);
		Input.compass.enabled = true;
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

		//Okey, we got some kind of position
		if(iLocationStatus == 0) {
			if(Input.location.lastData.timestamp != dGPSLastTimestamp ) {
				//New data from gps!
				CalculateMovement();
				UpdateSpeed(dCurSpeed);
			} else if((dNowInEpoch() - Input.location.lastData.timestamp) > 3.0d) {
				//We are not moving?? Handle speed in a nice way, not a real error!
				UpdateSpeed(-1.0d);
				//UpdateHeading(-1.0d);
			}
		}
		if(fHeadingTime < 0.2f){
			fAvgHeading += Input.compass.trueHeading;
			iHeadingValues ++;
			fHeadingTime += Time.deltaTime;
		} else {
			UpdateHeading(fAvgHeading / iHeadingValues);
			iHeadingValues = 0;
			fHeadingTime = 0.0f;
			fAvgHeading = 0.0f;
		}
		if(Input.compass.timestamp != dCompassLastTimestamp ){
			dCompassLastTimestamp = Input.compass.timestamp;
		} else if((dNowInEpoch() - Input.compass.timestamp) > 3.0d) {
			UpdateHeading (-1.0f);
		}
		tmDebug.text = Time.deltaTime.ToString ();
		tmDebug.Commit (); //BUG
	}
	
	void OnSwipe(SwipeGesture gesture) {
		Debug.Log ("OnSwipe: " + gesture.Direction );
		if(gesture.Direction == FingerGestures.SwipeDirection.Right) {
			iCurrentScreen --;
			if(iCurrentScreen < 0) iCurrentScreen = 0;
			ShowScreen (iCurrentScreen, 1.0f);
		} else if(gesture.Direction == FingerGestures.SwipeDirection.Left) {
			iCurrentScreen ++;
			if(iCurrentScreen > 2) iCurrentScreen = 2;
			ShowScreen (iCurrentScreen, 1.0f);
		}
	}
	
	void OnApplicationPause(bool pauseStatus) {
        //pauseStatus = true -> applicationDidEnterBackground();
		//pauseStatus = false -> applicationDidBecomeActive();
		if(pauseStatus == true) {
			//We have changed orientation, save it please!
			if(Screen.orientation == ScreenOrientation.LandscapeLeft ) {
				PlayerPrefs.SetInt("Screen.orientation", 0);	
			} else if (Screen.orientation == ScreenOrientation.LandscapeRight  ) {
				PlayerPrefs.SetInt("Screen.orientation", 1);
			}
			PlayerPrefs.SetInt("iCurrentScreen", iCurrentScreen);
		} else {
			iCurrentScreen = PlayerPrefs.GetInt("iCurrentScreen", 0);
			ShowScreen (iCurrentScreen, 0.0f);
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
	
	private void UpdateHeading(float fHeading) { 
		if(fHeading >= 0.0d) {
			tmHeading.text = Math.Round (fHeading, 0).ToString ("#0") + "°";
		} else {
			tmHeading.text = "---°";
		}
		tmHeading.Commit ();
	}
	
	
	
	private void CalculateMovement() {
        LocationInfo liTmp = Input.location.lastData;
		
		if(iWaitForGPS  >= 4) {
			//Not the first run
			//Start by calulating the bearing
			//dBearing = CalculateBearingTo(dLastLon, dLastLat, (double)liTmp.longitude , (double)liTmp.latitude );
			
			//Okey, try to calculate speed
			double dDist = CalculateDistanceBetweenGPSCoordinates (dLastLon, dLastLat, (double)liTmp.longitude , (double)liTmp.latitude );
			dAccDist += dDist;
			double dTimeDif = Math.Abs(liTmp.timestamp - dGPSLastTimestamp );
			double dMperSec = (dDist / Math.Abs(liTmp.timestamp - dGPSLastTimestamp ));
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
		dGPSLastTimestamp = liTmp.timestamp;   
	}
	
	void ShowScreen(int iScreenId, float fSpeed) {
		//Handels showing of the correct screen when swiping and so on...
		if(iScreenId == 2) {
			if(fSpeed > 0.0f) {
				iTween.MoveTo(gameObject, new Vector3(-102.4f, 0.0f, 0.0f), fSpeed);
			} else {
				gameObject.transform.position = new Vector3(-102.4f, 0.0f, 0.0f);
			}
			spritePaginationMarked.transform.position = new Vector3(1.0f, -17.8f, -1.0f);
		} else if(iScreenId == 1){
			if(fSpeed > 0.0f) {
				iTween.MoveTo(gameObject, new Vector3(-51.2f, 0.0f, 0.0f), fSpeed);
			} else {
				gameObject.transform.position = new Vector3(-51.2f, 0.0f, 0.0f);
			}
			spritePaginationMarked.transform.position = new Vector3(0.0f, -17.8f, -1.0f);
		} else { //Asume 0
			if(fSpeed > 0.0f) {
				iTween.MoveTo(gameObject, new Vector3(0.0f, 0.0f, 0.0f), fSpeed);
			} else {
				gameObject.transform.position = new Vector3(0.0f, 0.0f, 0.0f);
			}
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
