#include "pch.h"
#include "OpenCVHelper.h"
#include "MemoryBuffer.h"

using namespace OpenCVBridge;
using namespace Platform;
using namespace Windows::Graphics::Imaging;
using namespace Windows::Foundation;
using namespace Microsoft::WRL;
using namespace cv;

bool OpenCVHelper::GetPointerToPixelData(SoftwareBitmap^ bitmap, unsigned char** pPixelData, unsigned int* capacity)
{
    BitmapBuffer^ bmpBuffer = bitmap->LockBuffer(BitmapBufferAccessMode::ReadWrite);
    IMemoryBufferReference^ reference = bmpBuffer->CreateReference();

    ComPtr<IMemoryBufferByteAccess> pBufferByteAccess;
    if ((reinterpret_cast<IInspectable*>(reference)->QueryInterface(IID_PPV_ARGS(&pBufferByteAccess))) != S_OK)
    {
        return false;
    }

    if (pBufferByteAccess->GetBuffer(pPixelData, capacity) != S_OK)
    {
        return false;
    }
    return true;
}

bool OpenCVHelper::TryConvert(SoftwareBitmap^ from, Mat& convertedMat)
{
    unsigned char* pPixels = nullptr;
    unsigned int capacity = 0;
    if (!GetPointerToPixelData(from, &pPixels, &capacity))
    {
        return false;
    }

    Mat mat(from->PixelHeight,
        from->PixelWidth,
        CV_8UC4, // assume input SoftwareBitmap is BGRA8
        (void*)pPixels);

    // shallow copy because we want convertedMat.data = pPixels
    // don't use .copyTo or .clone
    convertedMat = mat;
    return true;
}

// resizes the input image to the size of output.
void OpenCVHelper::Resize(SoftwareBitmap^ input, SoftwareBitmap^ output)
{
    Mat inputMat, outputMat;
    if (!(TryConvert(input, inputMat) && TryConvert(output, outputMat)))
    {
        return;
    }
    resize(inputMat, outputMat, cv::Size(output->PixelWidth, output->PixelHeight));
}

// crops the input image based on input size, resizes the cropped image to the size of output.
// posX, X position of the start point for cropping
// posY, Y position of the start point for cropping
// width, width of the cropped image
// height, height of the cropped image
bool OpenCVHelper::CropResize(
    SoftwareBitmap^ input,
    SoftwareBitmap^ output,
    int posX,
    int posY,
    int width,
    int height)
{
    Mat inputMat, outputMat;
    if (!(TryConvert(input, inputMat) && TryConvert(output, outputMat)))
    {
        return false;
    }
    int originalImageWidth = input->PixelWidth;
    int originalImageHeight = input->PixelHeight;
    posX = std::max(0, std::min(posX, originalImageWidth));
    posY = std::max(0, std::min(posY, originalImageHeight));
    if ((posX + width > originalImageWidth) || (posY + height > originalImageHeight))
    {
        return false;
    }
    cv::Rect croppedROI(posX, posY, width, height);
    Mat middleMat = inputMat(croppedROI);
    resize(middleMat, outputMat, cv::Size(output->PixelWidth, output->PixelHeight));
    return true;
}
