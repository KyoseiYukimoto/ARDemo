using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaneManager : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		GameObject cubeGO = GameObject.Find("Canvas").transform.Find("Panel/PlaneToggle").gameObject;
		UnityEngine.UI.Toggle toggle = (cubeGO == null) ? null : cubeGO.GetComponent<UnityEngine.UI.Toggle> ();
		if (toggle != null) {
			MeshRenderer mr = gameObject.GetComponent<MeshRenderer> ();
			if (mr != null) {
				mr.enabled = toggle.isOn;
			}
		}
	}
}
