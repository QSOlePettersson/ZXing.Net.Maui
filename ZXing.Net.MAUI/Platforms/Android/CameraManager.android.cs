using Android.Graphics;
using AndroidX.Camera.Core;
using AndroidX.Camera.Lifecycle;
using AndroidX.Camera.View;
using AndroidX.Core.Content;
using Java.Util.Concurrent;
using AView = Android.Views.View;

namespace ZXing.Net.Maui
{
	internal partial class CameraManager
	{
		AndroidX.Camera.Core.Preview cameraPreview;
		ImageAnalysis imageAnalyzer;
		PreviewView previewView;
		IExecutorService cameraExecutor;
		CameraSelector cameraSelector = null;
		ProcessCameraProvider cameraProvider;
		ICamera camera;

		public NativePlatformCameraPreviewView CreateNativeView()
		{
			previewView = new PreviewView(Context.Context);
			cameraExecutor = Executors.NewSingleThreadExecutor();

			return previewView;
		}

		public void Connect()
		{
			var cameraProviderFuture = ProcessCameraProvider.GetInstance(Context.Context);

			cameraProviderFuture.AddListener(new Java.Lang.Runnable(() =>
			{
				// Used to bind the lifecycle of cameras to the lifecycle owner
				cameraProvider = (ProcessCameraProvider)cameraProviderFuture.Get();
                // Preview
                cameraPreview = new AndroidX.Camera.Core.Preview.Builder().Build();
                cameraPreview.SetSurfaceProvider(previewView.SurfaceProvider);

                //var resolution = cameraPreview.ResolutionInfo is { } resolutionInfo ? resolutionInfo.Resolution : new Android.Util.Size(1024, 1024);
                //// Frame by frame analyze
                //imageAnalyzer = new ImageAnalysis.Builder()
                //    //.SetDefaultResolution(resolution)
                //    .SetBackpressureStrategy(ImageAnalysis.StrategyKeepOnlyLatest)
                //    .Build();
                //imageAnalyzer.SetAnalyzer(cameraExecutor, new FrameAnalyzer((buffer, size) =>
                //    FrameReady?.Invoke(this, new CameraFrameBufferEventArgs(new Readers.PixelBufferHolder { Data = buffer, Size = size }))));

				UpdateCamera();

			}), ContextCompat.GetMainExecutor(Context.Context)); //GetMainExecutor: returns an Executor that runs on the main thread.
		}

		public void Disconnect()
		{ }

		public void UpdateCamera()
		{
			if (cameraProvider != null)
			{
				// Unbind use cases before rebinding
				cameraProvider.UnbindAll();

				var cameraLocation = CameraLocation;

				// Select back camera as a default, or front camera otherwise
				if (cameraLocation == CameraLocation.Rear && cameraProvider.HasCamera(CameraSelector.DefaultBackCamera))
					cameraSelector = CameraSelector.DefaultBackCamera;
				else if (cameraLocation == CameraLocation.Front && cameraProvider.HasCamera(CameraSelector.DefaultFrontCamera))
					cameraSelector = CameraSelector.DefaultFrontCamera;
				else
					cameraSelector = CameraSelector.DefaultBackCamera;

				if (cameraSelector == null)
					throw new System.Exception("Camera not found");

				// The Context here SHOULD be something that's a lifecycle owner
                if (Context.Context is not AndroidX.Lifecycle.ILifecycleOwner lifecycleOwner)
                {
                    // if not, this should be sufficient as a fallback
                    lifecycleOwner =
                        Microsoft.Maui.ApplicationModel.Platform.CurrentActivity as AndroidX.Lifecycle.ILifecycleOwner;
                }

                if (lifecycleOwner is null) return;

                camera = cameraProvider.BindToLifecycle(lifecycleOwner, cameraSelector, cameraPreview);

                var resolution = cameraPreview.ResolutionInfo is { } resolutionInfo ? resolutionInfo.Resolution : new Android.Util.Size(1024, 1024);
                // Frame by frame analyze
                imageAnalyzer = new ImageAnalysis.Builder()
                    .SetDefaultResolution(resolution)
                    .SetBackpressureStrategy(ImageAnalysis.StrategyKeepOnlyLatest)
                    .Build();
                imageAnalyzer.SetAnalyzer(cameraExecutor, new FrameAnalyzer((buffer, size) =>
                    FrameReady?.Invoke(this, new CameraFrameBufferEventArgs(new Readers.PixelBufferHolder { Data = buffer, Size = size }))));

                cameraProvider.BindToLifecycle(lifecycleOwner, cameraSelector, imageAnalyzer);
            }
		}

		public void UpdateTorch(bool on)
		{
			camera?.CameraControl?.EnableTorch(on);
		}

		public void Focus(Point point)
		{

		}

		public void AutoFocus()
		{

		}

		public void Dispose()
		{
			cameraExecutor?.Shutdown();
			cameraExecutor?.Dispose();
		}
	}
}
