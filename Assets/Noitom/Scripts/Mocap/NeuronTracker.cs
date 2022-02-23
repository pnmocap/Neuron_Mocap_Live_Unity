using System;
using UnityEngine;
using NeuronDataReaderManaged;

namespace Neuron
{
    public class NeuronTracker : NeuronInstance
    {
       
        public string deviceName;
        bool inited = false;
        new void OnEnable()
        {
            if (inited)
            {
                return;
            }
            inited = true;

            //base.OnEnable();

        }

        new void Update()
        {
            //base.ToggleConnect();
            base.Update();

            //if( boundActor != null)
            {
                ApplyMotion(this.transform, deviceName);
            }

        }


        // apply transforms extracted from actor mocap data to bones
        public static void ApplyMotion(Transform trans, string deviceName)
        {
            if (NeuronActor.trackerLocalPositions.ContainsKey(deviceName))
            {
                trans.localPosition = NeuronActor.trackerLocalPositions[deviceName];
            }


            if (NeuronActor.trackerLocalRotations.ContainsKey(deviceName))
            {
                trans.localRotation = NeuronActor.trackerLocalRotations[deviceName];
            }


        }

    }
}