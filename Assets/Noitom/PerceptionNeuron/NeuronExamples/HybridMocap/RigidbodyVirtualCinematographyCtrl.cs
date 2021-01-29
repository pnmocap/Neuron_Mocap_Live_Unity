using UnityEngine;
using System.Collections;

public class RigidbodyVirtualCinematographyCtrl : MonoBehaviour
{

    int switchCount = 0;
	// Update is called once per frame
	void Update ()
    {
	    if(Input.GetKeyDown(KeyCode.Tab))
        {
            switchCount++;
            if (switchCount >= transform.childCount)
                switchCount = 0;
            for (int i = 0; i < this.transform.childCount; i++)
            {
                transform.GetChild(i).gameObject.SetActive(switchCount == i);
            }
        }
	}
}
