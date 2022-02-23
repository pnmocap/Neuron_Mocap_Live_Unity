/************************************************************************************
 Copyright: Copyright 2021 Beijing Noitom Technology Ltd. All Rights reserved.
 Pending Patents: PCT/CN2014/085659 PCT/CN2014/071006

 Licensed under the Perception Neuron SDK License Beta Version (the “License");
 You may only use the Perception Neuron SDK when in compliance with the License,
 which is provided at the time of installation or download, or which
 otherwise accompanies this software in the form of either an electronic or a hard copy.

 A copy of the License is included with this package or can be obtained at:
 http://www.neuronmocap.com

 Unless required by applicable law or agreed to in writing, the Perception Neuron SDK
 distributed under the License is provided on an "AS IS" BASIS,
 WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 See the License for the specific language governing conditions and
 limitations under the License.
************************************************************************************/

using System;
using UnityEngine;
using NeuronDataReaderManaged;

namespace Neuron
{
	public class NeuronRigidbody : NeuronInstance
	{
        [Header("Normally, rigidbodyId is 131---134, 231---234, 331---334, 431---434 or 531---534")]
        public int rigidbodyId;
        bool inited = false;
		new void OnEnable()
		{
            if(inited)
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
				ApplyMotion(this.transform, rigidbodyId);
			}

		}
	

		// apply transforms extracted from actor mocap data to bones
		public static void ApplyMotion(Transform trans, int rigidbodyId)
		{
            if (NeuronActor.rigidbodyLocalPositions.ContainsKey(rigidbodyId))
            {
                trans.localPosition = NeuronActor.rigidbodyLocalPositions[rigidbodyId];
            }


            if (NeuronActor.rigidbodyLocalRotations.ContainsKey(rigidbodyId))
            {
                trans.localRotation = NeuronActor.rigidbodyLocalRotations[rigidbodyId];
            }


        }

	}
}