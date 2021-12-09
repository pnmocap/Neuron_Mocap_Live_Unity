using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Neuron
{
    public static class AliceMotionBindHelper 
    {

        public static string GetEnumBoneName(int id, bool isRigV2)
        {
            if (isRigV2 && (id == (int)Neuron.NeuronBones.Spine3 || id == (int)Neuron.NeuronBones.Neck))
                return Enum.GetName(typeof(NeuronBonesV2), (NeuronBonesV2)id);
            else
                return Enum.GetName(typeof(NeuronBones), (NeuronBones)id);
        }

        public static int Bind(Transform root, Transform[] bones, string prefix = "", NeuronBoneVersion forceRigVersion = NeuronBoneVersion.V2)
        {
            int foundCount = 0;
            //bool shouldExactlyMatch = !string.IsNullOrEmpty(prefix);
            const bool shouldExactlyMatch = false;
            for (int i = 0; i < (int)Neuron.NeuronBonesV2.NumOfBones; i++)
            {
                string enumName = Enum.GetName(typeof(Neuron.NeuronBonesV2), (Neuron.NeuronBonesV2)i);
                string enumName2 = string.Empty;

                if ((i == (int)Neuron.NeuronBones.Spine3 || i == (int)Neuron.NeuronBones.Neck))
                {
                    if (forceRigVersion == NeuronBoneVersion.NotSet_AutoByName)
                    {
                        enumName = GetEnumBoneName(i, true);
                        enumName2 = GetEnumBoneName(i, false);
                    }
                    else if (forceRigVersion ==  NeuronBoneVersion.V1)
                    {
                        enumName = GetEnumBoneName(i, false);
                    }
                    else if (forceRigVersion == NeuronBoneVersion.V2)
                    {
                        enumName = GetEnumBoneName(i, true);
                    } 
                }
                else
                {
                    if (enumName.StartsWith("RightInHand"))
                    {
                        enumName2 = enumName.Replace("RightInHand", "RightHand");
                    }
                    else if (enumName.StartsWith("RightHand"))
                    {
                        enumName2 = enumName.Replace("RightHand", "RightInHand");
                    }
                    else if (enumName.StartsWith("LeftInHand"))
                    {
                        enumName2 = enumName.Replace("LeftInHand", "LeftHand");
                    }
                    else if (enumName.StartsWith("LeftHand"))
                    {
                        enumName2 = enumName.Replace("LeftHand", "LeftInHand");
                    }
                }

                

                //if (shouldExactlyMatch)
                //    enumName = prefix + enumName;
                Transform foundTrans = FindChild(root, enumName, shouldExactlyMatch);
                if(foundTrans == null)
                {
                    if(!string.IsNullOrEmpty(enumName2))
                    {
                        enumName = enumName2;
                    }
                    foundTrans = FindChild(root, enumName, shouldExactlyMatch);
                }
                if (foundTrans != null)
                {
                    foundCount++;
                    if (bones[i] == null)
                        bones[i] = foundTrans;
                }
                else
                {
                    if (bones[i] != null)
                        foundCount++;
                    else
                        Debug.LogWarningFormat("can't find {0} bone under {1}", enumName, root.name);
                }
            }
            return foundCount;
        }


        static Transform FindChild(Transform father, string name, bool shouldExactlyMatch)
        {
            Transform trans = null;
            int childCount = father.childCount;

            if (shouldExactlyMatch)
            {
                if (father.name == name)
                {
                    trans = father;
                    return father;
                } 

            }
            else
            {
                if (father.name.EndsWith(name, System.StringComparison.OrdinalIgnoreCase))
                {
                    trans = father;
                    return father;
                }
            }


            for (int i = 0; i < childCount; i++)
            {
                trans = FindChild(father.GetChild(i), name, shouldExactlyMatch);
                if (trans != null)
                    break;
            }
            return trans;
        }
    }
}
