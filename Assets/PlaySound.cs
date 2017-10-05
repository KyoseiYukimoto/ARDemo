using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaySound : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	// Play or Stop
	public void PlayOrStop()
	{
		AudioSource a = GetComponent<AudioSource> ();
		if (a == null) {
			return;
		}
		if (a.isPlaying) {
			a.Stop ();
		} else {
			a.Play ();
		}
	}
}
