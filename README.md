# NEURON MOCAP LIVE Plugin for Unity
This plugin provide the ability to stream motion data from Axis Studio into Blender.

#Requirement

Unity 5.4.2p4 or higher
Window x86_64

Features
  This document should help you get familiar with real-time motion capture data reading and how to use fbx file exported from Axis Neuron/Axis Studio  inside the game engine Unity 3D. Axis Neuron/Axis Studio not only allows the export of motion capture data but also be able to stream motion capture data in real-time to third party applications, making the data available to drive characters in animation.
The data stream to the game engine is identical regardless if you use recorded or live motion data. This allows you to record certain actions and use them to test before putting on the full system and doing a live test.You can have multiple actors at the same time inside Axis and have them all streamed into Unity.The data stream is based on the BVH structure including header information and body dimension data is available from Axis via a command interface.Because motion-data is streamed over network the computer running Axis Neuron doesn’t necessarily have to be the same computer running Unity.
If you find bugs or need help please contact us at Noitom_service@noitom.com  
If you’re looking to extend your real-time Perception Neuron experience, then have a look at the NeuronDataReader SDK.

