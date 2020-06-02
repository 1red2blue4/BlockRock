using UnityEngine;
using System;  // Needed for Math
using UnityEngine.Events;
using Puzzlets;
using System.Collections;
using System.Collections.Generic;

public class Sinus2 : MonoBehaviour , IPuzzletReceiver
{
	// un-optimized version
	private float[] yPos=new float[2400];
	private float[] yPosSong=new float[2400];
	private int SoundWaveFrequency=2400;
	public GameObject SoundWave;
	public GameObject SoundWave2;
	public GameObject SoundWave3;
	public GameObject SoundWaveSong;
	public GameObject bar;
	private int counter;
	public int musicSpeed;
	private int delay;
	public int[] song;
	private double[] gains  	 = new double[90];
	private double[] phases      = new double[90];
	private double[] frequencies = new double[90];
	private int[] 	 notes		 = new int[90];
	private double[] songGains  	 = new double[90];
	private double[] songPhases      = new double[90];
	private double[] songFrequencies = new double[90];
	private int[] 	 songNotes		 = new int[90];
	private double increment;
	private double sampling_frequency = 48000;
	private System.Random RandomNumber=new System.Random();
	private float TimeOffset;
	private int prevTime;
	private bool playingSong;
	private bool lowNotes;
	private bool mediumNotes;
	private bool highNotes;

	public GameObject herGrey;
	public GameObject hisGrey;
	public GameObject victory;

	void Start ()
	{

		PuzzletManager.RegisterReceiver (this);
		PuzzletConnection.StartScanning();
		musicSpeed=4;
		lowNotes=true;
		mediumNotes=true;
		highNotes=true;

	}

	void OnAudioFilterRead(float[] data, int channels)
	{
		// update increment in case frequency has changed
		increment =  2 * Math.PI / sampling_frequency;
		decreaseGain();
		decreaseGain();

		bool startPoint=false;

		for (var i = 0; i < data.Length; i = i + channels)
		{
			if(Mathf.Abs(data[i]*5)<0.0001f && data[i]*5<=data[i+1]*5){
				startPoint=true;
			}
			for(int j=0;j<90;j++){//increment phases based on frequencies
				phases[j]=phases[j]+increment*frequencies[j];
				songPhases[j]=songPhases[j]+increment*songFrequencies[j];
			}

			// this is where we copy audio data to make them “available” to Unity
			if(playingSong){
				data[i] = 0;//POPULATE BOARD DATA FIRST
				for(int j=0;j<30;j++){//add all the current notes together
					if(notes[j]==0){
						data[i]+=(float)flute(phases[j],gains[j],j);
					}
					if(notes[j]==3){
						data[i]+=(float)flute(phases[j],gains[j],j)+(float)flute(phases[j+30],gains[j+30],j+30)+(float)flute(phases[j+60],gains[j+60],j+60);
					}
				}
				if(i%2==0 && counter<SoundWaveFrequency-1 && counter>=0 && startPoint){//generate sound wave
					yPos[counter]=(data[i]*5);
					yPos[counter+1]=(data[i]*5);
				}


				data[i] = 0;//OVERWRITE WITH SONGDATA
				for(int j=0;j<30;j++){//add all the current notes together
					if(songNotes[j]==0)
						data[i]+=(float)songFlute(songPhases[j],songGains[j],j);
					if(songNotes[j]==3){
						data[i]+=(float)songFlute(songPhases[j],songGains[j],j)+(float)flute(songPhases[j+30],songGains[j+30],j+30)+(float)flute(songPhases[j+60],songGains[j+60],j+60);
					}
				}
				if(i%2==0 && counter<SoundWaveFrequency-1 && counter>=0 && startPoint){//generate sound wave
					yPosSong[counter]=(data[i]*5);
					yPosSong[counter+1]=(data[i]*5);
					counter+=2;

				}

			}
			else{
				data[i] = 0;//OVERWRITE WITH SONGDATA
				for(int j=0;j<30;j++){//add all the current notes together
					if(songNotes[j]==0)
						data[i]+=(float)songFlute(songPhases[j],songGains[j],j);
					if(songNotes[j]==3){
						data[i]+=(float)songFlute(songPhases[j],songGains[j],j)+(float)flute(songPhases[j+30],songGains[j+30],j+30)+(float)flute(songPhases[j+60],songGains[j+60],j+60);
					}
				}
				if(i%2==0 && counter<SoundWaveFrequency-1 && counter>=0 && startPoint){//generate sound wave
					yPosSong[counter]=(data[i]*5);
					yPosSong[counter+1]=(data[i]*5);
				}

				data[i] = 0;
				for(int j=0;j<30;j++){//add all the current notes together
					if(notes[j]==0)
						data[i]+=(float)flute(phases[j],gains[j],j);
					if(notes[j]==3){
						data[i]+=(float)flute(phases[j],gains[j],j)+(float)flute(phases[j+30],gains[j+30],j+30)+(float)flute(phases[j+60],gains[j+60],j+60);
					}
				}
				if(i%2==0 && counter<SoundWaveFrequency-1 && counter>=0 && startPoint){//generate sound wave
					yPos[counter]=(data[i]*5);
					yPos[counter+1]=(data[i]*5);
					counter+=2;

				}
			}
			if(delay>0){
				data[i]=0;
			}

			// if we have stereo, we copy the mono data to each channel
			if (channels == 2) data[i + 1] = data[i];
			for(int j=0;j<90;j++){
				if (phases[j] > 2 * Math.PI) phases[j] = 0;
				if (songPhases[j] > 2 * Math.PI) songPhases[j] = 0;
			}
		}
	}

	void decreaseGain(){
		for(int j=0;j<90;j++){
			if(gains[j]>0)
				gains[j]-=0.0005*musicSpeed;
			if(songGains[j]>0)
				songGains[j]-=0.0005*musicSpeed;
		}
	}



	void Update(){
		if(Input.GetKey(KeyCode.Space)){
			return;
		}
		if(victory.activeSelf){
			return;
		}
		bool test=true;
		for(int i=0;i<30;i++){
			int piece=0;

			piece = PuzzletConnection.RawPuzzletData [i];
			if(piece!=song[i])test=false;
		}
		//if(test)victory.SetActive(true);
		float timeVal=Time.time*musicSpeed+TimeOffset;
		if(counter>=SoundWaveFrequency){
			

				SoundWave.transform.GetChild(prevTime).GetComponent<SoundWave>().yPos=yPos;
				SoundWave.transform.GetChild(prevTime).GetComponent<SoundWave>().reGraph();

				SoundWave2.transform.GetChild(prevTime).GetComponent<SoundWave>().yPos=yPos;
				SoundWave2.transform.GetChild(prevTime).GetComponent<SoundWave>().reGraph();

				SoundWave3.transform.GetChild(prevTime).GetComponent<SoundWave>().yPos=yPos;
				SoundWave3.transform.GetChild(prevTime).GetComponent<SoundWave>().reGraph();

			SoundWaveSong.transform.GetChild(prevTime).GetComponent<SoundWave>().yPos=yPosSong;
			SoundWaveSong.transform.GetChild(prevTime).GetComponent<SoundWave>().reGraph();
			for(int i=0;i<6;i++){
				Vector3 pos=SoundWave.transform.GetChild(i).transform.localPosition;
				SoundWave.transform.GetChild(i).transform.localPosition=new Vector3(pos.x,pos.y,100);
				pos=SoundWave2.transform.GetChild(i).transform.localPosition;
				SoundWave2.transform.GetChild(i).transform.localPosition=new Vector3(pos.x,pos.y,105);
				pos=SoundWave3.transform.GetChild(i).transform.localPosition;
				SoundWave3.transform.GetChild(i).transform.localPosition=new Vector3(pos.x,pos.y,105);
			}
			for (int i = 0; i < 30; i++) {
				int piece=0;

				piece = PuzzletConnection.RawPuzzletData [i];
				if(piece!=0){
					if(piece==1 || piece==3 || piece==5 || piece==4){
						Vector3 pos=SoundWave2.transform.GetChild(i%6).transform.localPosition;
						SoundWave2.transform.GetChild(i%6).transform.localPosition=new Vector3(pos.x,pos.y,0);
					}
					if(piece==5 || piece==2 || piece==3 || piece==6){
						Vector3 pos=SoundWave3.transform.GetChild(i%6).transform.localPosition;
						SoundWave3.transform.GetChild(i%6).transform.localPosition=new Vector3(pos.x,pos.y,0);
					}
					if(piece==5 || piece==7 || piece==2 || piece==1){
						Vector3 pos=SoundWave.transform.GetChild(i%6).transform.localPosition;
						SoundWave.transform.GetChild(i%6).transform.localPosition=new Vector3(pos.x,pos.y,0);
					}
				}
			}
		}
		if((int)(timeVal)%6!=prevTime){

			counter=0;

		}
		prevTime=(int)timeVal%6;

		bar.transform.position=new Vector3( ((11*(timeVal%6))-33),1,35);

		bool playButton=false;
		RaycastHit hit;
		Ray ray = transform.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);

		if (Physics.Raycast(ray, out hit)) {
			Transform objectHit = hit.transform;
			if(objectHit.name=="Play")playButton=true;
			// Do something with the object that was hit by the raycast.
		}
		if(Input.GetMouseButton(0) && playButton){
			playingSong=true;
			if(Input.GetMouseButtonDown(0))delay=15;
		}
		else{
			playingSong=false;
		}
		if(Input.GetMouseButtonUp(0) && (Input.mousePosition.x>740) && (Input.mousePosition.x<820)
			&& (Input.mousePosition.y>330) && (Input.mousePosition.y<415)){
			delay=15;
		}


		if(Input.GetKeyDown(KeyCode.Alpha1)){
			lowNotes=!lowNotes;
		}
		if(Input.GetKeyDown(KeyCode.Alpha2)){
			mediumNotes=!mediumNotes;
		}
		if(Input.GetKeyDown(KeyCode.Alpha3)){
			highNotes=!highNotes;
		}
		if(Input.GetKeyDown(KeyCode.R)){
			randomSong();
			delay=15;
		}
		/*if(Input.GetKeyDown(KeyCode.Alpha1)){
			musicSpeed=2;
			delay=15;
		}
		if(Input.GetKeyDown(KeyCode.Alpha2)){
			musicSpeed=4;
			delay=15;
		}
		if(Input.GetKeyDown(KeyCode.Alpha3)){
			musicSpeed=8;
			delay=15;
		}*/

		if(delay>0){
			for(int j=0;j<90;j++){
				gains[j]=0;
				songGains[j]=0;
			}
			delay--;
			if(delay==0){
				TimeOffset-=timeVal;
				timeVal=Time.time*musicSpeed+TimeOffset;
			}

		}

		if(playingSong){
			hisGrey.SetActive(true);
			herGrey.SetActive(false);
		}
		else{
			hisGrey.SetActive(false);
			herGrey.SetActive(true);
		}

		for (int i = 0; i < 30; i++) {
			int piece=0;

			piece = PuzzletConnection.RawPuzzletData [i];
			//print(piece);
			if(Math.Abs((int)timeVal%6-i%6)<1 && piece!=0 && gains[i]<0.001){

				gains[i]=0.05;
				phases[i]=0;
				if(i/6==4)
					frequencies[i]=262;
				if(i/6==3)
					frequencies[i]=294;
				if(i/6==2)
					frequencies[i]=330;
				if(i/6==1)
					frequencies[i]=392;
				if(i/6==0)
					frequencies[i]=440;


				notes[i]=0;
				if(piece==5 || piece==207){
					notes[i]=3;
					gains[i+30]=0.05;
					gains[i+60]=0.05;
					frequencies[i+30]=frequencies[i]*2;
					frequencies[i+60]=frequencies[i]/2;
				}
				if(piece==2){
					notes[i]=3;
					gains[i+30]=0.05;
					frequencies[i+30]=frequencies[i]*2;
				}
				if(piece==1){
					notes[i]=3;
					gains[i+60]=0.05;
					frequencies[i+60]=frequencies[i]/2;
				}
				if(piece==6){
					frequencies[i]*=2;
				}
				if(piece==7){
					frequencies[i]/=2;
				}
			}

			piece = song [i];

			if(Math.Abs((int)timeVal%6-i%6)<1 && piece!=0 && songGains[i]<0.001){
				songPhases[i]=0;
				songGains[i]=0.05;
				if(i/6==4)
					songFrequencies[i]=262;
				if(i/6==3)
					songFrequencies[i]=294;
				if(i/6==2)
					songFrequencies[i]=330;
				if(i/6==1)
					songFrequencies[i]=392;
				if(i/6==0)
					songFrequencies[i]=440;


				songNotes[i]=0;
				if(piece==5){
					songNotes[i]=3;
					songGains[i+30]=0.05;
					songGains[i+60]=0.05;
					songFrequencies[i+30]=frequencies[i]*2;
					songFrequencies[i+60]=frequencies[i]/2;
				}
				if(piece==2){
					songNotes[i]=3;
					songGains[i+30]=0.05;
					songFrequencies[i+30]=frequencies[i]*2;
				}
				if(piece==1){
					songNotes[i]=3;
					songGains[i+60]=0.05;
					songFrequencies[i+60]=frequencies[i]/2;
				}
				if(piece==6){
					songFrequencies[i]*=2;
				}
				if(piece==7){
					songFrequencies[i]/=2;
				}
			}


		}

	}

	public void PuzzletChanged (PuzzletData[] removedTiles, PuzzletData[] addedTiles)
	{ 
		return;
	}

	double flute(double phase,double gain,int index){
		if(frequencies[index]<262 && !lowNotes)return 0;
		if(frequencies[index]>440 && !highNotes)return 0;
		if(frequencies[index]>=262 && frequencies[index]<=440 && !mediumNotes)return 0;
		return gain*Math.Sin(phase);//+gain*Math.Sin(phase*2)+gain*.5*Math.Sin(phase*3)+gain*.25*Math.Sin(phase*4)*(1+RandomNumber.NextDouble()/5);
	}

	double songFlute(double phase,double gain,int index){
		if(songFrequencies[index]<262 && !lowNotes)return 0;
		if(songFrequencies[index]>440 && !highNotes)return 0;
		if(songFrequencies[index]>=262 && songFrequencies[index]<=440 && !mediumNotes)return 0;
		return gain*Math.Sin(phase);//+gain*Math.Sin(phase*2)+gain*.5*Math.Sin(phase*3)+gain*.25*Math.Sin(phase*4)*(1+RandomNumber.NextDouble()/5);
	}
	double piano(double phase,double gain){
		return gain*.5*Math.Sin(phase)+gain*2.4*Math.Sin(phase*2)+.25*gain*Math.Sin(phase*3)+.4*gain*Math.Sin(phase*5)*(1+RandomNumber.NextDouble()/5);
	}

	double sawWave(double phase,double gain){

		return gain*Math.Sin(phase)+gain*.8*Math.Sin(phase*2)+gain*1.2*Math.Sin(phase/2);
	}

	void randomSong(){
		for(int j=0;j<30;j++){
			if(RandomNumber.NextDouble()>.80){
				song[j]=6;
			}
			else if(RandomNumber.NextDouble()>.60){
				song[j]=7;
			}
			else if(RandomNumber.NextDouble()>.40){
				song[j]=4;
			}
			else {
				song[j]=0;
			}
		}
	}
} 