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
		[Header("Connection settings:")]
        [HideInInspector]
		public string						address = "127.0.0.1";
        [HideInInspector]
        public int							portTcp = 7003;
        [HideInInspector]
        public int                          portUdp = 7004;
        public NeuronEnums.SocketType	socketType = NeuronEnums.SocketType.TCP;
        //[Space(5)]
        [Header("Index of avatar in axis software, default is zero")]
        public int							actorID = 0;

        //[Space(10)]
        [Header("Whether to connect to axis software")]
        public bool							connectToAxis = true;

        public NeuronEnums.SkeletonType skeletonType = NeuronEnums.SkeletonType.PerceptionNeuronStudio;
        protected bool                      hasConnected;

		protected NeuronActor				boundActor = null;
		//protected bool						standalone = true;
		//protected int						lastEvaluateTime = 0;
		//protected bool						boneSizesDirty = false;
		//protected bool						applyBoneSizes = false;

		public bool							noFrameData { get ; private set; }

        protected NeuronSource source;

  //      public NeuronInstance()
		//{
		//}

		//public NeuronInstance( string address, int port, int commandServerPort, NeuronEnums.SocketType socketType, int actorID )
		//{
		//	//standalone = true;
		//}

		//public NeuronInstance( NeuronActor boundActor )
		//{
		//	if( boundActor != null )
		//	{
		//		this.boundActor = boundActor;
		//		RegisterCallbacks();
		//		//standalone = false;
		//	}
		//}

		//public void SetBoundActor( NeuronActor actor )
		//{
		//	if( boundActor != null )
		//	{
		//		UnregisterCallbacks();
		//	}

		//	if( actor != null )
		//	{
		//		boundActor = actor;
		//		RegisterCallbacks();
		//		actorID = actor.actorID;

		//		NeuronSource source = actor.owner;
		//		address = source.address;
		//		port = source.port;
		//		commandServerPort = source.commandServerPort;
		//		socketType = source.socketType;

		//		standalone = false;
		//	}
		//}

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
			}
		}

		protected void OnEnable()
		{
			ToggleConnect();
		}

		protected void OnDisable()
		{
			if( boundActor != null)// && standalone )
			{
				Disconnect();
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

		protected void ToggleConnect()
		{
            //if (standalone)
            {
                if (connectToAxis && boundActor == null)
                {
                    hasConnected = Connect();
                }
                else if (!connectToAxis && boundActor != null)
                {
                    Disconnect();
                }
            }
		}

        int GetPortByConnectionType()
        {
            return socketType == NeuronEnums.SocketType.TCP ? portTcp : portUdp;
        }

		protected bool Connect()
		{
            //source = NeuronConnection.Connect( address, port, commandServerPort, socketType );
            source = MocapApiManager.RequareConnection(address, GetPortByConnectionType(), socketType, skeletonType);

            if ( source != null )
			{
				boundActor = source.AcquireActor( actorID );
				RegisterCallbacks();
			}

			return source != null;
		}

		protected void Disconnect()
		{
            //NeuronConnection.Disconnect( boundActor.owner );
            MocapApiManager.Disconnect(boundActor.owner);
            UnregisterCallbacks();
			boundActor = null;
            source = null;
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