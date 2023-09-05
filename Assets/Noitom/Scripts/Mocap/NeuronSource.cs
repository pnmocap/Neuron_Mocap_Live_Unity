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
using System.Linq;
using NeuronDataReaderManaged;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Neuron
{
    public class NeuronSource
    {
        public static int NoDataFrameTimeOut = 5000;
        public delegate void ResumeActorDelegate(NeuronActor actor);
        public delegate void SuspendActorDelegate(NeuronActor actor);

        List<ResumeActorDelegate> resumeActorCallbacks = new List<ResumeActorDelegate>();
        List<SuspendActorDelegate> suspendActorCallbacks = new List<SuspendActorDelegate>();
        Dictionary<int, NeuronActor> allActors = new Dictionary<int, NeuronActor>();// key：index
        int actorIndex = 0;

        object actorCreateDestroyLock = new object();

        public Guid guid = Guid.NewGuid();
        public ulong applicationHandle;

        public string address { get; private set; }
        public int port { get; private set; }

        public NeuronEnums.SocketType socketType { get; private set; }

        public int numOfAllActors { get { return allActors.Count; } }

        public int referenceCounter { get; private set; }
        public bool HasActorReference { get { return referenceCounter > 0; } }

        public NeuronSource(string address, int port, NeuronEnums.SocketType socketType)
        {
            this.address = address;
            this.port = port;
            this.socketType = socketType;
            this.referenceCounter = 0;
        }

        public void RegisterResumeActorCallback(ResumeActorDelegate callback)
        {
            if (callback != null)
            {
                resumeActorCallbacks.Add(callback);
            }
        }

        public void UnregisterResumeActorCallback(ResumeActorDelegate callback)
        {
            if (callback != null)
            {
                resumeActorCallbacks.Remove(callback);
            }
        }

        public void RegisterSuspendActorCallback(SuspendActorDelegate callback)
        {
            if (callback != null)
            {
                suspendActorCallbacks.Add(callback);
            }
        }

        public void UnregisterSuspendActorCallback(SuspendActorDelegate callback)
        {
            if (callback != null)
            {
                suspendActorCallbacks.Remove(callback);
            }
        }

        public void Grab()
        {
            ++referenceCounter;
        }

        public void Release()
        {
            --referenceCounter;
        }



        public void OnUpdate()
        {

        }


        //public virtual void OnSocketStatusChanged( SocketStatus status, string msg )
        //{
        //}

        /// <summary> 
        /// AcquireActor by actorID
        /// </summary>
        /// <param name="actorID">actorID</param>
        /// <param name="isCreat">only creat can use it</param>
        /// <returns></returns>
        public NeuronActor AcquireActor(int actorID, bool isCreat = false)
        {
            lock (actorCreateDestroyLock)
            {
                NeuronActor actor = FindActorById(actorID);
                if (actor != null)
                {
                    return actor;
                }
                if (isCreat)
                {
                    actor = CreateActor(actorID);
                }
                return actor;
            }
        }
        /// <summary>
        /// AcquireActor by actorName
        /// </summary>
        /// <param name="actorName">actorName</param>
        /// <param name="isCreat">only creat can use it</param>
        /// <returns></returns>
        public NeuronActor AcquireActor(string actorName, bool isCreat = false)
        {
            lock (actorCreateDestroyLock)
            {
                NeuronActor actor = FindTrackingMotionByAvatarName(actorName);
                if (actor != null)
                {
                    return actor;
                }
                if (isCreat)
                {
                    actor = CreateActor(allActors.Count, actorName);
                }
                return actor;
            }
        }
        public NeuronActor[] GetActors()
        {
            NeuronActor[] actors = new NeuronActor[allActors.Count];
            allActors.Values.CopyTo(actors, 0);
            return actors;
        }

        NeuronActor CreateActor(int actorID, string actorName = null)
        {
            NeuronActor find = FindActorById(actorID);
            if (find == null)
            {
                if (!string.IsNullOrEmpty(actorName))
                {
                    find = FindTrackingMotionByAvatarName(actorName);
                }

                if (find == null)
                {
                    NeuronActor actor = new NeuronActor(this, actorID);
                    if (!string.IsNullOrEmpty(actorName))
                    {
                        actor.SetAvatarName(actorName);
                    }
                    allActors.Add(actorIndex, actor);
                    actorIndex++;
                    return actor;
                }
                else
                {
                    return find;
                }
            }
            return find;
        }

        void DestroyActor(int actorID)
        {
            lock (actorCreateDestroyLock)
            {
                for (int i = 0; i < allActors.Count; i++)
                {
                    KeyValuePair<int, NeuronActor> kv = allActors.ElementAt(i);
                    if (kv.Value.AvatarIndex == actorID)
                    {
                        allActors.Remove(kv.Key);
                    }
                }
            }
        }

        //void SuspendActor( NeuronActor actor )
        //{
        //          actor.IsActive = false;
        //          Debug.Log( string.Format( "[NeuronSource] Suspend actor {0}", actor.guid.ToString( "N" ) ) );
        //}

        //void ResumeActor( NeuronActor actor )
        //{
        //          actor.IsActive = true;
        //          Debug.Log( string.Format( "[NeuronSource] Resume actor {0}", actor.guid.ToString( "N" ) ) );
        //}

        //void SuspendAllActors()
        //{
        //	foreach( KeyValuePair<int, NeuronActor> iter in allActors )
        //	{
        //              iter.Value.IsActive = false;
        //          }
        //}

        NeuronActor FindActorById(int actorID)
        {
            foreach (var act in allActors)
            {
                if (act.Value.AvatarIndex == actorID)
                    return act.Value;
            }
            return null;
        }


        public NeuronActor FindTrackingMotionByAvatarName(string avatarName)
        {
            foreach (var act in allActors)
            {
                if (act.Value.AvatarName == avatarName)
                    return act.Value;
            }
            return null;
        }
    }
}