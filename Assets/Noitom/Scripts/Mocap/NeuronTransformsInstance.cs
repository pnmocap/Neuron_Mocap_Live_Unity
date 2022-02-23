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
	public class NeuronTransformsInstance : NeuronInstance
	{
        [Space(10)]
        public bool enableHipMove = true;
        public bool enableFingerMove = true;
        public Transform					root = null;
        //
        // Obsolete don't use it
        [HideInInspector]
		public string						prefix = "Robot_";
		public bool							boundTransforms { get ; private set; }
		public UpdateMethod					motionUpdateMethod = UpdateMethod.Normal;

        [HideInInspector]
        public Transform[]					transforms = new Transform[(int)NeuronBones.NumOfBones];

        [Header("use an already existing NeuronTransformsInstance as the physical reference source")]
        [HideInInspector]
        public Transform					physicalReferenceOverride; //use an already existing NeuronAnimatorInstance as the physical reference

        public NeuronTransformsPhysicalReference	physicalReference = new NeuronTransformsPhysicalReference();
		Vector3[]							bonePositionOffsets = new Vector3[(int)NeuronBones.NumOfBones];
		Vector3[]							boneRotationOffsets = new Vector3[(int)NeuronBones.NumOfBones];

        Quaternion[] orignalRot = new Quaternion[(int)NeuronBones.NumOfBones];
        Quaternion[] orignalParentRot = new Quaternion[(int)NeuronBones.NumOfBones];
        Vector3[] orignalPositions = new Vector3[(int)NeuronBones.NumOfBones];

        [HideInInspector]
        public bool[] disableBoneMovement = new bool[(int)NeuronBones.NumOfBones];


        bool inited = false;
		new void OnEnable()
		{
            if(inited)
            {
                return;
            }
            inited = true;

            //base.OnEnable();
			
			if( root == null )
			{
				root = transform;
			}
			
			Bind( root, prefix );
		}
		
		new void Update()
		{
			//base.ToggleConnect();
			base.Update();
			
			if( boundActor != null && boundTransforms && motionUpdateMethod == UpdateMethod.Normal )
			{				
				if( physicalReference.Initiated() )
				{
					ReleasePhysicalContext();
				}
				
				ApplyMotion( boundActor, transforms, bonePositionOffsets, boneRotationOffsets , enableHipMove, orignalRot, orignalParentRot, orignalPositions);
			}

		}
		
		void FixedUpdate()
		{
			//base.ToggleConnect();
			
			if(boundTransforms && motionUpdateMethod != UpdateMethod.Normal )
			{				
				if( !physicalReference.Initiated() )
				{
					InitPhysicalContext();
				}

				ApplyMotionPhysically( physicalReference.GetReferenceTransforms(), transforms );
			}
		}
		
		public Transform[] GetTransforms()
		{
			return transforms;
		}
		
		static bool ValidateVector3( Vector3 vec )
		{
			return !float.IsNaN( vec.x ) && !float.IsNaN( vec.y ) && !float.IsNaN( vec.z )
				&& !float.IsInfinity( vec.x ) && !float.IsInfinity( vec.y ) && !float.IsInfinity( vec.z );
		}
		
		// set position for bone
		static void SetPosition( Transform[] transforms, NeuronBones bone, Vector3 pos, bool withOriginlPosition = false )
		{
			Transform t = transforms[(int)bone];
			if( t != null )
			{
                if (!withOriginlPosition)
                {
                    // calculate position when we have scale
                    Vector3 lossyScale = t.parent == null ? Vector3.one : t.parent.lossyScale;

                    pos.Scale(new Vector3(1.0f / lossyScale.x, 1.0f / lossyScale.y, 1.0f / lossyScale.z));
                }

				if( !float.IsNaN( pos.x ) && !float.IsNaN( pos.y ) && !float.IsNaN( pos.z ) )
				{
					t.localPosition = pos;
				}
			}
		}
		
		// set rotation for bone
		static void SetRotation( Transform[] transforms, NeuronBones bone, Vector3 rotation )
		{
			Transform t = transforms[(int)bone];
			if( t != null )
			{
				Quaternion rot = Quaternion.Euler( rotation );
				if( !float.IsNaN( rot.x ) && !float.IsNaN( rot.y ) && !float.IsNaN( rot.z ) && !float.IsNaN( rot.w ) )
				{
					t.localRotation = rot;
				}
			}
		}

		// apply transforms extracted from actor mocap data to bones
		public void ApplyMotion( NeuronActor actor, Transform[] transforms, Vector3[] bonePositionOffsets, Vector3[] boneRotationOffsets , bool enableHipMove,  Quaternion[] orignalRot , Quaternion[] orignalParentRot , Vector3[] orignalPositions)
		{
            if (!actor.HsReceivedData)
                return;
            // apply Hips position
            if (enableHipMove &&  (!disableBoneMovement[(int)NeuronBones.Hips]))
                SetPosition(transforms, NeuronBones.Hips, actor.GetReceivedPosition(NeuronBones.Hips, orignalPositions[(int)NeuronBones.Hips]));
            else
            {
                Vector3 p = actor.GetReceivedPosition(NeuronBones.Hips);
                SetPosition(transforms, NeuronBones.Hips, new Vector3(0f, p.y, 0f));
            }

            SetRotation(transforms, NeuronBones.Hips,
                (Quaternion.Euler(actor.GetReceivedRotation(NeuronBones.Hips)) * orignalRot[(int)NeuronBones.Hips]).eulerAngles);


            // apply positions
            //if (actor.AvatarWithDisplacement )
			{
				for( int i = 1; i < (int)NeuronBones.NumOfBones && i < transforms.Length; ++i )
				{
                    if (transforms[i] == null)
                        continue;

                    // q
                    Quaternion orignalBoneRot = Quaternion.identity;
                    if (orignalRot != null)
                    {
                        orignalBoneRot = orignalRot[i];
                    }
                    Vector3 rot = actor.GetReceivedRotation((NeuronBones)i) + boneRotationOffsets[i] ;
                    //Debug.LogError(actor.AvatarIndex + " " +  actor.GetReceivedRotation((NeuronBones)i) + ", " + rot);
                    Quaternion srcQ = Quaternion.Euler(rot);

                    Quaternion usedQ = Quaternion.Inverse(orignalParentRot[i]) * srcQ * orignalParentRot[i];
                    Vector3 transedRot = usedQ.eulerAngles;
                    Quaternion finalBoneQ = Quaternion.Euler(transedRot) * orignalBoneRot;
                    SetRotation(transforms, (NeuronBones)i, finalBoneQ.eulerAngles);

                    // p   
                    bool enableNodeMove = actor.GetHasPosition((NeuronBones)i);
                    enableNodeMove &= (!disableBoneMovement[i]);
                    if (!enableFingerMove)
                    {
                        if (i >= (int)NeuronBones.RightHand && i <= (int)NeuronBones.RightHandPinky3)
                            enableNodeMove = false;
                        if (i >= (int)NeuronBones.LeftHand && i <= (int)NeuronBones.LeftHandPinky3)
                            enableNodeMove = false;
                    }
                    if (enableNodeMove)
                    {
                        Vector3 srcP = actor.GetReceivedPosition((NeuronBones)i) + bonePositionOffsets[i];
                        Vector3 finalP = Quaternion.Inverse(orignalParentRot[i]) * srcP;
                        SetPosition(transforms, (NeuronBones)i, finalP);
                    }
                    else
                    {
                        SetPosition(transforms, (NeuronBones)i, orignalPositions[i], true);
                    }
                    //  SetPosition( transforms, (NeuronBones)i, actor.GetReceivedPosition( (NeuronBones)i ) + bonePositionOffsets[i] );
                    //	SetRotation( transforms, (NeuronBones)i, actor.GetReceivedRotation( (NeuronBones)i ) + boneRotationOffsets[i] );
                }
            }
			
		}
		
		// apply Transforms of src bones to dest Rigidbody Components of bone
		public void ApplyMotionPhysically( Transform[] src, Transform[] dest )
		{
			if( src != null && dest != null )
			{
				for( int i = 0; i < (int)NeuronBones.NumOfBones; ++i )
				{
					Transform src_transform = src[i];
					Transform dest_transform = dest[i];
					if( src_transform != null && dest_transform != null )
					{
						Rigidbody rigidbody = dest_transform.GetComponent<Rigidbody>();
						if( rigidbody != null )
						{
							switch (motionUpdateMethod) {
							case UpdateMethod.Physical:
								rigidbody.MovePosition( src_transform.position );
								rigidbody.MoveRotation( src_transform.rotation );
								break;

							case UpdateMethod.EstimatedPhysical:
								Quaternion dAng = src_transform.rotation * Quaternion.Inverse (dest_transform.rotation);
								float angle = 0.0f;
								Vector3 axis = Vector3.zero;
								dAng.ToAngleAxis (out angle, out axis);

                                if (angle > 180f)
                                    angle -= 360f;

                                // Here I drop down to 0.9f times the desired movement,
                                // since we'd rather undershoot and ease into the correct angle
                                // than overshoot and oscillate around it in the event of errors.
                                Vector3 angular = (0.9f * Mathf.Deg2Rad * angle / Time.fixedDeltaTime) * axis.normalized;


                                Vector3 velocityTarget = (src_transform.position - dest_transform.position) / Time.fixedDeltaTime;
                                Vector3 angularTarget = angular;

                                ApplyVelocity(rigidbody, velocityTarget, angularTarget);

								break;

							case UpdateMethod.MixedPhysical:
								Vector3 velocityTarget2 = (src_transform.position - dest_transform.position) / Time.fixedDeltaTime;

								Vector3 v = Vector3.MoveTowards(rigidbody.velocity, velocityTarget2, 10.0f);
								if( ValidateVector3( v ) )
								{
									rigidbody.velocity = v;
								}

								rigidbody.MoveRotation( src_transform.rotation );

								break;
							}
						}
					}
				}
			}
		}


		void ApplyVelocity(Rigidbody rb, Vector3 velocityTarget, Vector3 angularTarget)
		{
            Vector3 v =  Vector3.MoveTowards(rb.velocity, velocityTarget, 100.0f);
			if( ValidateVector3( v ) )
			{

                rb.velocity = v;
			}

            v =  Vector3.MoveTowards(rb.angularVelocity, angularTarget, 100.0f);
			if( ValidateVector3( v ) )
			{

                rb.angularVelocity = v;

			}
		}

		
		public bool Bind( Transform root, string prefix )
		{
			this.root = root;
			this.prefix = prefix;
			//int bound_count = 
            NeuronHelper.Bind( root, transforms, prefix, false, (skeletonType == NeuronEnums.SkeletonType.PerceptionNeuronStudio) ? NeuronBoneVersion.V2 : NeuronBoneVersion.V1);
            boundTransforms = true; // bound_count >= (int)NeuronBones.NumOfBones;
			UpdateOffset();
            CaluateOrignalRot();
            return boundTransforms;
		}
		
		void InitPhysicalContext()
		{
			if( physicalReference.Init( root, prefix, transforms, physicalReferenceOverride ) )
			{
				// break original object's hierachy of transforms, so we can use MovePosition() and MoveRotation() to set transform
				NeuronHelper.BreakHierarchy( transforms );
			}

			CheckRigidbodySettings ();
		}

		
		void ReleasePhysicalContext()
		{
			physicalReference.Release();
		}
		
		void UpdateOffset()
		{
			// initiate values
			for( int i = 0; i < (int)HumanBodyBones.LastBone; ++i )
			{
				bonePositionOffsets[i] = Vector3.zero;
				boneRotationOffsets[i] = Vector3.zero;
			}
			/*
			if( boundTransforms )
			{
                if(transforms[(int)NeuronBones.LeftUpLeg] != null)
				    bonePositionOffsets[(int)NeuronBones.LeftUpLeg] = new Vector3( 0.0f, transforms[(int)NeuronBones.LeftUpLeg].localPosition.y, 0.0f );
                if(transforms[(int)NeuronBones.RightUpLeg] != null)
				    bonePositionOffsets[(int)NeuronBones.RightUpLeg] = new Vector3( 0.0f, transforms[(int)NeuronBones.RightUpLeg].localPosition.y, 0.0f );
			}
			*/
		}

        void CaluateOrignalRot()
        {
            for (int i = 0; i < orignalPositions.Length; i++)
            {
                orignalPositions[i] = transforms[i] == null ? Vector3.zero : transforms[i].localPosition;
            }
            for (int i = 0; i < orignalRot.Length; i++)
            {
                orignalRot[i] = transforms[i] == null ? Quaternion.identity : transforms[i].localRotation;
            }
            for (int i = 0; i < orignalRot.Length; i++)
            {
                Quaternion parentQs = Quaternion.identity;
                if (transforms[i] == null)
                {
                    orignalParentRot[i] = Quaternion.identity;
                    continue;
                }
                Transform tempParent = transforms[i].transform.parent;
                while (tempParent != null)
                {
                    parentQs = tempParent.transform.localRotation * parentQs;
                    tempParent = tempParent.parent;
                    if (tempParent == null || tempParent == this.transform || (transforms[0] != null && tempParent == transforms[0].transform.parent))
                        break;
                }
                orignalParentRot[i] = parentQs;
            }
        }
		void CheckRigidbodySettings( ){
			//check if rigidbodies have correct settings
			bool kinematicSetting = false;
			if (motionUpdateMethod == UpdateMethod.Physical) {
				kinematicSetting = true;
			}

			for( int i = 0; i < (int)NeuronBones.NumOfBones && i < transforms.Length; ++i )
			{
				Rigidbody r = transforms[i].GetComponent<Rigidbody> ();
				if (r != null) {
					r.isKinematic = kinematicSetting;
				}
			}
		}
	}
}