# CONTROL SYSTEM

The Control System object is center of all user input to the system from the VR controllers. The "CONTROL SYSTEM" object itself is the parent of the two modes the user can be in to operate the robot — Drive and Dynamic Arm, as well as the Locomotion System to allow the user to fly around in the scene. Each of those elements will be discussed in its own file in this directory. The components of the parent object are documented here.

## Mode Manager
This component is responsible for switching between Drive and Arm Mode. It is called by the VRGeneralControls component. The system starts in Drive Mode, and when the "nextMode()" method is called from VRGeneralControls, ModeManager puts the system into Arm mode. It returns to Drive mode the next time "nextMode()" is called, and so on. If more modes are added, they will need to be added to the "Modes" list, a public attribute of this script. Each mode has a ControlMode component, and when ModeManager switches modes, it enables everything in the "Objects of Mode to Enable" field of ControlMode, and all the components in . It also disables every element that was in the other modes' lists to enable.

This component also controls the hint text that is displayed to the user in VR, which tells the user what mode is currently active.

Finally, this component is responsible for sending the far plane via ROS if depth compression is used. When depth compression is active (see RosParent.md), depth values are compressed up to a max distance (far plane). The lower the far plane value, the more precise the depth values are, but the user is limited to viewing up to that distance. Because of that tradeoff, the far plane is set to 6 meters in Drive mode, so the user can see farther for navigation, and 2 meters in Dynamic Arm mode, so the values are as precise as possible for up-close manipulation. The compression is done on the ROS machine in python, and that system needs to know what the far plane is when it performs compression. To accomplish this, the RosConnector object has a SetFarPlane component, which publishes the necessary far plane over ROS. When the user switches modes, ModeManager calls SetFarPlane to change the value accordingly.

## VR General Controls
This component manages all the VR controller input actions that are available in both Drive and Dynamic Arm modes. Those controls are explained below. Note that "LT1/RT1" refer to the triggers for the index fingers, and "LT2/RT2" refer to the triggers for the middle fingers.
* Emergency stop: if the user presses LT1, RT1, and the "x" button, an emergency stop command will be sent to Spot.
* Switch control modes: when "a" on the right controller is pressed, the system toggles between Drive and Dynamic Arm modes.
* Switch between 2D and 3D: when the "x" button on the left controller is pressed, it toggles every point cloud between 2D mode and 3D mode (see spot.md). The system starts in 3D mode. When 2D switching to 2D mode, the surrounding point clouds are all enabled. When in 3D mode, all are disabled except front left and front right.
* Stow arm: when LT2 is pressed, Spot's arm is stowed.
* Open/Close Gripper: when RT2 is pressed, Spot's gripper changes from closed to open or open to closed.

Additionally, this script tracks the total time spent in 2D and 3D mode, and when the Unity application is closed, it logs the total time.
