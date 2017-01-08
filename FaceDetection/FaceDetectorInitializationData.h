#pragma once

namespace FaceDetection
{
    public ref class FaceDetectorInitializationData sealed
    {
    private:
        Platform::String^ mFaceData;

    public:
        property Platform::String^ FaceData
        {
            Platform::String^ get() { return mFaceData; }
            void set(Platform::String^ value) { mFaceData = value; }
        };
    };
}