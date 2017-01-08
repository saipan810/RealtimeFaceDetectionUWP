#pragma once

#include "FaceDetectResult.h"
#include "FaceDetectorInitializationData.h"
#include <dlib/image_processing/frontal_face_detector.h>
#include <dlib/image_processing.h>

using namespace Windows::Graphics::Imaging;

namespace FaceDetection
{
    public ref class FaceDetector sealed
    {
    public:
        FaceDetector();

    private:
        bool mIsInitialized = false;
        dlib::frontal_face_detector mFaceDetector;
        dlib::shape_predictor mShapePredictor;

    public:
        bool Initialize(FaceDetectorInitializationData^ initData);
        FaceDetectResult^ Detect(const Platform::Array<byte>^ bgraArray, int width, int height);
    };
}
