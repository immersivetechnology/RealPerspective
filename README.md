# RealPerspective
RealPerspective utilizes Intel&reg; RealSense&trade; technology to create a unique experience. This code sample utilizes head tracking to perform a monoscopic technique for better 3D fidelity. For more details, please see this  [article](https://software.intel.com/en-us/articles/realperspective-head-tracking-with-intel-realsense-technology).

## Build and Deploy
The Intel&reg; RealSense&trade; SDK and the Depth Camera Manager are required for development.

For deploying the project to end-users, the matching SDK Runtime must be installed.

### Unity
For Unity, to ensure compatibility with the SDK installed on the system, please replace `libpxcclr.unity.dll` and `libpxccpp2c.dll` in _Libraries\x64_ and _Libraries\x86_ of the project with the DLLs from _bin\x64_ and _bin\x86_ of the SDK respectively.

## Requirements
- Intel&reg; RealSense&trade; Camera
- Intel&reg; RealSense&trade; SDK (R5 release or newer)
- Windows 8.1 or newer
- Unity 5.3 or newer

Intel&reg; RealSense&trade; SDK: https://software.intel.com/en-us/intel-realsense-sdk/download.
