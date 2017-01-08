#include "pch.h"
#include "FaceDetector.h"
#include <codecvt>
#include <dlib/opencv.h>
#include <opencv2/imgproc.hpp>

using namespace FaceDetection;
using namespace Platform;
using namespace std;
using namespace dlib;

FaceDetector::FaceDetector()
{
}

bool FaceDetector::Initialize(FaceDetectorInitializationData^ initData)
{
    if (initData == nullptr)
    {
        throw ref new InvalidArgumentException();
    }

    try
    {
        mFaceDetector = get_frontal_face_detector();

        // Load face data
        wstring_convert<codecvt_utf8<wchar_t>, wchar_t> cv;
        string faceData = cv.to_bytes(initData->FaceData->Data());
        deserialize(faceData) >> mShapePredictor;

        mIsInitialized = true;
    }
    catch (exception& e)
    {
        OutputDebugStringA("Initialization failed.");
        OutputDebugStringA(e.what());
    }
}

FaceDetectResult^ FaceDetector::Detect(const Platform::Array<byte>^ bgraArray, int width, int height)
{
    if (bgraArray == nullptr || width <= 0 || height <= 0)
    {
        throw ref new InvalidArgumentException();
    }

    if (!mIsInitialized)
    {
        return nullptr;
    }

    auto result = ref new FaceDetectResult();

    try
    {
        // Convert BGRA->RGB fromat
        cv::Mat bgraMat(height, width, CV_8UC4, bgraArray->Data);
        cv::Mat rgbMat(height, width, CV_8UC3);
        cv::cvtColor(bgraMat, rgbMat, CV_BGRA2RGB);

        // Convert OpenCV->dlib format
        cv_image<rgb_pixel> cvImage(rgbMat);

        // Detect face
        std::vector<rectangle> faces = mFaceDetector(cvImage);

        std::vector<full_object_detection> shapes;
        for (unsigned long i = 0; i < faces.size(); ++i)
        {
            shapes.push_back(mShapePredictor(cvImage, faces[i]));
        }

        // Pack result
        for (auto shape : shapes)
        {
            auto faceData = ref new FaceData();

            auto rect = shape.get_rect();
            faceData->FaceRect = Rect(rect.left(), rect.top(), rect.width(), rect.height());

            for (unsigned long i = 0; i < shape.num_parts(); ++i)
            {
                auto point = shape.part(i);
                faceData->FacePoints->Append(Point(point.x(), point.y()));
            }

            result->Faces->Append(faceData);
        }
    }
    catch (exception& e)
    {
        OutputDebugStringA("Face detection failed.");
        OutputDebugStringA(e.what());
    }

    return result;
}