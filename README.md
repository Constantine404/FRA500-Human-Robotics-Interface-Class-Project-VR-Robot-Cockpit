# FRA500-Human-Robotics-Interface-Class-Project-VR-Robot-Cockpit-
# Unity ROS VR Robot Controller

A Unity project for controlling and visualizing a robot arm via ROS (Robot Operating System) in a VR environment. The system streams camera feeds, renders LiDAR data, syncs joint states, and supports XR controller input for real-time robot control.

---

## Scripts Overview

| Script | Class | Description |
|--------|-------|-------------|
| `CameraStreamer.cs` | `CameraStreamer` | Subscribes to a ROS compressed image topic and displays it on a `RawImage` UI element |
| `CircularRadarHud.cs` | `CircularRadarHud` | Renders LiDAR scan data as a circular radar HUD on a `RawImage` texture |
| `FollowFace.cs` | `FollowHead` | Makes a UI panel follow the player's head position in VR |
| `JointSyncManager_VR.cs` | `JointSyncManager` | Full joint control: syncs sliders, 3D model, and XR joystick input with ROS servo commands |
| `RobotJoyController.cs` | `RobotControllerXR` | Drives the robot base using the left XR controller joystick via `/cmd_vel` |
| `RobotStateSubscriber.cs` | `RobotStateSubscriber` | Subscribes to `/joint_states` and mirrors real robot positions onto the 3D model |
| `Text_Display.cs` | `ReadBattery` | Reads battery voltage from a ROS `UInt16` topic and displays it as 3D text |

---

## ROS Topics

| Topic | Message Type | Direction | Used By |
|-------|-------------|-----------|---------|
| `/depth_cam/rgb/image_raw/compressed` | `sensor_msgs/CompressedImage` | Subscribe | `CameraStreamer` |
| `/scan` | `sensor_msgs/LaserScan` | Subscribe | `CircularRadarHud` |
| `/joint_states` | `sensor_msgs/JointState` | Subscribe | `JointSyncManager`, `RobotStateSubscriber` |
| `/servo_controller` | `ServoController/ServosPosition` | Publish | `JointSyncManager` |
| `/cmd_vel` | `geometry_msgs/Twist` | Publish | `RobotJoyController` |
| `/ros_robot_controller/battery` | `std_msgs/UInt16` | Subscribe | `ReadBattery` |

---

## Script Details

### `CameraStreamer.cs`
Displays a live camera stream from ROS inside the VR scene.

**Inspector Fields:**
- `displayImage` — Drag a `RawImage` UI component here
- `topicName` — ROS topic name (default: `/depth_cam/rgb/image_raw/compressed`)

**How it works:** Receives `CompressedImage` messages, decodes the byte array into a Unity `Texture2D`, and updates the `RawImage` each frame.

---

### `CircularRadarHud.cs`
Renders a 2D LiDAR radar display using Unity's `Texture2D`.

**Inspector Fields:**
- `displayImage` — `RawImage` to render radar onto
- `topicName` — LiDAR topic (default: `/scan`)
- `textureResolution` — Radar texture size in pixels (default: `256`)
- `maxRadarDistance` — Max real-world distance shown on radar in meters (default: `5.0`)
- `backgroundColor` — Background color of the radar
- `pointSize` — Size of each LiDAR point in pixels (default: `2`)
- `distanceGradient` — Color gradient from close (red) to far (green)
- `invertX` — Mirror left/right
- `rotationOffset` — Rotate the radar view (0–360°)

---

### `FollowFace.cs` *(class: `FollowHead`)*
Keeps a UI panel positioned in front of the player's head.

**Inspector Fields:**
- `head` — Reference to the XR camera / head transform
- `distance` — Distance in front of the head (default: `1.0`)
- `heightOffset` — Vertical offset from head position (default: `0.3`)

>  Note: The filename is `FollowFace.cs` but the class name is `FollowHead`.

---

### `JointSyncManager_VR.cs` *(class: `JointSyncManager`)*
The main arm control script. Handles UI sliders, 3D model sync, XR joystick input, and ROS publishing.

**Inspector Fields:**
- `pubTopic` — Servo command topic (default: `/servo_controller`)
- `subTopic` — Joint state feedback topic (default: `/joint_states`)
- `jointSliders` — Array of 5 UI `Slider` components
- `jointNames` — ROS joint name strings (default: `joint1`–`joint5`)
- `servoIDs` — Servo hardware IDs (default: `1`–`5`)
- `isInverted` — Per-joint inversion flags
- `robotJoints` — Array of `ArticulationBody` components for 3D model
- `joystickSpeed` — Degrees/second for joystick control (default: `300`)
- `publishRate` — ROS publish interval in seconds (default: `0.05`, i.e. 20 Hz)

**XR Controls (Right Hand):**
- **Joystick X-Axis** → Rotates Joint 1 left/right
- **Button A** → Resets all joints to home position (`[500, 500, 150, 500, 500]`)

**Pulse Range:** 0–1000, where 500 = center position. Uses `±2.094395 rad (±120°)` for conversion.

---

### `RobotJoyController.cs` *(class: `RobotControllerXR`)*
Controls the robot's base movement using the left XR controller.

**Inspector Fields:**
- `topicName` — Velocity command topic (default: `/cmd_vel`)
- `linearSpeed` — Forward/backward speed multiplier (default: `1.0`)
- `angularSpeed` — Rotation speed multiplier (default: `1.0`)

**XR Controls (Left Hand):**
- **Joystick Y-Axis** → `linear.x` (forward/backward)
- **Joystick X-Axis** → `angular.z` (turn left/right)

---

### `RobotStateSubscriber.cs` *(class: `RobotStateSubscriber`)*
Mirrors real robot joint angles onto the Unity 3D model. Simpler alternative to `JointSyncManager` when only visual feedback is needed (no control).

**Inspector Fields:**
- `topicName` — Joint state topic (default: `/joint_states`)
- `targetJointNames` — Joint names to listen for
- `robotJoints` — `ArticulationBody` array matching the joint names above

Converts radians from ROS to degrees and sets `xDrive.target` on each `ArticulationBody`.

---

### `Text_Display.cs` *(class: `ReadBattery`)*
Displays battery voltage as 3D text in the VR scene.

**Inspector Fields:**
- `topicName` — Battery topic (default: `/ros_robot_controller/battery`)
- `batteryText` — `TextMeshPro` component for display
- `healthyColor` — Text color when voltage is normal (default: green)
- `criticalColor` — Text color when voltage is low (default: red)
- `lowBatteryThreshold` — Voltage threshold in volts (default: `10.0`)

**Data format:** Receives `UInt16` in millivolts (e.g. `12000` → `12.00 V`).

---

## 🛠️ Dependencies

- [Unity Robotics Hub — ROS-TCP-Connector](https://github.com/Unity-Technologies/ROS-TCP-Connector)
- `RosMessageTypes.Sensor` — `CompressedImageMsg`, `LaserScanMsg`, `JointStateMsg`
- `RosMessageTypes.Geometry` — `TwistMsg`
- `RosMessageTypes.Std` — `UInt16Msg`
- `RosMessageTypes.ServoController` — `ServosPositionMsg`, `ServoPositionMsg` *(custom message)*
- Unity XR Interaction Toolkit
- TextMeshPro

---

##  Setup

1. Install the [ROS-TCP-Connector](https://github.com/Unity-Technologies/ROS-TCP-Connector) package in Unity.
2. Configure `ROSConnection` with your ROS master IP and port.
3. Add scripts to GameObjects and assign Inspector fields as described above.
4. Ensure your ROS side publishes/subscribes to the matching topic names.
5. For `JointSyncManager`, make sure the custom `ServoController` ROS message package is built and sourced.

---

##  Notes

- `JointSyncManager` and `RobotStateSubscriber` both subscribe to `/joint_states` — use only one per scene unless you need both slider UI and pure visual sync simultaneously.
- All scripts use `ROSConnection.GetOrCreateInstance()`, so only one `ROSConnection` component is needed in the scene.
- Radar texture calls `Apply()` every frame when new LiDAR data arrives — consider reducing `textureResolution` if performance is a concern.

---
##  Developer Member
Kraiwich Vichakhon
Khanapon Katthanyakit

