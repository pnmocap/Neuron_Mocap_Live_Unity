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

using System.Collections.Generic;
using UnityEngine;
using NeuronDataReaderManaged;

namespace Neuron
{
	public enum UpdateMethod 
	{
		Normal,
		Physical,
		EstimatedPhysical,
		MixedPhysical
	}

    // NeuronInstance 
    public class NeuronInstance : MonoBehaviour
    {
        public NeuronEnums.SkeletonType skeletonType = NeuronEnums.SkeletonType.PerceptionNeuronStudio;

        //[Space(5)]
        [Header("Index of avatar in axis software, default is zero")]
        public int actorID = 0;               

        protected NeuronActor boundActor = null;

        public bool noFrameData { get; private set; }

        public void Init(NeuronSource source, NeuronEnums.SkeletonType sklType)
        {
            if (source != null)
            {
                boundActor = source.AcquireActor(actorID);
                if (boundActor != null)
                {
                    skeletonType = sklType;
                    RegisterCallbacks();
                }
            }
        }

        public void Showdown()
        {
            UnregisterCallbacks();
            boundActor = null;
        }

		protected void RegisterCallbacks()
		{
            Debug.Log("RegisterCallbacks");
			if( boundActor != null )
			{
				boundActor.RegisterNoFrameDataCallback( OnNoFrameData );
				boundActor.RegisterResumeFrameDataCallback( OnResumeFrameData );
			}
		}

		protected void UnregisterCallbacks()
		{
			if( boundActor != null )
			{
				boundActor.UnregisterNoFrameDataCallback( OnNoFrameData );
				boundActor.UnregisterResumeFrameDataCallback( OnResumeFrameData );
                boundActor = null;
            }
		}

		protected void Update()
		{
            MocapApiManager.Update(Time.frameCount);

            if ( /*standalone &&*/ boundActor != null )
			{
				boundActor.owner.OnUpdate();
			}
		}


		public virtual bool OnNoFrameData()
		{
			noFrameData = true;
			return false;
		}

		public virtual bool OnResumeFrameData()
		{
			noFrameData = false;
			return false;
		}

		//public virtual bool OnReceivedBoneSizes()
		//{
		//	boneSizesDirty = applyBoneSizes;
		//	return false;
		//}

		public NeuronActor GetActor()
		{
			return boundActor;
		}

        //protected static float CalculateSwapRatio( int timeStamp, ref int last_evaluate_time )
        //{
        //	int now = NeuronActor.GetTimeStamp();
        //	float swap_ratio = (float)( timeStamp - last_evaluate_time ) / (float)( now - last_evaluate_time );
        //	last_evaluate_time = now;
        //	return Mathf.Clamp( swap_ratio, 0.0f, 1.0f );
        //}

        public void OnApplicationQuit()
        {
            MocapApiManager.OnDestroy();
        }
    }
}