# Spot

## Robot Rendering
"spot" is the root object for rendering Spot's body. The [RosReality](https://ieeexplore.ieee.org/document/8593513) project created Unity functionality that made it possible to import full robot specifications from a URDF file into a Unity GameObject hierarchy. The "spot" GameObject's with its child "body", with its children "base_link", "front_rail" etc. all the way to "sh0 Canvas", were all generated from this process.

Every joint in the robot comes with visuals and meshes. The joints are updated live so that when the scene is running, the rendering moves in real-time with Spot. Each of the joints are referenced and updated live from the JointStateSubscriber component of RosConnector, which is discussed in RosParent.md.

The GameObjects after "sh0 Canvas" are separate features that were created as children of the "body" object so that their position in the scene could move with the robot. Those features are discussed in the next sections. The "ghost arm" object can be ignored, as it was intended to be part of the Inverse Kinematics feature is not implemented.

## Dummy Hand
To allow the user to control the movement of Spot's arm, we created a copy of the generated object for the hand. The copy is called "dummy_arm0.link_wr1". While the user is operating the application in Arm Mode, it can control the position of this dummy hand, as explained in CONTROL SYSTEM/DynamicArm.md. The position of the hand is then sent to the robot over ROS using the PoseStampedRelativePublisher.cs script, another component of RosConnector.

## Point Clouds
The "H", "B", "FL", "FR", "L", and "R" objects each correspond to one of the robot's cameras — hand, back, front left, front right, left, and right. 

### Extrinsics
Each object's transform is situated based on extrinsics received live from the robot, so that the scene rendered to the user is as accurate relative to the robot as possible. The extrinsics are all controlled in the TransformUpdater component of each point cloud object. Each TransformUpdater component connects to an ExtrinsicsSubscriber component of RosConnector, which is responsible for retrieving the extrinsics from the robot. There is a checkbox that can negate each positional and rotational value, because some of the cameras give values that need negation. These boxes were checked through trial and error.

### Rendering a Point Cloud
Every point cloud object has a child object, i.e. "hand_cloud", which holds a DrawMeshInstanced component. That script uses several inputs in order to accurately render the cloud.

First, it gets the intrinsics of each camera to set the "CX", "CY", "FX", and "FY" fields. This process is described in ScriptRunner.md.

Next, each point cloud needs color and depth information for the point cloud it renders. These are constantly updated through the "Depth Subscriber" and "Color Subscriber" fields, which connect to components that are children of the RosParent and are explained in RosParent.md.

Because the bandwidth to send six depth arrays through ROS is too demanding, only the front left and front right cameras get depth data. The rest of the point cloud objects are disabled by default. However, color information for those cameras is still received, and there is a user control that enables 2D mode (discussed in ControlSystem/README.md). In 2D mode, the depth arrays are not necessary, so all 6 point cloud objects are enabled and rendered in 2D, meaning the "T" field of the DrawMeshInstanced script is set to 0.

The point clouds also have a public method "setCloudFreeze", which is also accessed by the control system. It allows the caller to set the point cloud to freeze or unfreeze the point cloud. When frozen, no new color and depth arrays are processed, and the point cloud becomes static in the scene.

The actual point cloud rendering is done with compute shaders, and is too complex to describe here.