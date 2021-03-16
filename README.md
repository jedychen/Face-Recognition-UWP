# Face-Detection-UWP
 UWP/C# - Face Detection and Estimates using Onnx Models

<!-- ABOUT THE PROJECT -->
## About The Project
This is a UWP version for face detection, generating faical landmarks and camera distance estimation.

<!-- GETTING STARTED -->
## Getting Started

### Prerequisites

* Windows x64
* Visual Studio 2019

### Installation

Open `FaceRecognitionUWP/FaceRecognitionUWP.sln`

Add packages to `FaceRecognitionUWP`
* Microsoft.AI.DirectML
* Microsoft.AI.MachineLearning

Add packages to `OpenCVBridge`
* OpenCV.Win.Core
* OpenCV.Win.ImgProc

Add packages to `OpenCVBridge`
* OpenCV.Win.Core
* OpenCV.Win.ImgProc

Set Solution Configuration to `Debug` and platform to `x64`

<!-- USAGE EXAMPLES -->
## Usage

1. Open the app.
2. There are 2 modes: Camera and Image. You can click button "Image/Camera" to switch between 2 modes.
3. Image Mode
** Click "Select image" to load a local image
** Click "Recognize" to detect face and display a rectangle for detected faces
** Click "Show/Hide Distance" to toggle display of camera distance estimation
4. Camera Mode
** Click "Image/Camera" to activate Camera Mode
** Click "Show/Hide Distance" to toggle display of camera distance estimation

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
