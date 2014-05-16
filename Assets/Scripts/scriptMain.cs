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
	//private double dAccDist = 0.0d;
	private double dCurTrip = 0.0d;
	private double dCurMax = 0.0d;
	private double dCurAvg = 0.0d;
	private double dCurAvgTrip = 0.0d;
	private float fAvgHeading = 0.0f;
	private float fHeadingTime = 0.0f;
	private float fCurHeading = 0.0f;
	private float fCurAccuracy = 0.0f;
	private int iHeadingValues = 0;
	private DateTime dtStartAvg = new DateTime();
	private DateTime dtStopAvg = new DateTime();
	private int iRunAvg = 0; //0 = do not run, 1 = did +10kn, 2 = did +20kn, 3 = did -10kn -> DONE 
	
	//Text Meshes
	private tk2dTextMesh tmCurrentSpeed;
	private tk2dTextMesh tmCurrentSpeedDecimal;
	private tk2dTextMesh tmHeading;
	private tk2dTextMesh tmAccuracy;
	private tk2dTextMesh tmTrip;
	private tk2dTextMesh tmTripDecimal;
	private tk2dTextMesh tmMax;
	private tk2dTextMesh tmMaxDecimal;
	private tk2dTextMesh tmAvg;
	private tk2dTextMesh tmAvgDecimal;
	private tk2dTextMesh tmDebug;


	private Color cWhite = new Color(0.925490196f,0.925490196f,0.686274510f, 1.0f);
	private Color cRed = new Color(0.905882353f,0.235294118f,0.250980392f, 1.0f);
	private Color cBlue = new Color(0.164705882f,0.537254902f,0.752941176f, 1.0f);


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
		//spritePaginationMarked = GameObject.Find ("spritePaginationMarked").GetComponent<tk2dSprite>();
		tmCurrentSpeed = GameObject.Find ("tmCurrentSpeed").GetComponent<tk2dTextMesh>();
		tmCurrentSpeedDecimal = GameObject.Find ("tmCurrentSpeedDecimal").GetComponent<tk2dTextMesh>();
		tmHeading = GameObject.Find ("tmHeading").GetComponent<tk2dTextMesh>();
		tmAccuracy = GameObject.Find ("tmAccuracy").GetComponent<tk2dTextMesh>();
		tmTrip = GameObject.Find ("tmTrip").GetComponent<tk2dTextMesh>();
		tmTripDecimal = GameObject.Find ("tmTripDecimal").GetComponent<tk2dTextMesh>();
		tmMax = GameObject.Find ("tmMax").GetComponent<tk2dTextMesh>();
		tmMaxDecimal = GameObject.Find ("tmMaxDecimal").GetComponent<tk2dTextMesh>();
		tmAvg = GameObject.Find ("tmAvg").GetComponent<tk2dTextMesh>();
		tmAvgDecimal = GameObject.Find ("tmAvgDecimal").GetComponent<tk2dTextMesh>();
		goWarning = GameObject.Find ("/goWarning");
		
		//And start GPS + compass
		adAvgSpeed = new double[3];
		//dGPSLastTimestamp = dNowInEpoch();
		Input.location.Start (1.0f, 0.1f);
		//Input.compass.enabled = true;

		Screen.sleepTimeout = SleepTimeout.NeverSleep;

		tmDebug.text = "RUN";
		tmDebug.Commit();

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
			} else if((dNowInEpoch() - Input.location.lastData.timestamp) > 3.0d) {
				//We are not moving?? Handle speed in a nice way, not a real error!
				dCurSpeed=-1.0d;
				Input.location.Stop ();
				Input.location.Start (1.0f, 0.1f);
				//UpdateHeading(-1.0d);
			}
		}
		/*if(fHeadingTime < 0.2f){
			float fHeading = Input.compass.trueHeading;
			if(((fAvgHeading / iHeadingValues) < 180.0f) & (fHeading >= 180.0f)) {
				fCurHeading = (fAvgHeading / iHeadingValues); 
				iHeadingValues = 0;
				fHeadingTime = 0.0f;
				fAvgHeading = 0.0f;
			} else if(((fAvgHeading / iHeadingValues) > 180.0f) & (fHeading <= 180.0f)) {
				fCurHeading = (fAvgHeading / iHeadingValues);
				iHeadingValues = 0;
				fHeadingTime = 0.0f;
				fAvgHeading = 0.0f;
			} else {
				fAvgHeading += Input.compass.trueHeading;
				iHeadingValues ++;
				fHeadingTime += Time.deltaTime;
			}
		} else {
			fCurHeading = (fAvgHeading / iHeadingValues);
			iHeadingValues = 0;
			fHeadingTime = 0.0f;
			fAvgHeading = 0.0f;
		}
		if(Input.compass.timestamp != dCompassLastTimestamp ){
			dCompassLastTimestamp = Input.compass.timestamp;
		} else if((dNowInEpoch() - Input.compass.timestamp) > 3.0d) {
			fCurHeading = -1.0f;
		}*/
		//Update text

		UpdateHeading (fCurHeading);
		UpdateSpeed (dCurSpeed);
		UpdateAccuracy (fCurAccuracy);
		UpdateTrip (dCurTrip);
		UpdateMax (dCurMax);
		UpdateAvg (dCurAvg);

		//tmDebug.text = Time.deltaTime.ToString ();
		//tmDebug.Commit (); //BUG
	}

	void OnLongPress(LongPressGesture gesture) {
		if(gesture.Position.y > Screen.height/3) {
			//Reset ALL
			dCurTrip = 0.0d;
			dCurMax = 0.0d;
			dCurAvg = 0.0d;
			iRunAvg = 0;
			tmDebug.text = "RESET ALL";
			tmDebug.Commit();
		} else {
			if(gesture.Position.x > ((Screen.width /3)*2)) {
				dCurTrip = 0.0d;
				tmDebug.text = "RESET TRIP";
				tmDebug.Commit();
			} else if(gesture.Position.x < (Screen.width /3)) {
				dCurAvg = 0.0d;
				iRunAvg = 0;
				dCurAvgTrip = 0.0d;
				tmDebug.text = "RESET AVG";
				tmDebug.Commit();
			} else {
				dCurMax = 0.0d;	
				tmDebug.text = "RESET MAX";
				tmDebug.Commit();
			}

		}
		Debug.Log ("OnLongPressY:" + gesture.Position.y);
		//RESET 
	
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
			//ShowScreen (iCurrentScreen, 0.0f);
		}
	}
	
	private void UpdateSpeed(double dSpeed) { 
		if(dSpeed > 0.0d) {
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
		if(fHeading > 0.0d) {
			tmHeading.text = Math.Round (fHeading, 0).ToString ("#0");
		} else {
			tmHeading.text = "---";
		}
		tmHeading.Commit ();
	}

	private void UpdateAccuracy(float fAccuracy) {
		if(fAccuracy >= 1.0f) {
			tmAccuracy.text = Math.Round (fAccuracy, 0).ToString ("#0");
			if (tmAccuracy.text.Length > 3) {
				tmAccuracy.text = "999";
			}
		} else {
			tmAccuracy.text = "---";
		}
		if(fAccuracy > 20.0f) {
			tmAccuracy.color = cRed;
		} else {
			tmAccuracy.color = cWhite ;
		}
		tmAccuracy.Commit ();
	}

	private void UpdateTrip(double dTrip) {
		if(dTrip > 0.0d) {
			double dTripInNM = dTrip /1852;
			double dIntegerPart = Math.Floor(Math.Round (dTripInNM, 1));
			double dDecimalPart = Math.Round (dTripInNM, 1) - dIntegerPart;
			tmTrip.text = dIntegerPart.ToString ("#0");
			tmTripDecimal.text = dDecimalPart.ToString("0.0").Remove (0,1);
			if(tmTrip.text.Length > 2){
				tmTrip.text = "99";
				tmTripDecimal.text = ".9";
			}
		} else {
			tmTrip.text = "--";
			tmTripDecimal.text = ".-";
		}
		tmTrip.Commit ();
		tmTripDecimal.Commit ();
	}

	private void UpdateMax(double dMax) {
		if(dMax > 0.0d) {
			double dIntegerPart = Math.Floor(Math.Round (dMax, 1));
			double dDecimalPart = Math.Round (dMax, 1) - dIntegerPart;
			tmMax.text = dIntegerPart.ToString ("#0");
			tmMaxDecimal.text = dDecimalPart.ToString("0.0").Remove (0,1);
			if(tmMax.text.Length > 2){
				tmMax.text = "99";
				tmMaxDecimal.text = ".9";
			}
		} else {
			tmMax.text = "--";
			tmMaxDecimal.text = ".-";
		}
		tmMax.Commit ();
		tmMaxDecimal.Commit ();
	}

	private void UpdateAvg(double dAvg) {
		if(dAvg > 0.0d) {
			double dIntegerPart = Math.Floor(Math.Round (dAvg, 1));
			double dDecimalPart = Math.Round (dAvg, 1) - dIntegerPart;
			tmAvg.text = dIntegerPart.ToString ("#0");
			tmAvgDecimal.text = dDecimalPart.ToString("0.0").Remove (0,1);
			if(tmAvg.text.Length > 2){
				tmAvg.text = "99";
				tmAvgDecimal.text = ".9";
			}
		} else {
			tmAvg.text = "--";
			tmAvgDecimal.text = ".-";
		}
		if(iRunAvg == 0) {
			tmAvg.color = cWhite;
			tmAvgDecimal.color = cWhite;
		} else if(iRunAvg == 1)  {
			tmAvg.color = cBlue;
			tmAvgDecimal.color = cBlue;
		}  else if(iRunAvg == 2)  {
			tmAvg.color = cBlue;
			tmAvgDecimal.color = cBlue;
		}
		else {
			tmAvg.color = cWhite;
			tmAvgDecimal.color = cWhite;
		}

		tmAvg.Commit ();
		tmAvgDecimal.Commit ();
	}
	
	private void CalculateMovement() {
        LocationInfo liTmp = Input.location.lastData;
		if(liTmp.horizontalAccuracy < 21.0d){
			if(iWaitForGPS  >= 4) {
				//Not the first run
				//Start by calulating the bearing
				fCurHeading = (float)CalculateBearingTo(dLastLon, dLastLat, (double)liTmp.longitude , (double)liTmp.latitude );
				
				//Okey, try to calculate speed
				double dDist = CalculateDistanceBetweenGPSCoordinates (dLastLon, dLastLat, (double)liTmp.longitude , (double)liTmp.latitude );
				dCurTrip += dDist;
				if((iRunAvg >= 1) && (iRunAvg <= 2)) {
					dCurAvgTrip += dDist;
				}
				
				//dAccDist += dDist;
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
				dCurSpeed = dSpeed; //dAvgSpeed;
				fCurAccuracy = liTmp.horizontalAccuracy;
				if(dAvgSpeed > dCurMax ) {
					dCurMax = dSpeed; //dAvgSpeed;
				}
				if((iRunAvg == 0) && (dAvgSpeed > 10.0d)) {
					iRunAvg = 1;
					dtStartAvg = DateTime.Now;
					dCurAvgTrip = 0.0d;
					tmDebug.text = "Start: " + dtStartAvg.ToString ();
					tmDebug.Commit();
				}
				if((iRunAvg == 1) && (dAvgSpeed > 20.0d)) {
					iRunAvg = 2;
				}
				if((iRunAvg == 2) && (dAvgSpeed < 10.0d)) {
					//Stop
					dtStopAvg = DateTime.Now;
					iRunAvg = 3;
					tmDebug.text = "Start: " + dtStartAvg.ToString () + " Stop: " + dtStopAvg.ToString () + " Dist: " + dCurAvgTrip.ToString ();
					tmDebug.Commit();
				}

				if((iRunAvg == 1) || (iRunAvg == 2)){
					TimeSpan span = DateTime.Now.Subtract(dtStartAvg );
					if((double)span.TotalSeconds > 0.0d) {
						dCurAvg = (dCurAvgTrip/(double)span.TotalSeconds)*dMS2KN;
					}
				} else if(iRunAvg == 3) {
					TimeSpan span = dtStopAvg.Subtract(dtStartAvg );
					if((double)span.TotalSeconds > 0.0d) {
						dCurAvg = (dCurAvgTrip/(double)span.TotalSeconds)*dMS2KN;
					}
				} else {
					dCurAvg = 0.0d;
				}


			} else {
				//Okey, this is the first run, dont calculate speed
				iWaitForGPS ++;
				dCurSpeed = -1.0d;
			}	
			//Save current pos as last pos
			dLastLat = (double)liTmp.latitude;
			dLastLon = (double)liTmp.longitude;
			dGPSLastTimestamp = liTmp.timestamp;
		} else {
			fCurAccuracy = liTmp.horizontalAccuracy;
			dCurSpeed = 0.0d;
			fCurHeading = 0.0f;
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
