using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Neuron
{

    public class NeuronSourceManager : MonoBehaviour
    {
        [Header("Connection settings:")]
        [HideInInspector]
        public string address = "127.0.0.1";
        [HideInInspector]
        public int portTcp = 7003;
        [HideInInspector]
        public int portUdpServer = 7003;
        [HideInInspector]
        public int portUdp = 7004;
        public NeuronEnums.SocketType socketType = NeuronEnums.SocketType.TCP;

        public NeuronEnums.SkeletonType skeletonType = NeuronEnums.SkeletonType.PerceptionNeuronStudio;

        //[Space(10)]
        [Header("Whether to connect to axis software")]
        public bool connectToAxis = true;
        protected bool hasConnected;

        protected NeuronSource source;


        protected void ToggleConnect()
        {
            //if (standalone)
            {
                if (connectToAxis)
                {
                    hasConnected = Connect();
                }
                else if (!connectToAxis)
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
            source = MocapApiManager.RequareConnection(address, 
                GetPortByConnectionType(), 
                portUdpServer, 
                socketType, 
                skeletonType);

            NeuronInstance[] instances = GetComponentsInChildren<NeuronInstance>();
            foreach (var instance in instances)
            {
                instance.Init(source, skeletonType);
            }
            
            return source != null;
        }

        protected void Disconnect()
        {
            NeuronInstance[] instances = GetComponentsInChildren<NeuronInstance>();
            foreach (var instance in instances)
            {
                instance.Showdown();
            }

            MocapApiManager.Disconnect(source);

            source = null;
        }

        // Use this for initialization
        void Start()
        {
            ToggleConnect();
        }

        void OnDestroy()
        {
            
        }

        // Update is called once per frame
        void Update()
        {

        }
    }

}