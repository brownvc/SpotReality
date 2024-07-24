# Drive

In this mode, the user can control Spot to move around with the joysticks, and can command Spot to move higher or lower. This is all managed by the VRDriveSpot script. The controls are as follows:

* Move forward/back/left/right: left joystick
* Rotate: right joystick
* Stand higher: click the right joystick down
* Stand lower: click the left joystick down

When any of the above controls are activated, the VRDriveSpot component sends the combined movement, rotation, and height to the MoveSpot component of the RosConnector object, which then sends the command to Spot.