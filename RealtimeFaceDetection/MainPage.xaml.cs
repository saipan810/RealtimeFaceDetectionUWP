using FaceDetection;
using RealtimeFaceDetection.FaceDrawer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace RealtimeFaceDetection
{
    public sealed partial class MainPage : Page
    {
        private const Windows.Devices.Enumeration.Panel DeviceLocation = Windows.Devices.Enumeration.Panel.Front;
        private readonly Size PreviewResolution = new Size(640, 480);

        private const string FacePredictorFileName = "shape_predictor_68_face_landmarks.dat";

        public readonly List<IFaceDrawer> FaceDrawers = new List<IFaceDrawer>()
        {
            new BasicFaceDrawer(),
            new FaceLineDrawer(),
            new BlackEyeLineDrawer(),
        };

        private MediaCapture _capture = null;
        private bool _isPreviewing = false;
        private CancellationTokenSource _faceDetectionCancellationTokenSource = null;
        private Task _faceDetectionTask = null;
        private IFaceDrawer _faceDrawer = null;

        public MainPage()
        {
            InitializeComponent();

            _faceDrawer = FaceDrawers.FirstOrDefault();
            DrawSelector.ItemsSource = FaceDrawers;
            DrawSelector.SelectedItem = FaceDrawers.FirstOrDefault();

            NavigationCacheMode = NavigationCacheMode.Disabled;

            Application.Current.Suspending += Current_Suspending;
            Application.Current.Resuming += Current_Resuming;
        }

        #region Event

        protected override async void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            SystemMediaTransportControls.GetForCurrentView().PropertyChanged -= MainPage_PropertyChanged;

            await FinalizeMediaCaptureAsync();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            await InitializeMediaCaptureAsync();
            SystemMediaTransportControls.GetForCurrentView().PropertyChanged += MainPage_PropertyChanged;

            base.OnNavigatedTo(e);
        }

        private async void Current_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            if (Frame.CurrentSourcePageType == typeof(MainPage))
            {
                var deferral = e.SuspendingOperation.GetDeferral();

                await FinalizeMediaCaptureAsync();
                SystemMediaTransportControls.GetForCurrentView().PropertyChanged -= MainPage_PropertyChanged;

                deferral.Complete();
            }
        }

        private async void Current_Resuming(object sender, object e)
        {
            await InitializeMediaCaptureAsync();
            SystemMediaTransportControls.GetForCurrentView().PropertyChanged += MainPage_PropertyChanged;
        }

        private async void MainPage_PropertyChanged(SystemMediaTransportControls sender, SystemMediaTransportControlsPropertyChangedEventArgs args)
        {
            if (args.Property != SystemMediaTransportControlsProperty.SoundLevel)
            {
                return;
            }

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                if (sender.SoundLevel == SoundLevel.Muted)
                {
                    await FinalizeMediaCaptureAsync();
                }
                else
                {
                    await InitializeMediaCaptureAsync();
                }
            });
        }

        private void DrawSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _faceDrawer = DrawSelector.SelectedItem as IFaceDrawer;
        }

        #endregion Event

        #region Initialize/Finalize MediaCapture and Start/Stop Preview

        private async Task InitializeMediaCaptureAsync()
        {
            if (_capture != null)
            {
                return;
            }

            try
            {
                _capture = new MediaCapture();

                var device = await GetCameraDeviceInformationAsync(DeviceLocation);

                await _capture.InitializeAsync(new MediaCaptureInitializationSettings()
                {
                    VideoDeviceId = device?.Id,
                    StreamingCaptureMode = StreamingCaptureMode.Video
                });

                await StartPreviewAsync(_capture);

                StartFaceDetection(_capture);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("MediaCapture initialization failed.");
                Debug.WriteLine(ex.Message);
            }
        }

        private async Task<DeviceInformation> GetCameraDeviceInformationAsync(Windows.Devices.Enumeration.Panel panel)
        {
            var allVideoDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            var desiredDevices = allVideoDevices.FirstOrDefault(
                x => x.IsEnabled && x.EnclosureLocation != null && x.EnclosureLocation.Panel == panel);

            return desiredDevices;
        }

        private async Task FinalizeMediaCaptureAsync()
        {
            if (_capture == null)
            {
                return;
            }

            try
            {
                await StopFaceDetectionAsync();

                await StopoPreviewAsync(_capture);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("MediaCapture finalization failed.");
                Debug.WriteLine(ex.Message);
            }
            finally
            {
                Media.Source = null;

                _capture.Dispose();
                _capture = null;
            }
        }

        private async Task StartPreviewAsync(MediaCapture capture)
        {
            if (_isPreviewing)
            {
                return;
            }

            if (capture == null)
            {
                return;
            }

            var props = capture.VideoDeviceController.GetAvailableMediaStreamProperties(MediaStreamType.VideoPreview);
            var filtered = props.OfType<VideoEncodingProperties>()
                .Where(p => p.Width == PreviewResolution.Width && p.Height == PreviewResolution.Height);

            await capture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.VideoPreview, filtered.FirstOrDefault());
            await capture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.Photo, filtered.FirstOrDefault());
            await capture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.VideoRecord, filtered.FirstOrDefault());

            Media.Source = _capture;
            await capture.StartPreviewAsync();

            _isPreviewing = true;

            Debug.WriteLine("Preview started.");
            Debug.WriteLineIf(!filtered.Any(), $"Camera do not support {PreviewResolution.Width} * {PreviewResolution.Height}.");
        }

        private async Task StopoPreviewAsync(MediaCapture capture)
        {
            if (!_isPreviewing)
            {
                return;
            }

            if (capture != null)
            {
                await capture.StopPreviewAsync();
            }

            _isPreviewing = false;

            Debug.WriteLine("Preview stopped.");
        }

        #endregion Initialize/Finalize MediaCapture and Start/Stop Preview

        #region Start/Stop face detection

        private void StartFaceDetection(MediaCapture capture)
        {
            if (capture == null)
            {
                throw new ArgumentNullException();
            }

            if (_faceDetectionCancellationTokenSource != null)
            {
                return;
            }

            _faceDetectionCancellationTokenSource = new CancellationTokenSource();
            var token = _faceDetectionCancellationTokenSource.Token;

            _faceDetectionTask = Task.Factory.StartNew(async () =>
            {
                try
                {
                    FaceDetector detector = new FaceDetector();
                    detector.Initialize(new FaceDetectorInitializationData()
                    {
                        FaceData = Package.Current.InstalledLocation.Path + "\\" + FacePredictorFileName
                    });

                    while (!token.IsCancellationRequested)
                    {
                        await FaceDetectAsync(detector, capture, token);
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception e)
                {
                    Debug.WriteLine("Face detection failed.");
                    Debug.WriteLine(e.Message);
                }
            }, token);
        }

        private async Task StopFaceDetectionAsync()
        {
            if (_faceDetectionCancellationTokenSource == null)
            {
                return;
            }

            _faceDetectionCancellationTokenSource.Cancel();
            _faceDetectionCancellationTokenSource.Dispose();
            _faceDetectionCancellationTokenSource = null;

            _faceDetectionTask.Wait();

            await Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
            {
                FaceDrawCanvas.Children.Clear();
            });
        }

        #endregion Start/Stop face detection

        #region Detect Face

        private async Task FaceDetectAsync(FaceDetector detector, MediaCapture capture, CancellationToken token)
        {
            if (detector == null || capture == null || token == null)
            {
                throw new ArgumentNullException();
            }

            var previewProperties = capture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview) as VideoEncodingProperties;
            var videoFrame = new VideoFrame(BitmapPixelFormat.Bgra8, (int)previewProperties.Width, (int)previewProperties.Height);

            int width = (int)previewProperties.Width;
            int height = (int)previewProperties.Height;

            FaceDetectResult result = null;

            var stopWatch = Stopwatch.StartNew();
            {
                using (var currentFrame = await capture.GetPreviewFrameAsync(videoFrame))
                using (var softwareBitmap = currentFrame.SoftwareBitmap)
                {
                    if (softwareBitmap == null)
                    {
                        return;
                    }

                    // SoftwareBitmap -> byte array
                    var buffer = new byte[4 * width * height];
                    softwareBitmap.CopyToBuffer(buffer.AsBuffer());

                    token.ThrowIfCancellationRequested();

                    // Detect face
                    result = detector.Detect(buffer, width, height);

                    token.ThrowIfCancellationRequested();
                }
            }
            stopWatch.Stop();

            videoFrame.Dispose();

            // Draw result to Canvas
            await Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
            {
                FaceDrawCanvas.Width = width;
                FaceDrawCanvas.Height = height;

                // Draw fps
                FpsTextBlock.Text = (1000 / stopWatch.ElapsedMilliseconds) + "fps";

                // Draw face point
                if (_faceDrawer != null && result != null)
                {
                    List<FaceDetectData> faces = new List<FaceDetectData>();
                    foreach (var f in result.Faces)
                    {
                        FaceDetectData data = new FaceDetectData();
                        data.FaceRect = f.FaceRect;

                        foreach (var p in f.FacePoints)
                        {
                            data.FaceLandmarks.Add(p);
                        }

                        faces.Add(data);
                    }

                    _faceDrawer.DrawToCanvas(FaceDrawCanvas, faces);
                }
            });
        }

        #endregion Detect Face
    }
}