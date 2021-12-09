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
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using NeuronDataReaderManaged;
using Neuron;

namespace Neuron
{
	// cache motion data and parse to animator
	public class NeuronActor
	{
		public delegate bool 							NoFrameDataDelegate();
		public delegate bool							ResumeFrameDataDelegate();
		

		List<NoFrameDataDelegate>						noFrameDataCallbacks = new List<NoFrameDataDelegate>();
		List<ResumeFrameDataDelegate>					resumeFrameDataCallbacks = new List<ResumeFrameDataDelegate>();
		
		public Guid										guid = Guid.NewGuid();
		public NeuronSource								owner = null;

        private string avatarName;
        private int avatarIndex;

        public string AvatarName
        {
            get
            {
                return avatarName;
            }
        }
        public int AvatarIndex
        {
            get
            {
                return avatarIndex;
            }

        }

        // source data buffer
        bool[] sourceHasLocalPositions = new bool[(int)NeuronBones.NumOfBones];
        Vector3[] sourceLocalPositions = new Vector3[(int)NeuronBones.NumOfBones];
        Quaternion[] sourceLocalRotations = new Quaternion[(int)NeuronBones.NumOfBones];

        public static Dictionary<int, Vector3> rigidbodyLocalPositions = new Dictionary<int, Vector3>();
        public static Dictionary<int, Quaternion> rigidbodyLocalRotations = new Dictionary<int, Quaternion>();

        public static Dictionary<string, Vector3> trackerLocalPositions = new Dictionary<string, Vector3>();
        public static Dictionary<string, Quaternion> trackerLocalRotations = new Dictionary<string, Quaternion>();


        public Vector3[] SourceLocalPositions
        {
            get
            {
                return sourceLocalPositions;
            }
        }

        public Quaternion[] SourceLocalRotations
        {
            get
            {
                return sourceLocalRotations;
            }
        }
        public bool[] SourceHasLocalPositions
        {
            get
            {
                return sourceHasLocalPositions;
            }
        }

        public bool HsReceivedData = false;

		public void RegisterNoFrameDataCallback( NoFrameDataDelegate callback )
		{
			if( callback != null )
			{
				noFrameDataCallbacks.Add( callback );
			}
		}
		
		public void UnregisterNoFrameDataCallback( NoFrameDataDelegate callback )
		{
			if( callback != null )
			{
				noFrameDataCallbacks.Remove( callback );
			}
		}
		
		public void RegisterResumeFrameDataCallback( ResumeFrameDataDelegate callback )
		{
			if( callback != null )
			{
				resumeFrameDataCallbacks.Add( callback );
			}
		}
		
		public void UnregisterResumeFrameDataCallback( ResumeFrameDataDelegate callback )
		{
			if( callback != null )
			{
				resumeFrameDataCallbacks.Remove( callback );
			}
		}
		
		public NeuronActor( NeuronSource owner, int avatarId )
		{
			this.owner = owner;
			this.avatarIndex = avatarId;
			
			if( owner != null )
			{
				owner.RegisterResumeActorCallback( OnResumeFrameData );
				owner.RegisterSuspendActorCallback( OnNoFrameData );
			}
		}
		
		~NeuronActor()
		{
			if( owner != null )
			{
				owner.UnregisterResumeActorCallback( OnResumeFrameData );
				owner.UnregisterSuspendActorCallback( OnNoFrameData );
			}
		}
			
		public virtual void OnNoFrameData( NeuronActor actor )
		{
			for( int i = 0; i < noFrameDataCallbacks.Count; ++i )
			{
				noFrameDataCallbacks[i]();
			}
		}		
		
		public virtual void OnResumeFrameData( NeuronActor actor  )
		{
			for( int i = 0; i < resumeFrameDataCallbacks.Count; ++i )
			{
				resumeFrameDataCallbacks[i]();
			}
		}
		

		
		public Vector3 GetReceivedPosition( NeuronBones bone)
		{
            return sourceLocalPositions[(int)bone];

        }
        public Vector3 GetReceivedPosition(NeuronBones bone, Vector3 defaultValue)
        {
            return sourceHasLocalPositions[(int)bone] ? sourceLocalPositions[(int)bone] : defaultValue;

        }

        public bool GetHasPosition(NeuronBones i)
        {
            return sourceHasLocalPositions[(int)i];
        }

        public Vector3 GetReceivedRotation( NeuronBones bone )
		{
            return sourceLocalRotations[(int)bone].eulerAngles;
        }

        public void SetAvatarName(string name)
        {
            this.avatarName = name;
        }
    }
}