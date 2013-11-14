using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class scriptMain : MonoBehaviour {
	private double dCurSpeed = 0.0;
	private double dAccDist = 0.0;
	private int iWaitForGPS = 0;
	private const double  dMS2KN = 1.943844d;
	private double dLastLat = 0;
	private double dLastLon = 0;
	private double[] adAvgSpeed;
	private int iAvgSpeedIdx = 0;	
	private double dLastTimestamp = 0.0d;
	private double dBearing = 0.0d;
	
	private Color cRed = new Color(1.000000000f,0.266666667f,0.180392157f, 1.0f);
	private Color cBlue = new Color(0.243137255f,0.745098039f,1.000000000f, 1.0f);
	private Color cGrey = new Color(0.635294118f,0.698039216f,0.749019608f, 1.0f);
	private Color cWhite = new Color(1.0f, 1.0f, 1.0f, 1.0f);
	
	private tk2dTextMesh tmStatus;
	private tk2dTextMesh tmMaxSpeed;
	private tk2dTextMesh tmAvgSpeed;
	private tk2dTextMesh tmCurSpeed;
	private tk2dTextMesh tmLapNumber;
	private tk2dTextMesh tmLapTime;
	private tk2dTextMesh tmGPS;
	private tk2dTextMesh tmBigInfo;
	private tk2dTextMesh tmBigInfoCaption;
	
	private tk2dButton btnStartStop;
	private tk2dSprite spriteStartStop;
	private tk2dButton btnSettings;
	private tk2dSprite spriteSettings;
	
	public enum GpsStates
    {
        Begin,
        Running,
		Initializing,
		Faild,
		Stoped
    }
	
	private GpsStates gpsState = GpsStates.Begin;
	private GpsStates oldGpsState = GpsStates.Begin;

	private bool bRunning = false;
	
	
	
	System.IO.StreamWriter fwDebug = null; //DEBUG
	string sDebugFile;
	
	 
	// Start is called just before any of the
	// Update methods is called the first time.
	void Start () {
		// Grab all textmeshes we gonna change
    	tmStatus = GameObject.Find ("tmStatus").GetComponent<tk2dTextMesh>(); //DEBUG
		tmMaxSpeed = GameObject.Find ("tmMaxSpeed").GetComponent<tk2dTextMesh>();
		tmAvgSpeed = GameObject.Find ("tmAvgSpeed").GetComponent<tk2dTextMesh>();
		tmCurSpeed = GameObject.Find ("tmCurSpeed").GetComponent<tk2dTextMesh>();
		tmLapNumber = GameObject.Find ("tmLapNumber").GetComponent<tk2dTextMesh>();
		tmLapTime = GameObject.Find ("tmLapTime").GetComponent<tk2dTextMesh>();
		tmGPS = GameObject.Find ("tmGPS").GetComponent<tk2dTextMesh>();
		tmBigInfo = GameObject.Find ("tmBigInfo").GetComponent<tk2dTextMesh>();
		tmBigInfoCaption = GameObject.Find ("tmBigInfoCaption").GetComponent<tk2dTextMesh>();
		// And the start/stop and settings button
		spriteStartStop = GameObject.Find ("spriteStartStop").GetComponent<tk2dSprite>();
		btnStartStop = spriteStartStop.GetComponent<tk2dButton>();
		spriteSettings = GameObject.Find ("spriteSettings").GetComponent<tk2dSprite>();
		btnSettings = spriteSettings.GetComponent<tk2dButton>();
		
		//Variables
		adAvgSpeed = new double[3]; //BUG??
		
		sDebugFile = Application.persistentDataPath + "/debug.txt";
		fwDebug = System.IO.File.CreateText(sDebugFile);
		
		//Start the location service
		Input.location.Start(1.0f, 0.1f);
		ResetGUI();
		
	}
	
	void OnApplicationQuit() {
        Input.location.Stop ();
		
		//PlayerPrefs.Save();
    }
	
	void DebugLog(string sMsg) {
		fwDebug.WriteLine (sMsg);
		fwDebug.Flush ();
	}
	
	void ResetGUI(){
		tmMaxSpeed.text = "- kn";
		tmAvgSpeed.text = "- kn";
		tmCurSpeed.text = "- kn";
		tmLapNumber.text = "0";
		tmLapTime.text = "0:00.0";
		tmGPS.text = "-- m";
		tmBigInfoCaption.text = "STOP";
		tmBigInfo.text = ".";
		
		tmMaxSpeed.Commit ();
		tmAvgSpeed.Commit ();
		tmCurSpeed.Commit ();
		tmLapNumber.Commit ();
		tmLapTime.Commit ();
		tmGPS.Commit ();
		tmBigInfoCaption.Commit ();
		tmBigInfo.Commit ();
		
		Screen.sleepTimeout = SleepTimeout.NeverSleep;
	}
	
	
	
	void EnableStartStop(bool bEnable) {
		tk2dBaseSprite baseSprite = spriteStartStop.GetComponent<tk2dBaseSprite>();
		if(baseSprite) {
			if(bEnable) {
				spriteStartStop.spriteId = baseSprite.GetSpriteIdByName("Start_Normal");
				btnStartStop.enabled = true;
			} else {
				spriteStartStop.spriteId = baseSprite.GetSpriteIdByName("Start_Disabel");
				btnStartStop.enabled = false;
				//Add all tm here! BUG
				tmGPS.text = "-- m";
				tmGPS.Commit ();
			}	
		}
	}
	
	void ShowWarning(bool bEnableWarning){
		GameObject[] goEnableDisableTexts = GameObject.FindGameObjectsWithTag("EnableDisableText");
		//Enable/disable text color
		foreach (GameObject goEnableDisableText in goEnableDisableTexts) {
			tk2dTextMesh tmEnableDisableText = goEnableDisableText.GetComponent<tk2dTextMesh>();
			if(tmEnableDisableText != null) {
				//We have a textmesh with tag "EnableDisableText"
				if(bEnableWarning == true) {
					tmEnableDisableText.color = cRed;
				} else {
					tmEnableDisableText.color = cBlue;
				}
				tmEnableDisableText.Commit ();
			}
		}
		if(bEnableWarning == true) {
			tmCurSpeed.text = "--- kn";
			tmCurSpeed.Commit();
			tmGPS.text = "--- m";
			tmGPS.Commit ();
			iWaitForGPS = 0;
		}
	}
		
	private void UpdateSpeed() {
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
			
			//sRetVal = Math.Round (dAvgSpeed,0).ToString ("#0") + "";
			
			//DEBUG
			/*fwDebug.Write(liTmp.longitude.ToString() + "\t");
			fwDebug.Write(liTmp.latitude.ToString() + "\t");
			fwDebug.Write(liTmp.timestamp.ToString() + "\t");
			fwDebug.Write(liTmp.horizontalAccuracy.ToString() + "\t");
			fwDebug.Write(dDist.ToString() + "\t");
			fwDebug.Write(dTimeDif.ToString() + "\t");
			fwDebug.Write(dMperSec.ToString() + "\t");
			fwDebug.Write(dSpeed.ToString() + "\t");
			fwDebug.Write(adAvgSpeed[0].ToString() + "\t");
			fwDebug.Write(dAvgSpeed.ToString() + "\t");
			fwDebug.Write(sRetVal + "\n");
			fwDebug.Flush (); */
		} else {
			//Okey, this is the first run, dont calculate speed
			iWaitForGPS ++;
			dCurSpeed = 0.0;
			//tmCurSpeed.text = Math.Round (dCurSpeed,0).ToString ("#0");
			//tmCurSpeed.Commit();
		}	
		//Save current pos as last pos
		dLastLat = (double)liTmp.latitude;
		dLastLon = (double)liTmp.longitude;
		dLastTimestamp = liTmp.timestamp;   
	}
	
	// Update is called every frame, if the
	// MonoBehaviour is enabled.
	void Update () {
		//Make new state
		if(Input.location.status == LocationServiceStatus.Running ) {
			gpsState = GpsStates.Running;
		} else if(Input.location.status == LocationServiceStatus.Failed ) {
			gpsState = GpsStates.Faild;
		} else if(Input.location.status == LocationServiceStatus.Initializing  ) {
			gpsState = GpsStates.Initializing;
		} else if(Input.location.status == LocationServiceStatus.Stopped   ) {
			gpsState = GpsStates.Stoped;
		} 
		
		//GPS FSM
		switch (gpsState) {
		case GpsStates.Begin:
			if(gpsState != oldGpsState ) {
				//Enter state
			}
		    break;
		case GpsStates.Faild:
			if(gpsState != oldGpsState ) {
				//Enter state
				tmStatus.text = "No GPS data!";
				tmStatus.Commit ();
				ShowWarning (true);
				EnableStartStop (false);
			}
			
		    break;
		case GpsStates.Initializing:
			if(gpsState != oldGpsState ) {
				//Enter state
				tmStatus.text = "No GPS data!";
				tmStatus.Commit ();
				ShowWarning (true);
				EnableStartStop (false);
			}
			
		    break;
		case GpsStates.Running:
			if(gpsState != oldGpsState ) {
				//Enter state
				tmStatus.text = "";
				tmStatus.Commit ();
				ShowWarning (false);
				EnableStartStop (true);
			}
			//Do what we should!
			if(Input.location.lastData.timestamp != dLastTimestamp ) {
				//New data from gps!
				UpdateSpeed();
				tmCurSpeed.text = Math.Round (dCurSpeed ,0).ToString ("#0") + "kn";
				tmCurSpeed.Commit();
				tmGPS.text = Input.location.lastData.horizontalAccuracy.ToString ("#0") + " m";
				tmGPS.Commit ();
				tmBigInfo.text = dBearing.ToString("#0");
				tmBigInfo.Commit();
				
			} else if((dNowInEpoch() - Input.location.lastData.timestamp) > 3.0d) {
				//We are not moving?? Handle speed in a nice way, not a real error!
				tmCurSpeed.text  = "--- kn";
				tmCurSpeed.Commit();
			} 
		    break;
		case GpsStates.Stoped:
			if(gpsState != oldGpsState ) {
				//Enter state
				tmStatus.text = "No GPS data!";
				tmStatus.Commit ();
				ShowWarning (true);
				EnableStartStop (false);
			}
		    break;
		}
		//Save old state
		oldGpsState = gpsState;	
	}
	
	
	double dNowInEpoch() {
		TimeSpan span = DateTime.UtcNow.Subtract (new DateTime(1970,1,1,0,0,0));
		return (double)span.TotalSeconds;
	}
	
	void StartStopClicked(){
		if(bRunning == true) {
			//Okey, we have clicked stop, show play 
			btnStartStop.buttonDownSprite = "Start_Highlight";
			btnStartStop.buttonUpSprite = "Start_Normal";
			btnStartStop.buttonPressedSprite = "Start_Normal";
			bRunning = false;
		} else {
			btnStartStop.buttonDownSprite = "Stop_Highlight";
			btnStartStop.buttonUpSprite = "Stop_Normal";
			btnStartStop.buttonPressedSprite = "Stop_Normal";
			bRunning = true;
		}
		btnStartStop.UpdateSpriteIds ();
	}
	
	void SettingsClicked(){
		
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
	
}

