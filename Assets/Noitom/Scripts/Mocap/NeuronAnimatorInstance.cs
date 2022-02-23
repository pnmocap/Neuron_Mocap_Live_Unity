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
using UnityEngine;
using Neuron;

public class NeuronAnimatorInstance : NeuronInstance
{

    [Header("Animator to motion, null means motion self animator")]
    public Animator							boundAnimator = null;

	public bool enableHipMove = true;
    public bool enableFingerMove = true;

    [Header("use an already existing NeuronAnimatorInstance as the physical reference source")]
    [HideInInspector]
    public Animator 						physicalReferenceOverride; //use an already existing NeuronAnimatorInstance as the physical reference
    public UpdateMethod motionUpdateMethod = UpdateMethod.Normal;

    public NeuronAnimatorPhysicalReference 	physicalReference = new NeuronAnimatorPhysicalReference();
    Vector3[]								bonePositionOffsets = new Vector3[(int)HumanBodyBones.LastBone];
	Vector3[]								boneRotationOffsets = new Vector3[(int)HumanBodyBones.LastBone];

    Quaternion[] orignalRot = new Quaternion[(int)HumanBodyBones.LastBone];
    Quaternion[] orignalParentRot = new Quaternion[(int)HumanBodyBones.LastBone];
    Vector3[] orignalPositions = new Vector3[(int)HumanBodyBones.LastBone];


    [HideInInspector]
    public bool[] disableBoneMovement = new bool[(int)HumanBodyBones.LastBone];

    bool inited = false;
    new void OnEnable()
	{
        if (inited)
        {
            return;
        }
        inited = true;
        //base.OnEnable();
		if( boundAnimator == null )
		{
			boundAnimator = GetComponent<Animator>();
		}
        UpdateOffset();
        CaluateOrignalRot();
    }
	
	new void Update()
	{	
		//base.ToggleConnect();
		base.Update();
		
		if( boundActor != null && boundAnimator != null && motionUpdateMethod == UpdateMethod.Normal) // !physicalUpdate )
		{			
			if( physicalReference.Initiated() )
			{
				ReleasePhysicalContext();
			}
			
			ApplyMotion( boundActor, boundAnimator, bonePositionOffsets, boneRotationOffsets);
        }
	}

	bool ValidateVector3( Vector3 vec )
	{
		return !float.IsNaN( vec.x ) && !float.IsNaN( vec.y ) && !float.IsNaN( vec.z )
			&& !float.IsInfinity( vec.x ) && !float.IsInfinity( vec.y ) && !float.IsInfinity( vec.z );
	}
	
	void SetScale( Animator animator, HumanBodyBones bone, float size, float referenceSize )
	{	
		Transform t = animator.GetBoneTransform( bone );
		if( t != null && bone <= HumanBodyBones.Jaw )
		{
			float ratio = size / referenceSize;
			
			Vector3 newScale = new Vector3( ratio, ratio, ratio );
			newScale.Scale( new Vector3( 1.0f / t.parent.lossyScale.x, 1.0f / t.parent.lossyScale.y, 1.0f / t.parent.lossyScale.z ) );
			
			if( ValidateVector3( newScale ) )
			{
				t.localScale = newScale;
			}
		}
	}
	
	// set position for bone in animator
	void SetPosition(bool hasPosData, Animator animator, HumanBodyBones bone, Vector3 pos)
	{
        if(this.disableBoneMovement[(int)bone])
            pos = this.orignalPositions[(int)bone];
		Transform t = animator.GetBoneTransform( bone );
		if( t != null )
		{
			if( !float.IsNaN( pos.x ) && !float.IsNaN( pos.y ) && !float.IsNaN( pos.z ) )
			{
                Vector3 srcP = pos;
                if (!hasPosData)
                {
                    t.localPosition = srcP;
                }
                else
                {
                    Vector3 finalP = Quaternion.Inverse(orignalParentRot[(int)bone]) * srcP;
                    t.localPosition = finalP;
                }
            }
		}
	}
	
	// set rotation for bone in animator
	void SetRotation( Animator animator, HumanBodyBones bone, Vector3 rotation )
	{
		Transform t = animator.GetBoneTransform( bone );
		if( t != null )
		{
			Quaternion rot = Quaternion.Euler( rotation );
			if( !float.IsNaN( rot.x ) && !float.IsNaN( rot.y ) && !float.IsNaN( rot.z ) && !float.IsNaN( rot.w ) )
			{
                //t.localRotation = rot;

                Quaternion orignalBoneRot = Quaternion.identity;
                if (orignalRot != null)
                {
                    orignalBoneRot = orignalRot[(int)bone];
                }
                Quaternion srcQ = rot;

                Quaternion usedQ = Quaternion.Inverse(orignalParentRot[(int)bone]) * srcQ * orignalParentRot[(int)bone];
                Vector3 transedRot = usedQ.eulerAngles;
                Quaternion finalBoneQ = Quaternion.Euler(transedRot) * orignalBoneRot;
                t.localRotation = finalBoneQ;
            }
		}
	}
	
	// apply transforms extracted from actor mocap data to transforms of animator bones
	public void ApplyMotion( NeuronActor actor, Animator animator, Vector3[] positionOffsets, Vector3[] rotationOffsets)
    {
        if (!actor.HsReceivedData)
            return;
        // apply Hips position
        if (enableHipMove) {
			SetPosition (actor.GetHasPosition(NeuronBones.Hips), animator, HumanBodyBones.Hips, actor.GetReceivedPosition (NeuronBones.Hips, this.orignalPositions[(int)HumanBodyBones.Hips]) + positionOffsets [(int)HumanBodyBones.Hips]);
		}
        SetRotation(animator, HumanBodyBones.Hips, actor.GetReceivedRotation(NeuronBones.Hips));

        // apply positions
        //if( actor.AvatarWithDisplacement )
        {
			// legs
			SetPosition(actor.GetHasPosition(NeuronBones.RightUpLeg), animator, HumanBodyBones.RightUpperLeg, actor.GetReceivedPosition(NeuronBones.RightUpLeg, this.orignalPositions[(int)HumanBodyBones.RightUpperLeg]) + positionOffsets[(int)HumanBodyBones.RightUpperLeg]);
            SetPosition(actor.GetHasPosition(NeuronBones.RightLeg  ), animator, HumanBodyBones.RightLowerLeg, actor.GetReceivedPosition(NeuronBones.RightLeg, this.orignalPositions[(int)HumanBodyBones.RightLowerLeg]));
            SetPosition(actor.GetHasPosition(NeuronBones.RightFoot ), animator, HumanBodyBones.RightFoot, actor.GetReceivedPosition(NeuronBones.RightFoot, this.orignalPositions[(int)HumanBodyBones.RightFoot]));
            SetPosition(actor.GetHasPosition(NeuronBones.LeftUpLeg ), animator, HumanBodyBones.LeftUpperLeg, actor.GetReceivedPosition(NeuronBones.LeftUpLeg, this.orignalPositions[(int)HumanBodyBones.LeftUpperLeg]) + positionOffsets[(int)HumanBodyBones.LeftUpperLeg]);
            SetPosition(actor.GetHasPosition(NeuronBones.LeftLeg), animator, HumanBodyBones.LeftLowerLeg, actor.GetReceivedPosition(NeuronBones.LeftLeg, this.orignalPositions[(int)HumanBodyBones.LeftLowerLeg]));
            SetPosition(actor.GetHasPosition(NeuronBones.LeftFoot  ),animator, HumanBodyBones.LeftFoot     ,			  actor.GetReceivedPosition( NeuronBones.LeftFoot, this.orignalPositions[(int)HumanBodyBones.LeftFoot     ]) );
			
			// spine
			SetPosition(actor.GetHasPosition(NeuronBones.Spine), animator, HumanBodyBones.Spine,					actor.GetReceivedPosition( NeuronBones.Spine, this.orignalPositions[(int)HumanBodyBones.Spine]) );
            if ((skeletonType == NeuronEnums.SkeletonType.PerceptionNeuronStudio))
            {
                SetPosition(actor.GetHasPosition(NeuronBones.Spine1), animator, HumanBodyBones.Chest,
                     actor.GetReceivedPosition(NeuronBones.Spine1, this.orignalPositions[(int)HumanBodyBones.Chest])
                     );
#if UNITY_2018_2_OR_NEWER
                SetPosition(actor.GetHasPosition(NeuronBones.Spine2), animator, HumanBodyBones.UpperChest,
                     actor.GetReceivedPosition(NeuronBones.Spine2)
                     );
#endif
                SetPosition(actor.GetHasPosition(NeuronBones.Neck), animator, HumanBodyBones.Neck,
                     actor.GetReceivedPosition((NeuronBones)NeuronBonesV2.Neck, this.orignalPositions[(int)HumanBodyBones.Neck]) +
                     (EulerToQuaternion(actor.GetReceivedRotation((NeuronBones)NeuronBonesV2.Neck)) * actor.GetReceivedPosition((NeuronBones)NeuronBonesV2.Neck1))
                    );
            }
            else
            {
                SetPosition(actor.GetHasPosition(NeuronBones.Spine3), animator, HumanBodyBones.Chest, actor.GetReceivedPosition(NeuronBones.Spine3, this.orignalPositions[(int)HumanBodyBones.Chest]));
                SetPosition(actor.GetHasPosition(NeuronBones.Neck), animator, HumanBodyBones.Neck, actor.GetReceivedPosition(NeuronBones.Neck, this.orignalPositions[(int)HumanBodyBones.Neck]));
            }
			SetPosition(actor.GetHasPosition(NeuronBones.Head), animator, HumanBodyBones.Head,						actor.GetReceivedPosition( NeuronBones.Head, this.orignalPositions[(int)HumanBodyBones.Head]) );
			
			// right arm
			SetPosition(actor.GetHasPosition(NeuronBones.RightShoulder), animator, HumanBodyBones.RightShoulder, actor.GetReceivedPosition(NeuronBones.RightShoulder, this.orignalPositions[(int)HumanBodyBones.RightShoulder]));
            SetPosition(actor.GetHasPosition(NeuronBones.RightArm), animator, HumanBodyBones.RightUpperArm, actor.GetReceivedPosition(NeuronBones.RightArm, this.orignalPositions[(int)HumanBodyBones.RightUpperArm]));
            SetPosition(actor.GetHasPosition(NeuronBones.RightForeArm),  animator, HumanBodyBones.RightLowerArm,			actor.GetReceivedPosition( NeuronBones.RightForeArm ,  this.orignalPositions[(int)HumanBodyBones.RightLowerArm ]) );

            // right hand
            if (enableFingerMove)
            {

                SetPosition(actor.GetHasPosition(NeuronBones.RightHand),animator, HumanBodyBones.RightHand,              actor.GetReceivedPosition(NeuronBones.RightHand, this.orignalPositions[(int)HumanBodyBones.RightHand]));
                SetPosition(actor.GetHasPosition(NeuronBones.RightHandThumb1),animator, HumanBodyBones.RightThumbProximal,     actor.GetReceivedPosition(NeuronBones.RightHandThumb1, this.orignalPositions[(int)HumanBodyBones.RightThumbProximal]));
                SetPosition(actor.GetHasPosition(NeuronBones.RightHandThumb2),animator, HumanBodyBones.RightThumbIntermediate, actor.GetReceivedPosition(NeuronBones.RightHandThumb2, this.orignalPositions[(int)HumanBodyBones.RightThumbIntermediate]));
                SetPosition(actor.GetHasPosition(NeuronBones.RightHandThumb3), animator, HumanBodyBones.RightThumbDistal,       actor.GetReceivedPosition(NeuronBones.RightHandThumb3, this.orignalPositions[(int)HumanBodyBones.RightThumbDistal]));

                SetPosition(actor.GetHasPosition(NeuronBones.RightHandIndex1),animator, HumanBodyBones.RightIndexProximal,    actor.GetReceivedPosition(NeuronBones.RightHandIndex1, this.orignalPositions[(int)HumanBodyBones.RightIndexProximal]));
                SetPosition(actor.GetHasPosition(NeuronBones.RightHandIndex2),animator, HumanBodyBones.RightIndexIntermediate, actor.GetReceivedPosition(NeuronBones.RightHandIndex2, this.orignalPositions[(int)HumanBodyBones.RightIndexIntermediate]));
                SetPosition(actor.GetHasPosition(NeuronBones.RightHandIndex3), animator, HumanBodyBones.RightIndexDistal, actor.GetReceivedPosition(NeuronBones.RightHandIndex3, this.orignalPositions[(int)HumanBodyBones.RightIndexDistal]));

                SetPosition(actor.GetHasPosition(NeuronBones.RightHandMiddle1),animator, HumanBodyBones.RightMiddleProximal, actor.GetReceivedPosition(NeuronBones.RightHandMiddle1, this.orignalPositions[(int)HumanBodyBones.RightMiddleProximal]));
                SetPosition(actor.GetHasPosition(NeuronBones.RightHandMiddle2),animator, HumanBodyBones.RightMiddleIntermediate, actor.GetReceivedPosition(NeuronBones.RightHandMiddle2, this.orignalPositions[(int)HumanBodyBones.RightMiddleIntermediate]));
                SetPosition(actor.GetHasPosition(NeuronBones.RightHandMiddle3),animator, HumanBodyBones.RightMiddleDistal, actor.GetReceivedPosition(NeuronBones.RightHandMiddle3, this.orignalPositions[(int)HumanBodyBones.RightMiddleDistal]));

                SetPosition(actor.GetHasPosition(NeuronBones.RightHandRing1),animator, HumanBodyBones.RightRingProximal, actor.GetReceivedPosition(NeuronBones.RightHandRing1, this.orignalPositions[(int)HumanBodyBones.RightRingProximal]));
                SetPosition(actor.GetHasPosition(NeuronBones.RightHandRing2),animator, HumanBodyBones.RightRingIntermediate, actor.GetReceivedPosition(NeuronBones.RightHandRing2, this.orignalPositions[(int)HumanBodyBones.RightRingIntermediate]));
                SetPosition(actor.GetHasPosition(NeuronBones.RightHandRing3),animator, HumanBodyBones.RightRingDistal, actor.GetReceivedPosition(NeuronBones.RightHandRing3, this.orignalPositions[(int)HumanBodyBones.RightRingDistal]));

                SetPosition(actor.GetHasPosition(NeuronBones.RightHandPinky1),animator, HumanBodyBones.RightLittleProximal, actor.GetReceivedPosition(NeuronBones.RightHandPinky1, this.orignalPositions[(int)HumanBodyBones.RightLittleProximal]));
                SetPosition(actor.GetHasPosition(NeuronBones.RightHandPinky2),animator, HumanBodyBones.RightLittleIntermediate, actor.GetReceivedPosition(NeuronBones.RightHandPinky2, this.orignalPositions[(int)HumanBodyBones.RightLittleIntermediate]));
                SetPosition(actor.GetHasPosition(NeuronBones.RightHandPinky3), animator, HumanBodyBones.RightLittleDistal, actor.GetReceivedPosition(NeuronBones.RightHandPinky3, this.orignalPositions[(int)HumanBodyBones.RightLittleDistal]));
            }
			// left arm
			SetPosition(actor.GetHasPosition(NeuronBones.LeftShoulder), animator, HumanBodyBones.LeftShoulder,				actor.GetReceivedPosition( NeuronBones.LeftShoulder, this.orignalPositions[(int)HumanBodyBones.LeftShoulder]) );
			SetPosition(actor.GetHasPosition(NeuronBones.LeftArm), animator, HumanBodyBones.LeftUpperArm,				actor.GetReceivedPosition( NeuronBones.LeftArm,      this.orignalPositions[(int)HumanBodyBones.LeftUpperArm    ]) );
			SetPosition(actor.GetHasPosition(NeuronBones.LeftForeArm), animator, HumanBodyBones.LeftLowerArm,				actor.GetReceivedPosition( NeuronBones.LeftForeArm,  this.orignalPositions[(int)HumanBodyBones.LeftLowerArm]) );

            // left hand
            if (enableFingerMove)
            {
                SetPosition(actor.GetHasPosition(NeuronBones.LeftHand),animator, HumanBodyBones.LeftHand, actor.GetReceivedPosition(NeuronBones.LeftHand, this.orignalPositions[(int)HumanBodyBones.LeftHand]));
                SetPosition(actor.GetHasPosition(NeuronBones.LeftHandThumb1),animator, HumanBodyBones.LeftThumbProximal, actor.GetReceivedPosition(NeuronBones.LeftHandThumb1, this.orignalPositions[(int)HumanBodyBones.LeftThumbProximal]));
                SetPosition(actor.GetHasPosition(NeuronBones.LeftHandThumb2),animator, HumanBodyBones.LeftThumbIntermediate, actor.GetReceivedPosition(NeuronBones.LeftHandThumb2, this.orignalPositions[(int)HumanBodyBones.LeftThumbIntermediate]));
                SetPosition(actor.GetHasPosition(NeuronBones.LeftHandThumb3),animator, HumanBodyBones.LeftThumbDistal, actor.GetReceivedPosition(NeuronBones.LeftHandThumb3, this.orignalPositions[(int)HumanBodyBones.LeftThumbDistal]));
    
                SetPosition(actor.GetHasPosition(NeuronBones.LeftHandIndex1),animator, HumanBodyBones.LeftIndexProximal, actor.GetReceivedPosition(NeuronBones.LeftHandIndex1, this.orignalPositions[(int)HumanBodyBones.LeftIndexProximal]));
                SetPosition(actor.GetHasPosition(NeuronBones.LeftHandIndex2),animator, HumanBodyBones.LeftIndexIntermediate, actor.GetReceivedPosition(NeuronBones.LeftHandIndex2, this.orignalPositions[(int)HumanBodyBones.LeftIndexIntermediate]));
                SetPosition(actor.GetHasPosition(NeuronBones.LeftHandIndex3),animator, HumanBodyBones.LeftIndexDistal, actor.GetReceivedPosition(NeuronBones.LeftHandIndex3, this.orignalPositions[(int)HumanBodyBones.LeftIndexDistal]));
          
                SetPosition(actor.GetHasPosition(NeuronBones.LeftHandMiddle1),animator, HumanBodyBones.LeftMiddleProximal, actor.GetReceivedPosition(NeuronBones.LeftHandMiddle1, this.orignalPositions[(int)HumanBodyBones.LeftMiddleProximal]));
                SetPosition(actor.GetHasPosition(NeuronBones.LeftHandMiddle2),animator, HumanBodyBones.LeftMiddleIntermediate, actor.GetReceivedPosition(NeuronBones.LeftHandMiddle2, this.orignalPositions[(int)HumanBodyBones.LeftMiddleIntermediate]));
                SetPosition(actor.GetHasPosition(NeuronBones.LeftHandMiddle3),animator, HumanBodyBones.LeftMiddleDistal, actor.GetReceivedPosition(NeuronBones.LeftHandMiddle3, this.orignalPositions[(int)HumanBodyBones.LeftMiddleDistal]));
         
                SetPosition(actor.GetHasPosition(NeuronBones.LeftHandRing1),animator, HumanBodyBones.LeftRingProximal, actor.GetReceivedPosition(NeuronBones.LeftHandRing1, this.orignalPositions[(int)HumanBodyBones.LeftRingProximal]));
                SetPosition(actor.GetHasPosition(NeuronBones.LeftHandRing2),animator, HumanBodyBones.LeftRingIntermediate, actor.GetReceivedPosition(NeuronBones.LeftHandRing2, this.orignalPositions[(int)HumanBodyBones.LeftRingIntermediate]));
                SetPosition(actor.GetHasPosition(NeuronBones.LeftHandRing3),animator, HumanBodyBones.LeftRingDistal, actor.GetReceivedPosition(NeuronBones.LeftHandRing3, this.orignalPositions[(int)HumanBodyBones.LeftRingDistal]));
      
                SetPosition(actor.GetHasPosition(NeuronBones.LeftHandPinky1),animator, HumanBodyBones.LeftLittleProximal, actor.GetReceivedPosition(NeuronBones.LeftHandPinky1, this.orignalPositions[(int)HumanBodyBones.LeftLittleProximal]));
                SetPosition(actor.GetHasPosition(NeuronBones.LeftHandPinky2),animator, HumanBodyBones.LeftLittleIntermediate, actor.GetReceivedPosition(NeuronBones.LeftHandPinky2, this.orignalPositions[(int)HumanBodyBones.LeftLittleIntermediate]));
                SetPosition(actor.GetHasPosition(NeuronBones.LeftHandPinky3), animator, HumanBodyBones.LeftLittleDistal, actor.GetReceivedPosition(NeuronBones.LeftHandPinky3, this.orignalPositions[(int)HumanBodyBones.LeftLittleDistal]));
            }
        }
		
		// apply rotations
		
		// legs
		SetRotation( animator, HumanBodyBones.RightUpperLeg,			actor.GetReceivedRotation( NeuronBones.RightUpLeg ) );
		SetRotation( animator, HumanBodyBones.RightLowerLeg, 			actor.GetReceivedRotation( NeuronBones.RightLeg ) );
		SetRotation( animator, HumanBodyBones.RightFoot, 				actor.GetReceivedRotation( NeuronBones.RightFoot ) );
		SetRotation( animator, HumanBodyBones.LeftUpperLeg,				actor.GetReceivedRotation( NeuronBones.LeftUpLeg ) );
		SetRotation( animator, HumanBodyBones.LeftLowerLeg,				actor.GetReceivedRotation( NeuronBones.LeftLeg ) );
		SetRotation( animator, HumanBodyBones.LeftFoot,					actor.GetReceivedRotation( NeuronBones.LeftFoot ) );
		
		// spine
		SetRotation( animator, HumanBodyBones.Spine,					actor.GetReceivedRotation( NeuronBones.Spine ) );
        //SetRotation( animator, HumanBodyBones.Chest,					actor.GetReceivedRotation( NeuronBones.Spine1 ) + actor.GetReceivedRotation( NeuronBones.Spine2 ) + actor.GetReceivedRotation( NeuronBones.Spine3 ) ); 
        if ((skeletonType == NeuronEnums.SkeletonType.PerceptionNeuronStudio))
        {
            SetRotation(animator, HumanBodyBones.Chest,
                (EulerToQuaternion(actor.GetReceivedRotation(NeuronBones.Spine1)) *
                EulerToQuaternion(actor.GetReceivedRotation(NeuronBones.Spine2))).eulerAngles
                );

            SetRotation(animator, HumanBodyBones.Neck,
                (EulerToQuaternion(actor.GetReceivedRotation((NeuronBones)NeuronBonesV2.Neck)) *
                EulerToQuaternion(actor.GetReceivedRotation((NeuronBones)NeuronBonesV2.Neck1))).eulerAngles
                );
        }
        else
        {
            SetRotation(animator, HumanBodyBones.Chest,
                (EulerToQuaternion(actor.GetReceivedRotation(NeuronBones.Spine1)) *
                EulerToQuaternion(actor.GetReceivedRotation(NeuronBones.Spine2)) *
                EulerToQuaternion(actor.GetReceivedRotation(NeuronBones.Spine3))).eulerAngles
                );
            SetRotation(animator, HumanBodyBones.Neck, actor.GetReceivedRotation(NeuronBones.Neck));
        }

		SetRotation( animator, HumanBodyBones.Head,						actor.GetReceivedRotation( NeuronBones.Head ) );
		
		// right arm
		SetRotation( animator, HumanBodyBones.RightShoulder,			actor.GetReceivedRotation( NeuronBones.RightShoulder ) );
		SetRotation (animator, HumanBodyBones.RightUpperArm,		actor.GetReceivedRotation (NeuronBones.RightArm) );
		SetRotation( animator, HumanBodyBones.RightLowerArm,			actor.GetReceivedRotation( NeuronBones.RightForeArm ) );
		
		// right hand
		SetRotation( animator, HumanBodyBones.RightHand,				actor.GetReceivedRotation( NeuronBones.RightHand ) );
		SetRotation( animator, HumanBodyBones.RightThumbProximal,		actor.GetReceivedRotation( NeuronBones.RightHandThumb1 ) );
		SetRotation( animator, HumanBodyBones.RightThumbIntermediate,	actor.GetReceivedRotation( NeuronBones.RightHandThumb2 ) );
		SetRotation( animator, HumanBodyBones.RightThumbDistal,			actor.GetReceivedRotation( NeuronBones.RightHandThumb3 ) );

        //SetRotation( animator, HumanBodyBones.RightIndexProximal,		actor.GetReceivedRotation( NeuronBones.RightHandIndex1 ) + actor.GetReceivedRotation( NeuronBones.RightInHandIndex ) );
        SetRotation(animator, HumanBodyBones.RightIndexProximal, 
            (EulerToQuaternion(actor.GetReceivedRotation(NeuronBones.RightInHandIndex)) *
             EulerToQuaternion(actor.GetReceivedRotation(NeuronBones.RightHandIndex1))).eulerAngles
            );
        SetRotation( animator, HumanBodyBones.RightIndexIntermediate,	actor.GetReceivedRotation( NeuronBones.RightHandIndex2 ) );
		SetRotation( animator, HumanBodyBones.RightIndexDistal,			actor.GetReceivedRotation( NeuronBones.RightHandIndex3 ) );

        //SetRotation( animator, HumanBodyBones.RightMiddleProximal,		actor.GetReceivedRotation( NeuronBones.RightHandMiddle1 ) + actor.GetReceivedRotation( NeuronBones.RightInHandMiddle ) );
        SetRotation(animator, HumanBodyBones.RightMiddleProximal, 
            (EulerToQuaternion(actor.GetReceivedRotation(NeuronBones.RightInHandMiddle)) *
             EulerToQuaternion(actor.GetReceivedRotation(NeuronBones.RightHandMiddle1))).eulerAngles
            );
        SetRotation( animator, HumanBodyBones.RightMiddleIntermediate,	actor.GetReceivedRotation( NeuronBones.RightHandMiddle2 ) );
		SetRotation( animator, HumanBodyBones.RightMiddleDistal,		actor.GetReceivedRotation( NeuronBones.RightHandMiddle3 ) );

        //SetRotation( animator, HumanBodyBones.RightRingProximal,		actor.GetReceivedRotation( NeuronBones.RightHandRing1 ) + actor.GetReceivedRotation( NeuronBones.RightInHandRing ) );
        SetRotation(animator, HumanBodyBones.RightRingProximal, 
            (EulerToQuaternion(actor.GetReceivedRotation(NeuronBones.RightInHandRing)) *
             EulerToQuaternion(actor.GetReceivedRotation(NeuronBones.RightHandRing1))).eulerAngles
            );
        SetRotation( animator, HumanBodyBones.RightRingIntermediate,	actor.GetReceivedRotation( NeuronBones.RightHandRing2 ) );
		SetRotation( animator, HumanBodyBones.RightRingDistal,			actor.GetReceivedRotation( NeuronBones.RightHandRing3 ) );

        //SetRotation( animator, HumanBodyBones.RightLittleProximal,		actor.GetReceivedRotation( NeuronBones.RightHandPinky1 ) + actor.GetReceivedRotation( NeuronBones.RightInHandPinky ) );
        SetRotation(animator, HumanBodyBones.RightLittleProximal, 
            (EulerToQuaternion(actor.GetReceivedRotation(NeuronBones.RightInHandPinky)) *
             EulerToQuaternion(actor.GetReceivedRotation(NeuronBones.RightHandPinky1))).eulerAngles
            );
        SetRotation( animator, HumanBodyBones.RightLittleIntermediate,	actor.GetReceivedRotation( NeuronBones.RightHandPinky2 ) );
		SetRotation( animator, HumanBodyBones.RightLittleDistal,		actor.GetReceivedRotation( NeuronBones.RightHandPinky3 ) );
		
		// left arm
		SetRotation( animator, HumanBodyBones.LeftShoulder,				actor.GetReceivedRotation( NeuronBones.LeftShoulder ) );
		SetRotation( animator, HumanBodyBones.LeftUpperArm,				actor.GetReceivedRotation( NeuronBones.LeftArm ) );
		SetRotation( animator, HumanBodyBones.LeftLowerArm,				actor.GetReceivedRotation( NeuronBones.LeftForeArm ) );
		
		// left hand
		SetRotation( animator, HumanBodyBones.LeftHand,					actor.GetReceivedRotation( NeuronBones.LeftHand ) );
		SetRotation( animator, HumanBodyBones.LeftThumbProximal,		actor.GetReceivedRotation( NeuronBones.LeftHandThumb1 ) );
		SetRotation( animator, HumanBodyBones.LeftThumbIntermediate,	actor.GetReceivedRotation( NeuronBones.LeftHandThumb2 ) );
		SetRotation( animator, HumanBodyBones.LeftThumbDistal,			actor.GetReceivedRotation( NeuronBones.LeftHandThumb3 ) );

        //SetRotation( animator, HumanBodyBones.LeftIndexProximal,		actor.GetReceivedRotation( NeuronBones.LeftHandIndex1 ) + actor.GetReceivedRotation( NeuronBones.LeftInHandIndex ) );
        SetRotation(animator, HumanBodyBones.LeftIndexProximal, 
            (EulerToQuaternion(actor.GetReceivedRotation(NeuronBones.LeftInHandIndex)) *
             EulerToQuaternion(actor.GetReceivedRotation(NeuronBones.LeftHandIndex1))).eulerAngles
            );
        SetRotation( animator, HumanBodyBones.LeftIndexIntermediate,	actor.GetReceivedRotation( NeuronBones.LeftHandIndex2 ) );
		SetRotation( animator, HumanBodyBones.LeftIndexDistal,			actor.GetReceivedRotation( NeuronBones.LeftHandIndex3 ) );

        //SetRotation( animator, HumanBodyBones.LeftMiddleProximal,		actor.GetReceivedRotation( NeuronBones.LeftHandMiddle1 ) + actor.GetReceivedRotation( NeuronBones.LeftInHandMiddle ) );
        SetRotation(animator, HumanBodyBones.LeftMiddleProximal, 
            (EulerToQuaternion(actor.GetReceivedRotation(NeuronBones.LeftInHandMiddle)) *
             EulerToQuaternion(actor.GetReceivedRotation(NeuronBones.LeftHandMiddle1))).eulerAngles
            );
        SetRotation( animator, HumanBodyBones.LeftMiddleIntermediate,	actor.GetReceivedRotation( NeuronBones.LeftHandMiddle2 ) );
		SetRotation( animator, HumanBodyBones.LeftMiddleDistal,			actor.GetReceivedRotation( NeuronBones.LeftHandMiddle3 ) );

        //SetRotation( animator, HumanBodyBones.LeftRingProximal,			actor.GetReceivedRotation( NeuronBones.LeftHandRing1 ) + actor.GetReceivedRotation( NeuronBones.LeftInHandRing ) );
        SetRotation(animator, HumanBodyBones.LeftRingProximal, 
            (EulerToQuaternion(actor.GetReceivedRotation(NeuronBones.LeftInHandRing)) *
             EulerToQuaternion(actor.GetReceivedRotation(NeuronBones.LeftHandRing1))).eulerAngles
            );
        SetRotation( animator, HumanBodyBones.LeftRingIntermediate,		actor.GetReceivedRotation( NeuronBones.LeftHandRing2 ) );
		SetRotation( animator, HumanBodyBones.LeftRingDistal,			actor.GetReceivedRotation( NeuronBones.LeftHandRing3 ) );

        //SetRotation( animator, HumanBodyBones.LeftLittleProximal,		actor.GetReceivedRotation( NeuronBones.LeftHandPinky1 ) + actor.GetReceivedRotation( NeuronBones.LeftInHandPinky ) );
        SetRotation(animator, HumanBodyBones.LeftLittleProximal,
            (EulerToQuaternion(actor.GetReceivedRotation(NeuronBones.LeftInHandPinky)) *
             EulerToQuaternion(actor.GetReceivedRotation(NeuronBones.LeftHandPinky1))).eulerAngles
            );
        SetRotation( animator, HumanBodyBones.LeftLittleIntermediate,	actor.GetReceivedRotation( NeuronBones.LeftHandPinky2 ) );
		SetRotation( animator, HumanBodyBones.LeftLittleDistal,			actor.GetReceivedRotation( NeuronBones.LeftHandPinky3 ) );		
	}
	
    Quaternion EulerToQuaternion(Vector3 euler)
    {
        return Quaternion.Euler(euler.x, euler.y, euler.z);
    }

	// apply Transforms of src bones to Rigidbody Components of dest bones
	public void ApplyMotionPhysically( Animator src, Animator dest )
	{


        for ( HumanBodyBones i = 0; i < HumanBodyBones.LastBone; ++i )
		{
            Transform src_transform = phy_src_transforms[(int)i];// src.GetBoneTransform( i );
            Transform dest_transform = phy_dest_transform[(int)i];// dest.GetBoneTransform( i );
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
                        Quaternion dAng = src_transform.rotation * Quaternion.Inverse(dest_transform.rotation);
                        float angle = 0.0f;
                        Vector3 axis = Vector3.zero;
                        dAng.ToAngleAxis(out angle, out axis);

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

						Vector3 v = Vector3.MoveTowards(rigidbody.velocity, velocityTarget2, 100.0f);
                            if (ValidateVector3(v))
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

	void ApplyVelocity(Rigidbody rb, Vector3 velocityTarget, Vector3 angularTarget)
	{
		Vector3 v = Vector3.MoveTowards(rb.velocity, velocityTarget, 10.0f);
		if( ValidateVector3( v ) )
		{
			rb.velocity = v;
		}

		v = Vector3.MoveTowards(rb.angularVelocity, angularTarget, 10.0f);
		if( ValidateVector3( v ) )
		{
			rb.angularVelocity = v;
		}
	}


    Transform[] phy_src_transforms = new Transform[(int)HumanBodyBones.LastBone];
    Transform[] phy_dest_transform = new Transform[(int)HumanBodyBones.LastBone];
    //bool InitPhysicalContext()
    void InitPhysicalContext(Animator src, Animator dest)
	{
        if (dest != null)
        {
            for (HumanBodyBones i = 0; i < HumanBodyBones.LastBone; ++i)
            {
                if (phy_dest_transform[(int)i] == null)
                    phy_dest_transform[(int)i] = dest.GetBoneTransform(i);
            }
        }
        if ( physicalReference.Init( boundAnimator, physicalReferenceOverride ) )
        {
            // break original object's hierachy of transforms, so we can use MovePosition() and MoveRotation() to set transform
            NeuronHelper.BreakHierarchy(boundAnimator);      
			//return true;
		}

		CheckRigidbodySettings ();
        if(src != null)
        {
            for (HumanBodyBones i = 0; i < HumanBodyBones.LastBone; ++i)
            {
                if(phy_src_transforms[(int)i] == null)
                    phy_src_transforms[(int)i] = src.GetBoneTransform(i);
            }
        }
        //	return false;
    }
	
	void ReleasePhysicalContext()
	{
		physicalReference.Release();
	}
	
	void UpdateOffset()
	{
		// we do some adjustment for the bones here which would replaced by our model retargeting later

        if( boundAnimator != null )
        {
            // initiate values
            for (int i = 0; i < (int)HumanBodyBones.LastBone; ++i)
            {
                bonePositionOffsets[i] = Vector3.zero;
                boneRotationOffsets[i] = Vector3.zero;
            }

            if (boundAnimator != null)
            {
                Transform leftLegTransform = boundAnimator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
                Transform rightLegTransform = boundAnimator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
                if (leftLegTransform != null)
                {
                    bonePositionOffsets[(int)HumanBodyBones.LeftUpperLeg] = new Vector3(0.0f, leftLegTransform.localPosition.y, 0.0f);
                    bonePositionOffsets[(int)HumanBodyBones.RightUpperLeg] = new Vector3(0.0f, rightLegTransform.localPosition.y, 0.0f);
                    bonePositionOffsets[(int)HumanBodyBones.Hips] = new Vector3(0.0f, -(leftLegTransform.localPosition.y + rightLegTransform.localPosition.y) * 0.5f, 0.0f);
                }
            }
        }
	}

    void CaluateOrignalRot()
    {
        for (int i = 0; i < orignalPositions.Length; i++)
        {
            Transform t = boundAnimator.GetBoneTransform((HumanBodyBones)i);

            orignalPositions[i] = t == null ? Vector3.zero : t.localPosition; ;
        }

        for (int i = 0; i < orignalRot.Length; i++)
        {
            Transform t = boundAnimator.GetBoneTransform((HumanBodyBones)i);

            orignalRot[i] = t == null ? Quaternion.identity : t.localRotation;
        }
        Transform t0 = boundAnimator.GetBoneTransform((HumanBodyBones.Hips));
        for (int i = 0; i < orignalRot.Length; i++)
        {
            Quaternion parentQs = Quaternion.identity;
            Transform t = boundAnimator.GetBoneTransform((HumanBodyBones)i);
            if (t == null || i == (int)HumanBodyBones.Hips)
            {
                orignalParentRot[i] = Quaternion.identity;
                continue;
            }
            Transform tempParent = t.transform.parent;
            while (tempParent != null)
            {
                parentQs = tempParent.transform.localRotation * parentQs;
                tempParent = tempParent.parent;
                if (tempParent == null || tempParent == transform || (t0 != null && tempParent == t0.transform.parent)  )
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

		for( HumanBodyBones i = 0; i < HumanBodyBones.LastBone; ++i )
		{
			Transform t = boundAnimator.GetBoneTransform( i );
			if( t != null )
			{
				Rigidbody r = t.GetComponent<Rigidbody> ();
				if (r != null) {
					r.isKinematic = kinematicSetting;
				}
			}
		}
	}
}