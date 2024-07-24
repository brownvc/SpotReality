# RosParent

The children of RosParent run all communication with the ROS system. Originally, all ROS processing was done within the RosConnctor object, but we found that separating out the color and depth information into separate GameObjects increased the efficiency of the scene.

## Depth Subscribers
The FrontRightDepthSubscriber and FrontLeftDepthSubscriber objects each have a RawImageSubscriber component that retrieves depth information over ROS. When a new depth array comes in, the raw image updates its own depth array object, which is referenced by the corresponding point cloud (see spot.md). Because new depth information that comes in is extremely sparse, the new depth value for each pixel takes the average over the last n frames, where n is set by the image_data_cbuffer_length variable. More advanced depth completion algorithms may replace this in the future.

The "compressed" checkbox controls whether the depth array is expected to have been compressed from 16 bits per depth value to 8 bits. It must correspond to whether the ROS computer is compressing the image, so that the format can be as expected. The Far Plane is retrieved as the image width when a new image comes in, so that depth can be uncompressed. This is a bit of a hack, but we can overwrite the width field because the images have a constant shape.

## Color Subscribers
The FrontRightColorSubscriber and FrontLeftColorSubscriber objects have a JPEGImageSubscriber component, which retrieves color information to go along with the depth data. Because color comes in much faster than depth, the script keeps a history of recent color data that was retrieved. Each depth and color array comes with a timestamp, so when a new depth array is detected, the color subscriber looks for the color array in its history that has the nearest timestamp to that depth array. The JPEGImageSubscriber has a reference to its associated RawImageSubscriber in order to check its timestamps, and whether a new array has been received. The color image is stored in a public Texture 2D variable that is accessed by the point cloud scripts at the same time as the depth.

The left, right, and back color subscribers have the same functionality as front left and front right, but are disabled in 3D mode, because they have no associated depth subscriber. When the associated RawImageSubscriber is empty, the time sync logic is not used.

## Ros Connector
The rest of the ROS logic is much lighter weight than image processing, and is all run as part of the RosConnector object. Here is an overview of each component and its role:
* PoseStampedRelativePublisher: while enabled, continually sends a command to Spot to move the real arm to the same position and rotation of the dummy arm (see spot.cs).
* JointStatePatcher: a script that looks through the children of the spot GameObject and adds each of the joints to the JointStateSubscriber and JointStatePublisher components.
* OdometrySubscriber: currently disabled because it is unused, but is able to detect Spot's estimation for its position relative to where it was booted up.
* MoveSpot: publishes velocity commands to ROS to move the robot or adjust its standing height.
* StowArm: publishes a command to ROS to stow the arm.
* KillMotor: publishes an emergency stop command.
* SetGripper: publishes the desired gripper percentage.
* JPEGImageSubscribers and RawImageSubscribers are disabled because they were moved to different children of RosParent.
* JointStatePublisher/JointStateSubscriber: manage updating the positions of every joint so that the rendering of Spot matches real Spot.
* GhostArmPublisher: unused
* ArmInputPublisher: unused
* ExtrinsicsSubscriber (one for each depth array): only enabled for frontleft and frontright cameras, update the extrinsics so that the point cloud has the correct offset.
* GetIntrinsics: publishes a request for ROS to send the intrinsics.
* IntrinsicsSubscriber: receives the intrinsics when ROS responds to the GetIntrinsics request.
* SetFarPlane: publishes the far plane for depth compression (see ControlSystem/README.md).