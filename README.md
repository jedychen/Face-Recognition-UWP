# Face Detection Demo - UWP / C#
Face Detection and Estimates using Onnx Models

<!-- ABOUT THE PROJECT -->
## About The Project
This is an UWP demo for face detection, generating faical landmarks and camera distance estimation.

The Onnx model processing for face detection, faical landmarks is based on the python implementation of [cunjian/pytorch_face_landmark](https://github.com/cunjian/pytorch_face_landmark).

<!-- GETTING STARTED -->
## Installation

### Option 1: Use App Package

#### Prerequisites
* Windows x64

#### Steps
1. Download `FaceRecognitionUWP/AppPackages.zip`.
2. Unzip the file and navigate to `AppPackages/FaceRecognitionUWP_1.0.2.0_Test/`.
3. Right click the file `Add-AppDevPackage.ps1` and select "Run with powershell".
4. You will need to use "Run as administrator" option.
5. Agree (type "Y") to all the requests and the app should be installed on your windows.
6. Find and open the app `FaceRecognitionUWP`.

### Option 2: Run Source Code

#### Prerequisites
* Windows x64
* Visual Studio 2019

#### Steps
1. Open `FaceRecognitionUWP/FaceRecognitionUWP.sln`
2. Add packages to `FaceRecognitionUWP`
	1. Microsoft.AI.DirectML
	2. Microsoft.AI.MachineLearning
3. Add packages to `OpenCVBridge`
	1. MOpenCV.Win.Core
	2. OpenCV.Win.ImgProc
4. Set Solution Configuration to `Debug` and platform to `x64`
5. You can run app in Visual Studio 2019

<!-- USAGE EXAMPLES -->
## Usage

1. Open the app.
2. There are 2 modes: Camera and Image. You can click button "Live Camera" to switch between 2 modes.
3. Image Mode
	1. Click "Select image" to load a local image
	2. Click "Recognize" to detect face and display a rectangle for detected faces
	3. Toggle "Distance" to activate facial landmarks
4. Camera Mode
	1. Click "Live Camera" to activate Camera Mode
	2. Toggle "Distance" to activate camera distance estimation
5. There are some face images in `FaceRecognitionUWP/Assets/Images` for testing face detection.

<!-- LICENSE -->
## License

Distributed under the MIT License

<!-- CONTACT -->
## Contact

Jedy Chen - [Portfolio Site](https://jedychen.com/) - jedy829@gmail.com

<!-- ACKNOWLEDGEMENTS -->
## Acknowledgements
* [UWP Tutorial: Create a Windows Machine Learning UWP application (C#)](https://docs.microsoft.com/en-us/windows/ai/windows-ml/get-started-uwp)
* [UWP Tutorial: Detect objects using ONNX in ML.NET](https://docs.microsoft.com/en-us/dotnet/machine-learning/tutorials/object-detection-onnx#use-the-model-for-scoring)
* [UWP Tutorial: Create or edit a SoftwareBitmap programmatically](https://docs.microsoft.com/en-us/windows/uwp/audio-video-camera/imaging#create-or-edit-a-softwarebitmap-programmatically)
* [UWP Tutorial: Process media frames with MediaFrameReader - UWP applications](https://docs.microsoft.com/en-us/windows/uwp/audio-video-camera/process-media-frames-with-mediaframereader)
* [UWP Tutorial: Integrate OpenCV with UWP](https://docs.microsoft.com/en-us/windows/uwp/audio-video-camera/process-software-bitmaps-with-opencv)
* [Github: cunjian/pytorch_face_landmark](https://github.com/cunjian/pytorch_face_landmark)
* [Github: takuya-takeuchi/UltraFaceDotNet](https://github.com/takuya-takeuchi/UltraFaceDotNet)
* [Github: Downscale the pixel values in UWP App](https://github.com/Microsoft/Windows-Machine-Learning/issues/22)
* [Pinhole Camera Model](https://en.wikipedia.org/wiki/Pinhole_camera_model)
