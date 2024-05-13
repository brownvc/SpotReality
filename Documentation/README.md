# Documentation Approach
SpotReality is a complex Unity application. The files in this folder will attempt to document the full range of functionality with a top-down approach starting from root GameObjects. Each relevant top-level GameObject has its own file or folder, which will explain the that object's role in the overall application, and how each of its child objects and components fit in. The documentation will all be for the Spot-PointCloud-ArmControl scene, which is the main version of the project.

## Undocumented Objects
The "XR Interaction Manager", "XR Origin", and "EventSystem" GameObjects do not have their own documentation files, because they were imported from open source libraries to allow the system to interact with the VR headset. You can read documentation about the process that set up those objects [here](https://xrbootcamp.com/unity-vr-tutorial-for-beginners/). Additionally, the RobotArmIK object does not have documentation because it is for a feature that is not implemented.

## Running the System
Below are step by step instructions for running the whole system (Spot, Linux ROS PC, Windows PC, VR headset). The Linux PC is managed through Windows

Setting up the linux PC
1. Turn on Spot, wait a few minutes for fans to quiet a bit and front lights are a solid color, indicating ready to operate.
2. Open PowerShell
3. Log in to Linux PC (ssh ericbot@spot) or ericbot@ whatever the ip of the Linux computer is (ssh ericbot@138.16.160.231)
4. Open docker (dockerbot), short for starting the container named beautiful_nightingale
5. Test connection to Spot: "sourcespot rooter", press y. This is the main point of failure, if it says "Unable to connect":
	1. Check that ethernet is plugged in
	2. Check that the IP address of Rooter hasn't changed:
		1. Connect to Rooter on the tablet, in Admin Console->Network Setup find ethernet's IP address
		2. If it has changed:
            1. Open setup_spot.bash
		    2. Change rooter's SPOT_ETH_IP
		    3. Save file, exit docker and reopen
6. Once connected, run "tmux kill-session -t 0"
7. Run "dev rooter" -- this opens a tmux session with five terminals running.
	1. Sometimes the connection to the robot will fail. If this happens, ctrl-C in all 5 terminals, then run the last command again in each of the terminals, starting with the top-left, then top-right, then the bottom-3.
	2. If Spot does not stand up, then either the connection failed, or the stand up function has been commented out. If the connection looks good, check spot_ros.py for a stand command that has been commented out. You can uncomment that command and relaunch the terminals.
8. Now you're ready to jump to Unity
9. Once done, ctrl-C out of each one, and then enter the following command:
	ctrl-A, :kill-session
10. Turn off and dock Spot

Running Unity
1. In documents/SpotReality, make sure repo is on right branch. Open the Unity Project
2. Connect the headset with a cable to the Windows PC.
3. In the headset, find the Quest Link option, and connect to the PC
4. Run the Unity application. 