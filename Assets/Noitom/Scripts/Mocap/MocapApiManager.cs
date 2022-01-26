using UnityEngine;
using System.Collections;
using MocapApi;
using Neuron;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;


public static class MocapApiManager
{

    static bool isConnectFailed = false;
    public static bool IsConnectFailed
    {
        get
        {
            return IsConnectFailed;
        }
    }

    static List<NeuronSource> AllNeuronSources = new List < NeuronSource >();
    static Dictionary<string, ulong> connectionApplications = new Dictionary<string, ulong>();

    public static NeuronSource RequareConnection(string address, int port, int portUdpServer,  NeuronEnums.SocketType socketType, NeuronEnums.SkeletonType skeletonType)
    {
        string connStrId = GetConnectionStringId(address, port, socketType);
        ulong applicationHandle;
        if (connectionApplications.ContainsKey(connStrId))
        {
            applicationHandle = connectionApplications[connStrId];
        }
        else
        {
            applicationHandle = CreateApplicationConnection(address, port, portUdpServer, socketType, skeletonType);
            if (applicationHandle > 0)
                connectionApplications.Add(connStrId, applicationHandle);
            else
                return null;
        }

        NeuronSource neuronSrc = RequareNeuronSourceInConnection(address, port, socketType);
        neuronSrc.applicationHandle = applicationHandle;
        return neuronSrc;
    }
    public static void Disconnect(NeuronSource source)
    {
        if (source != null)
        {
            source.Release();
            //if (source.referenceCounter == 0)
            //{
            //    DestroyConnection(source);
            //}
        }
    }

    static string GetConnectionStringId(string address, int port, NeuronEnums.SocketType socketType)
    {
        return address +"__" + port + "__" + socketType;
    }


    static ulong CreateApplicationConnection(string address, int port, int portUdpServer, NeuronEnums.SocketType socketType, NeuronEnums.SkeletonType skeletonType)
    {
        isConnectFailed = false;
        ulong applicationHandle = 0;
        EMCPError error = IMCPApplication.Application.CreateApplication(ref applicationHandle);

        if(error == EMCPError.Error_None)
        {
            ulong settings = 0;
            IMCPSettings.Settings.CreateSettings(ref settings);
            if(skeletonType ==  NeuronEnums.SkeletonType.PerceptionNeuronStudio)
                IMCPSettings.Settings.SetSettingsBvhData(EMCPBvhData.BvhDataType_Binary, settings);
            else
                IMCPSettings.Settings.SetSettingsBvhData(EMCPBvhData.BvhDataType_Binary | EMCPBvhData.BvhDataType_Mask_LegacyHumanHierarchy, settings);
            IMCPSettings.Settings.SetSettingsBvhRotation(EMCPBvhRotation.BvhRotation_YXZ, settings);
            IMCPSettings.Settings.SetSettingsBvhTransformation(EMCPBvhTransformation.BvhTransformation_Enable, settings);
            if (socketType == NeuronEnums.SocketType.UDP)
            {
                IMCPSettings.Settings.SetSettingsUDP((ushort)port, settings);
                IMCPSettings.Settings.SetSettingsUDPServer(address, (ushort)portUdpServer, settings);                
            }
            else
                IMCPSettings.Settings.SetSettingsTCP(address, (ushort)port, settings);
            IMCPApplication.Application.SetApplicationSettings(settings, applicationHandle);
            IMCPSettings.Settings.DestroySettings(settings); 
            IMCPApplication.Application.OpenApplication(applicationHandle);


        }
        else
        {
            Debug.LogErrorFormat("Error on connect to appliction, error code: {0}", error);
        }

        if(applicationHandle > 0)
        {
            ulong renderSettings = 0;
            /*
            IMCPRenderSettings.RenderSettings.CreateRenderSettings(ref renderSettings);
            IMCPRenderSettings.RenderSettings.SetUpVector(EMCPUpVector.UpVector_YAxis, 1, renderSettings);
            IMCPRenderSettings.RenderSettings.SetFrontVector(EMCPFrontVector.FrontVector_ParityEven, 1, renderSettings);
            IMCPRenderSettings.RenderSettings.SetCoordSystem(EMCPCoordSystem.CoordSystem_LeftHanded, renderSettings);
            IMCPRenderSettings.RenderSettings.SetRotatingDirection(EMCPRotatingDirection.RotatingDirection_Clockwise, renderSettings);
            IMCPRenderSettings.RenderSettings.SetUnit(EMCPUnit.Uint_Meter, renderSettings);
            IMCPApplication.Application.SetApplicationRenderSettings(renderSettings, applicationHandle);
            IMCPRenderSettings.RenderSettings.DestroyRenderSettings(renderSettings);
            */
            IMCPRenderSettings.RenderSettings.GetPreDefRenderSettings(EMCPPreDefinedRenderSettings.PreDefinedRenderSettings_Unity3D, ref renderSettings);
            IMCPApplication.Application.SetApplicationRenderSettings(renderSettings, applicationHandle);

            Debug.LogFormat("Connect to {0} {1} {2} success, handlerId: {3}", address, port, socketType, applicationHandle);
        }
        else
        {
            Debug.LogErrorFormat("Failed to connect to appliction");
        }

        return applicationHandle;

    }

    static bool alreadyDestroying = false;
    public static void OnDestroy()
    {
        if (alreadyDestroying)
            return;
        alreadyDestroying = true;
        foreach (var item in connectionApplications)
        {
            IMCPApplication.Application.CloseApplication(item.Value);
            // IMCPApplication.Application.DestroyApplication(item.Value);
        }
    }


    static int lastUpdateFrame = -1;
    static uint MCPEvent_tSize = 0;
    const uint McpEventBufCount = 128;
    static MCPEvent_t[] ev = new MCPEvent_t[128];
    public static void Update(int unityFrame)
    {
        if (lastUpdateFrame == unityFrame)
            return;
        lastUpdateFrame = unityFrame;
        if (MCPEvent_tSize == 0)
        {
            MCPEvent_tSize = (uint)Marshal.SizeOf(typeof(MCPEvent_t));
            for (uint i = 0; i < ev.Length; ++i)
            {
                ev[i].size = MCPEvent_tSize;
            }
        }

        var enumerator = connectionApplications.GetEnumerator();
        while (enumerator.MoveNext())
        {
            var pair = enumerator.Current;
            ulong appHandlerId = pair.Value;

            uint bufInOutcount = McpEventBufCount;
            var errorCode = IMCPApplication.Application.PollApplicationNextEvent(ev, ref bufInOutcount, appHandlerId);

            if (errorCode == EMCPError.Error_None && bufInOutcount > 0)
            {
                //Debug.Log("bufInOutcount " + bufInOutcount + " avatars : " + AllNeuronSources.Count);
                for (int i = 0; i < bufInOutcount ; i++)
                {
                    if (ev[i].eventType == EMCPEventType.MCPEvent_AvatarUpdated)
                    {
                        //Debug.Log("avatar: " + ev[i].data.avatarHandle);
                        for (int k = 0; k < AllNeuronSources.Count; k++)
                        {
                            var connectionSrc = AllNeuronSources[k];
                            if (connectionSrc.HasActorReference && connectionSrc.applicationHandle == appHandlerId)
                            {
                                // update the src
                                HandleAvatarUpdated(ev[i].eventData.motionData.avatarHandle, connectionSrc);
                            }
                        }
                    }
                    else if (ev[i].eventType == EMCPEventType.MCPEvent_TrackerUpdated)
                    {
                        HandleTrackerUpdateEvent(ev[i].eventData.trackerData._trackerHandle);
                    }
                    else if(ev[i].eventType == EMCPEventType.MCPEvent_Error)
                    {
                        isConnectFailed = true;
                        Debug.LogErrorFormat("Failed to connect to appliction, MCPEvent_Error");
                    }
                    else
                    {
                        Debug.Log("On event " + ev[i].eventType);
                    }
                      
                }

            }
        }

    }

    static void HandleAvatarUpdated(ulong avatarHandle, NeuronSource neuronSource)
    {
        //xxx = "";
        ulong rootJointHandler = 0;
        IMCPAvatar.Avatar.GetAvatarRootJoint(ref rootJointHandler, avatarHandle);
        uint avatarIndex = 0;

        IMCPAvatar.Avatar.GetAvatarIndex(ref avatarIndex, avatarHandle);
        NeuronActor neuronActor = neuronSource.AcquireActor((int)avatarIndex);
        if(!neuronActor.HsReceivedData)
        {
            neuronActor.HsReceivedData = true;
            string avatarName = "";
            IMCPAvatar.Avatar.GetAvatarName(ref avatarName, avatarHandle);
            neuronActor.SetAvatarName(avatarName);
            Debug.LogFormat("Created avatar  name:{0} id:{1}", avatarName, avatarIndex);
        }

        RecurrenceParseJointPQ(rootJointHandler, neuronActor, 0);

        ulong[] avatarRigidbodys = new ulong[64];
        uint rigidbodyCount = 64;
        IMCPAvatar.Avatar.GetAvatarRigidBodies(avatarRigidbodys, ref rigidbodyCount, avatarHandle);

        if (rigidbodyCount > 0)
        {
            for (int i = 0; i < rigidbodyCount && i < avatarRigidbodys.Length; i++)
            {
                int rigidbodyId = 0;
                IMCPRigidBody.RigidBody.GetRigidBodyId(ref rigidbodyId, avatarRigidbodys[i]);

                float qx = 0f, qy = 0f, qz = 0f, qw = 1f;
                float px = 0f, py = 0f, pz = 0f;
                IMCPRigidBody.RigidBody.GetRigidBodyRotation(ref qx, ref qy, ref qz, ref qw, avatarRigidbodys[i]);
                IMCPRigidBody.RigidBody.GetRigidBodyPosition(ref px, ref py, ref pz, avatarRigidbodys[i]);
                Quaternion q = new Quaternion(qx, qy, qz, qw);
                Vector3 p = new Vector3(px, py, pz);

                //Debug.Log(rigidbodyId  + " " + p+ " " + q);
                if (NeuronActor.rigidbodyLocalPositions.ContainsKey(rigidbodyId))
                {
                    NeuronActor.rigidbodyLocalPositions[rigidbodyId] = p;
                }
                else
                {
                    Debug.Log("Add rigidbody, id: " + rigidbodyId);
                    NeuronActor.rigidbodyLocalPositions.Add(rigidbodyId, p);
                }

                if (NeuronActor.rigidbodyLocalRotations.ContainsKey(rigidbodyId))
                {
                    NeuronActor.rigidbodyLocalRotations[rigidbodyId] = q;
                }
                else
                {
                    NeuronActor.rigidbodyLocalRotations.Add(rigidbodyId, q);
                }
            }
        }
        //Debug.Log(xxx);
    }

    static bool HandleTrackerUpdateEvent(ulong TrackerHandle)
    {
        IMCPTracker TrackerMgr = IMCPTracker.Tracker;

        int TrackerCount = 0;
        var error = TrackerMgr.GetDeviceCount(ref TrackerCount, TrackerHandle);
        if (error != EMCPError.Error_None)
        {
            return false;
        }

        for (int Idx = 0; Idx < TrackerCount; ++Idx)
        {
            string name = "";
            TrackerMgr.GetDeviceName(Idx, ref name, TrackerHandle);


            float qx = 0f, qy = 0f, qz = 0f, qw = 1f;
            float px = 0f, py = 0f, pz = 0f;
            TrackerMgr.GetTrackerRotation(ref qx, ref qy, ref qz, ref qw, name, TrackerHandle);
            TrackerMgr.GetTrackerPosition(ref px, ref py, ref pz, name, TrackerHandle);
            Quaternion q = new Quaternion(qx, qy, qz, qw);
            Vector3 p = new Vector3(px, py, pz);

            //Debug.Log(rigidbodyId  + " " + p+ " " + q);
            if (NeuronActor.trackerLocalPositions.ContainsKey(name))
            {
                NeuronActor.trackerLocalPositions[name] = p;
            }
            else
            {
                Debug.Log("Add rigidbody, id: " + name);
                NeuronActor.trackerLocalPositions.Add(name, p);
            }

            if (NeuronActor.trackerLocalRotations.ContainsKey(name))
            {
                NeuronActor.trackerLocalRotations[name] = q;
            }
            else
            {
                NeuronActor.trackerLocalRotations.Add(name, q);
            }

        }
        return true;
    }


    //static ulong[] childrenHandlerIdBufs = new ulong[64];
    static void RecurrenceParseJointPQ(ulong parentJointHandle, NeuronActor neuronSource, int levelCount)
    {
        if(levelCount > 64)
        {
            Debug.LogError("RecurrenceParseJointPQ Exception");
            return;
        }
        string jointName = "";
        IMCPJoint.Joint.GetJointName(ref jointName, parentJointHandle);

        Vector3 v = new Vector3();
        //if (neuronSource.AvatarWithDisplacement)

        //if (levelCount == 0 || neuronSource.AvatarWithDisplacement)
        //{
        //    EMCPError errCode = IMCPJoint.Joint.GetJointLocalTransformation(ref v.x, ref v.y, ref v.z, parentJointHandle);
        //    if (levelCount == 0)
        //        neuronSource.SetAvatarWithDisplacement(errCode == EMCPError.Error_None);
        //}
        EMCPError errCode = IMCPJoint.Joint.GetJointLocalPosition(ref v.x, ref v.y, ref v.z, parentJointHandle);

        Quaternion q = new Quaternion();
        IMCPJoint.Joint.GetJointLocalRotation(ref q.x, ref q.y, ref q.z, ref q.w, parentJointHandle);

        // int boneId = BoneNameToBoneId(jointName);
        EMCPJointTag pJointTag = EMCPJointTag.JointTag_Invalid;
        IMCPJoint.Joint.GetJointTag(ref pJointTag, parentJointHandle);
        int boneId = (int)pJointTag;
        // mocapapi 枚举和neuron枚举不一致, 对于在axis数据的spine3需要这样人工处理一下
        if (pJointTag == EMCPJointTag.JointTag_Spine3)
        {
            boneId = (int)NeuronBones.Spine3;
        }

        if (boneId >= 0 && boneId < (int)NeuronBones.NumOfBones)
        {
            //if (neuronSource.AvatarWithDisplacement)
            neuronSource.SourceHasLocalPositions[boneId] = (errCode == EMCPError.Error_None);
            neuronSource.SourceLocalPositions[boneId] = (errCode == EMCPError.Error_None) ? v : Vector3.zero;
            neuronSource.SourceLocalRotations[boneId] = q;
            //Debug.LogWarning(neuronSource.AvatarIndex + " " + neuronSource.sourceLocalRotations[boneId]);
        }


        uint numberOfChild = 0;
        IMCPJoint.Joint.GetJointChild(null, ref numberOfChild, parentJointHandle);
        if (numberOfChild > 0)
        {
            //if(numberOfChild > childrenHandlerIdBufs.Length)
            // TODO: 用静态buffer代替此处重复new的操作
            var  childrenHandlerIdBufs = new ulong[numberOfChild];
            IMCPJoint.Joint.GetJointChild(childrenHandlerIdBufs, ref numberOfChild, parentJointHandle);
            for (int i = 0; i < numberOfChild; i++)
            {
                RecurrenceParseJointPQ(childrenHandlerIdBufs[i], neuronSource, levelCount + 1);
            }
        }
    }





    public static int numOfSources { get { return AllNeuronSources.Count; } }



    static NeuronSource RequareNeuronSourceInConnection(string address, int port, NeuronEnums.SocketType socketType)
    {
        NeuronSource source = FindNeuronSourceInConnection(address, port, socketType);
        if (source != null)
        {
            source.Grab();
            return source;
        }

        source = CreateConnection(address, port, socketType);
        if (source != null)
        {
            source.Grab();
            return source;
        }

        return null;
    }
    static NeuronSource CreateConnection(string address, int port, NeuronEnums.SocketType socketType)
    {
        NeuronSource source = null;
        source = new NeuronSource(address, port, socketType);
        AllNeuronSources.Add(source);
        return source;
    }
    static NeuronSource FindNeuronSourceInConnection(string address, int port, NeuronEnums.SocketType socketType)
    {
        NeuronSource source = null;
        for (int i = 0; i < AllNeuronSources.Count; i++)
        {
            var it = AllNeuronSources[i];

            if (it.socketType == NeuronEnums.SocketType.UDP && socketType == NeuronEnums.SocketType.UDP && it.port == port)
            {
                source = it;
                break;
            }
            else if (it.socketType == NeuronEnums.SocketType.TCP && socketType == NeuronEnums.SocketType.TCP && it.address == address && it.port == port)
            {
                source = it;
                break;
            }
        }
        return source;
    }


    // 临时: 目前 mocapAPi传入的是字符串作为id, 上层逻辑出于效率原因期望用的是int, 在这一层做个封装转换, 待mocapAPi更改后去除
    //static int BoneNameToBoneId(string boneName)
    //{
    //    for (int i = 0; i < tempConvertStrTable.Length; i++)
    //    {
    //        if (tempConvertStrTable[i] == boneName)
    //            return i;        }

    //    return -1;
    //}
    //// 临时的字符串=>id映射表
    //static string[] tempConvertStrTable = new string[]
    //{
    //    "Hips"                    ,    // Hips = 0,
    //    "RightUpLeg"              ,    // RightUpLeg = 1,
    //    "RightLeg"                ,    // RightLeg = 2,
    //    "RightFoot"               ,    // RightFoot = 3,
    //    "LeftUpLeg"               ,    // LeftUpLeg = 4,
    //    "LeftLeg"                 ,    // LeftLeg = 5,
    //    "LeftFoot"                ,    // LeftFoot = 6,
    //    "Spine"                   ,    // Spine = 7,
    //    "Spine1"                  ,    // Spine1 = 8,
    //    "Spine2"                  ,    // Spine2 = 9,
    //    "Neck"                    ,    // Neck = 10,
    //    "Neck1"                   ,    // Neck1 = 11,
    //    "Head"                    ,    // Head = 12,
    //    "RightShoulder"           ,    // RightShoulder = 13,
    //    "RightArm"                ,    // RightArm = 14,
    //    "RightForeArm"            ,    // RightForeArm = 15,
    //    "RightHand"               ,    // RightHand = 16,
    //    "RightHandThumb1"         ,    // RightHandThumb1 = 17,
    //    "RightHandThumb2"         ,    // RightHandThumb2 = 18,
    //    "RightHandThumb3"         ,    // RightHandThumb3 = 19,
    //    "RightInHandIndex"        ,    // RightInHandIndex = 20,
    //    "RightHandIndex1"         ,    // RightHandIndex1 = 21,
    //    "RightHandIndex2"         ,    // RightHandIndex2 = 22,
    //    "RightHandIndex3"         ,    // RightHandIndex3 = 23,
    //    "RightInHandMiddle"       ,    // RightInHandMiddle = 24,
    //    "RightHandMiddle1"        ,    // RightHandMiddle1 = 25,
    //    "RightHandMiddle2"        ,    // RightHandMiddle2 = 26,
    //    "RightHandMiddle3"        ,    // RightHandMiddle3 = 27,
    //    "RightInHandRing"         ,    // RightInHandRing = 28,
    //    "RightHandRing1"          ,    // RightHandRing1 = 29,
    //    "RightHandRing2"          ,    // RightHandRing2 = 30,
    //    "RightHandRing3"          ,    // RightHandRing3 = 31,
    //    "RightInHandPinky"        ,    // RightInHandPinky = 32,
    //    "RightHandPinky1"         ,    // RightHandPinky1 = 33,
    //    "RightHandPinky2"         ,    // RightHandPinky2 = 34,
    //    "RightHandPinky3"         ,    // RightHandPinky3 = 35,
    //    "LeftShoulder"            ,    // LeftShoulder = 36,
    //    "LeftArm"                 ,    // LeftArm = 37,
    //    "LeftForeArm"             ,    // LeftForeArm = 38,
    //    "LeftHand"                ,    // LeftHand = 39,
    //    "LeftHandThumb1"          ,    // LeftHandThumb1 = 40,
    //    "LeftHandThumb2"          ,    // LeftHandThumb2 = 41,
    //    "LeftHandThumb3"          ,    // LeftHandThumb3 = 42,
    //    "LeftInHandIndex"         ,    // LeftInHandIndex = 43,
    //    "LeftHandIndex1"          ,    // LeftHandIndex1 = 44,
    //    "LeftHandIndex2"          ,    // LeftHandIndex2 = 45,
    //    "LeftHandIndex3"          ,    // LeftHandIndex3 = 46,
    //    "LeftInHandMiddle"        ,    // LeftInHandMiddle = 47,
    //    "LeftHandMiddle1"         ,    // LeftHandMiddle1 = 48,
    //    "LeftHandMiddle2"         ,    // LeftHandMiddle2 = 49,
    //    "LeftHandMiddle3"         ,    // LeftHandMiddle3 = 50,
    //    "LeftInHandRing"          ,    // LeftInHandRing = 51,
    //    "LeftHandRing1"           ,    // LeftHandRing1 = 52,
    //    "LeftHandRing2"           ,    // LeftHandRing2 = 53,
    //    "LeftHandRing3"           ,    // LeftHandRing3 = 54,
    //    "LeftInHandPinky"         ,    // LeftInHandPinky = 55,
    //    "LeftHandPinky1"          ,    // LeftHandPinky1 = 56,
    //    "LeftHandPinky2"          ,    // LeftHandPinky2 = 57,
    //    "LeftHandPinky3"               // LeftHandPinky3 = 58,
    //};


}
