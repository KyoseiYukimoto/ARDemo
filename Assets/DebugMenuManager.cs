using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugMenuManager : MonoBehaviour {

	public GameObject menuRoot;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
	}

	public void ChangeActive()
	{
		menuRoot.SetActive (!menuRoot.activeInHierarchy);
	}
}
