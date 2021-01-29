using UnityEngine;
using System.Collections;
using Neuron;

public class NeuronRigidbodyListHelper : MonoBehaviour {

    public UnityEngine.UI.Text targetText = null;

	// Update is called once per frame
	void Update () {
        if(targetText != null)
        {
            string ids = "rigidbody ids in scene: ";
            int rigCount = NeuronActor.rigidbodyLocalPositions.Count;
            int count = 0;
            foreach (var item in NeuronActor.rigidbodyLocalPositions)
            {
                ids += item.Key.ToString() + (count == rigCount - 1 ? "" : ", ");
                count++;
            }
            targetText.text = ids;
        }
	
	}
}
