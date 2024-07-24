# ScriptRunner

This object exists simply as an extra object for processing independent of other objects. It currently only has one component.

## ModifyIntrinsics
The ModifyIntrinsics component is designed to get camera intrinsics from an IntrinsicsSubscriber component of RosConnector, and then set the "CX", "CY", "FX", and "FY" fields of each point cloud script. 

Since intrinsics remain constant for each robot, they only need to be retrieved over ROS once. This happens in the following way: three seconds after the Unity application starts, ModifyIntrinsics sends a "GetIntrinsics" request through RosConnector, which tells ROS to send Intrinsics once. RosConnector also has an InstinsicsSubscriber, which will receive the intrinsics once RosConnector publishes them. The IntrinsicsSubscriber is a component of the ModifyIntrinsics script, so once it gets new intrinsics, it sets the necessary point cloud fields. This process is a bit complicated, but saves intrinsics from needing to constantly be sent over ROS, which would waste metwork bandwidth.