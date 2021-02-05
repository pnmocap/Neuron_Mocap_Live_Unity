# NEURON MOCAP LIVE Plugin for Unity
 
This plugin provide the ability to stream motion data from Axis Studio into Unity.

### Prerequisites
  
```
Unity 5.4.2p4 or higher

Window x86_64
```
 
## Getting Started
 
```
Import the SDK package to unity

open Axis Studio software

Run QuickStart Scene
```
 
## Usage

* Live motion data to animator or common avatar transform
  add "NeuronAnimatorInstance.cs" or "NeuronTransformsInstance.cs" component to your avatar gameobject
  
* Live  rigidbody props data from PNSHybridMocap
  add "NeuronRigidbody.cs" component to your prop gameobject


## Public Variables of Neuronanimatorinstance.cs
*	Address is the IP address of the machine running Axis Studio. If it's the same computer, the address should be 127.0.0.1. You can use this stream motion data from any other computer in the same network. In this case change the address accordingly.
*	Port Tcp is the port number associated with the BVH data stream output setting in Axis for tcp
*	Port Udp is the port number associated with the BVH data stream output setting in Axis for udp
*	Actor ID is the id number for the actor you want to use. If you have more than one actor connected in Axis Neuron this id number will increase. Default is 0 which is the first actor. 
*	Connect To Axis means the script will connect to Axis Neuron and apply the motion data. You can use this toggle to starts/stop the data stream.
*	Skeleton Type if it’s value is Perception Neuron Studio, it means the script will use the PN Studio seleton structure which includes 3 joints of spine, and 2 joints of neck. 
    if it’s value is Perception Neuron, it means the script will use the PN/PN Pro seleton structure which includes 4 joints of spine and 1 joint of neck.
*   Bound Animator the Animator component this instance should use for the received motion data. You can use this if you don’t want to keep the script on the same GameObject as the animator component.
*   Motion Update Method tells the instance if it should use rigidbody functions provided by Unity to move and rotate each bone. The default method is to apply the received float values directly to the transform components of each bone.
*   Enable Hip Move tells the instance if it should apply avatar’s hip transform movement
*   Enable Finger Move tells the instance if it should apply avatar’s finger transform movement
