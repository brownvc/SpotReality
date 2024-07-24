# Dynamic Arm

When entering this mode, Spot's body is set to translucent, so that the user can see through it while manipulating. This mode gives the user the following controls:
* Move the arm: while the right index trigger (RT1) is pressed, the relative movement (position and rotation) of the right controller will be applied to the dummy arm (see spot.md). Additionally, as soon as the trigger is pressed, the PoseStampedRelativePublisher component of RosConnector (see RosParent.md) is enabled. While it is activated, it constantly sends a command to move Spot's arm to the location of the dummy arm. This effectively allows the user to move Spot's arm by moving their own hand.
* Freeze point cloud: when the "b" button on the right controller is pressed, the "toggleFreezeCloud()" method of each point cloud object is called This allows the user to freeze the point cloud in place to avoid occlusion, and unfreeze when done.
* Slow gripper open/close: while the left index trigger (LT1) is pressed, moving the left joystick up or down commands the gripper to slowly open or close. This is done by changing the gripper open percentage by 0.25% every frame, and sending a command with that percentage via the SetGripper component of RosConnector.

Additionally, while in this mode the LocomotionSystem is enabled, allowing the user to adjust their position in the scene.