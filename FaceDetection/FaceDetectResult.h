#pragma once

using namespace Windows::Foundation;
using namespace Windows::Foundation::Collections;
using namespace Platform::Collections;

namespace FaceDetection
{
    public ref class FaceData sealed
    {
    private:
        Rect mFace = Rect::Empty;
        Vector<Point>^ mFacePoints = ref new Vector<Point>();

    public:
        property Rect FaceRect
        {
            Rect get() { return mFace; }
            void set(Rect value) { mFace = value; }
        }

        property IVector<Point>^ FacePoints
        {
            IVector<Point>^ get() { return mFacePoints; }
        }
    };

    public ref class FaceDetectResult sealed
    {
    private:
        Vector<FaceData^>^ mFaces = ref new Vector<FaceData^>();

    public:
        property  IVector<FaceData^>^ Faces
        {
            IVector<FaceData^>^ get() { return mFaces; }
        }
    };
}