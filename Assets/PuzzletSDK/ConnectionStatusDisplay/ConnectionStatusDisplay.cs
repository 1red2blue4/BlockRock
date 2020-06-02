using UnityEngine;
using System.Collections;
using Puzzlets;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ConnectionStatusDisplay : MonoBehaviour, PuzzletConnectionStateReceiver
{
	public GameObject root;
	public GameObject rootAnim;
	public GameObject[] texts;

	public GameObject interactionTimeoutPrompt;
	
	static ConnectionStatusDisplay instance;
	public static bool present{get{return instance && instance.root.activeSelf;}}

	const int tooFarDelay = 3;
	static int tooFarTimes = 0;

	public int handshakeRepeatsToHide = 3;
	int handshakeRepeats = 0;
	public GameObject handshakeRepeatText;

	public float noTrayFoundDelay = 8;
	float noTrayFoundTime = -1;
	public GameObject noTrayFoundText;

	public FirmwareVersionCheck firmwareVersionCheck;
	public Text firmwareVersionText;

	public Animator anim;
	public Animator textAnim;

	public void PuzzletConnectionStateChanged(PuzzletConnection.State lastState, PuzzletConnection.State state)
	{
		Debug.Log("PuzzletConnection last state: " + lastState + ". New state: " + state);

		interactionTimeoutPrompt.SetActive(false);

		bool showNewState = true;

		//hiding searching/connecting loop
		if(lastState == PuzzletConnection.State.Handshaking && state == PuzzletConnection.State.Disconnected)
			++handshakeRepeats;

		if(handshakeRepeats > 0)
		{
			if(state == PuzzletConnection.State.Handshaking || state == PuzzletConnection.State.Disconnected)
				showNewState = false;
			else
			{
				handshakeRepeats = 0;
				handshakeRepeatText.SetActive(false);
			}

			if(handshakeRepeats > handshakeRepeatsToHide)
				handshakeRepeatText.SetActive(true);
		}

		//checking if no tray found in a reasonable time frame (see also Update)
		#if !(UNITY_STANDALONE || UNITY_EDITOR)
		if(state == PuzzletConnection.State.Disconnected)
			noTrayFoundTime = 0;
		else
			TrayFound();
		#endif

		//enabling/disabling the display
		StopAllCoroutines();

		if(state == PuzzletConnection.State.Connected)
		{
			firmwareVersionText.text = PuzzletUtility.FirmwareString().Substring(0,5);

			StartCoroutine(this.Disable());
		}
		else
		{
			root.SetActive(true);
			rootAnim.SetActive(true);
		}

		//updating contents of display
		if(showNewState)
		{
			for(int a = 0; a < texts.Length; ++a)
				texts[a].SetActive(false);
			
			texts[(int) state].SetActive(true);

			string triggerName = "NO_TRIGGER";

			#if UNITY_STANDALONE || UNITY_EDITOR
			if(state == PuzzletConnection.State.Disconnected)
				triggerName = "ConnectUSB";
			else if (state == PuzzletConnection.State.Handshaking)
				triggerName = "ConnectingUSB";
			else if(state == PuzzletConnection.State.Connected)
				triggerName = "ConnectedUSB";
			#else
			if(state == PuzzletConnection.State.Disconnected)
				triggerName = "ConnectBluetooth";
			else if (state == PuzzletConnection.State.Handshaking)
				triggerName = "ConnectingBluetooth";
			else if(state == PuzzletConnection.State.Connected)
				triggerName = "ConnectedBluetooth";
			#endif

			/*if(state == PuzzletConnection.State.Disconnected)
				triggerName = "ConnectBluetooth";
			else if (state == PuzzletConnection.State.Handshaking)
				triggerName = "ConnectingBluetooth";
			else if(state == PuzzletConnection.State.Connected)
				triggerName = "ConnectedBluetooth";*/

			Debug.Log("Connection Status Animation Triggered: " + triggerName);
			anim.SetTrigger(triggerName);
		}

		//text in/out
		if(state == PuzzletConnection.State.Connected)
			textAnim.SetTrigger("ScaleOut");
		else
			textAnim.SetTrigger("ScaleIn");
		
		tooFarTimes = 0;
	}
	
	IEnumerator Disable()
	{
		while(!firmwareVersionCheck.passed)
			yield return 0;

		while(!anim.GetCurrentAnimatorStateInfo(0).IsName("Done"))
			yield return 0;
		
		if(SceneManager.GetActiveScene().name.Contains("ConnectionScene"))
			SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
		
		root.SetActive(false);
		rootAnim.SetActive(false);
	}
	
	void Awake()
	{
		if(instance)
			Destroy(gameObject);
		instance = this;

		PuzzletConnection.RegisterReceiver(this);

		DontDestroyOnLoad(gameObject);
		DontDestroyOnLoad(rootAnim);

		//set texts to appropriate for device
		#if UNITY_STANDALONE || UNITY_EDITOR
		string deleteStr = "Bluetooth";
		#else
		string deleteStr = "USB";
		#endif

		for(int a = 0; a < texts.Length; ++a)
		{
			for(int b = 0; b < texts[a].transform.childCount; ++b)
			{
				Transform child = texts[a].transform.GetChild(b);
				if(child.name == deleteStr)
					Destroy(child.gameObject);
			}
		}
	}


	void Update()
	{
		#if !(UNITY_STANDALONE || UNITY_EDITOR)
		if(noTrayFoundTime >= 0)
		{
			float timeOld = noTrayFoundTime;
			noTrayFoundTime += Time.unscaledDeltaTime;

			if(noTrayFoundTime >= noTrayFoundDelay && timeOld < noTrayFoundDelay)
				noTrayFoundText.SetActive(true);
		}
		#endif

		if(PuzzletConnection.Connected && firmwareVersionCheck.passed)
		{
			if(PuzzletConnection.interactionTimedOut)
			{
				root.SetActive(true);
				interactionTimeoutPrompt.SetActive(true);
				rootAnim.SetActive(false);
			}
			else if(interactionTimeoutPrompt.activeSelf)
			{
				root.SetActive(false);
				interactionTimeoutPrompt.SetActive(false);
				rootAnim.SetActive(true);
			}
		}
	}
	
	void TooFarInst(bool several)
	{
		for(int a = 0; a < texts.Length; ++a)
			texts[a].SetActive(false);
		
		int offset = several? 1:0;
		texts[3 + offset].SetActive(true);
		
		anim.SetTrigger(several? "TooFarMultiple" : "TooFarSingle");
		tooFarTimes = int.MinValue; //lock out tooFarTimes until next state change to avoid any animation weirdness
	}
	
	public static void TooFar(bool several)
	{
		Debug.Log(several? "2+ play trays too far!":"Play tray too far!");
		
		++tooFarTimes;
		
		if(tooFarTimes >= tooFarDelay)
			instance.TooFarInst(several);

		instance.TrayFound();
	}

	void TrayFound()
	{
		noTrayFoundTime = -1;
		noTrayFoundText.SetActive(false);
	}
}