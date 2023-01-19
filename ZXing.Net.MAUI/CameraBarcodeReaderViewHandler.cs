using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Point = Microsoft.Maui.Graphics.Point;

namespace ZXing.Net.Maui
{
	public partial class CameraBarcodeReaderViewHandler : ViewHandler<ICameraBarcodeReaderView, NativePlatformCameraPreviewView>
	{
		public static PropertyMapper<ICameraBarcodeReaderView, CameraBarcodeReaderViewHandler> CameraBarcodeReaderViewMapper = new()
		{
			[nameof(ICameraBarcodeReaderView.Options)] = MapOptions,
			[nameof(ICameraBarcodeReaderView.IsDetecting)] = MapIsDetecting,
			[nameof(ICameraBarcodeReaderView.IsTorchOn)] = (handler, virtualView) => handler.cameraManager.UpdateTorch(virtualView.IsTorchOn),
			[nameof(ICameraBarcodeReaderView.CameraLocation)] = (handler, virtualView) => handler.cameraManager.UpdateCameraLocation(virtualView.CameraLocation)
		};

		public static CommandMapper<ICameraBarcodeReaderView, CameraBarcodeReaderViewHandler> CameraBarcodeReaderCommandMapper = new()
		{
			[nameof(ICameraBarcodeReaderView.Focus)] = MapFocus,
			[nameof(ICameraBarcodeReaderView.AutoFocus)] = MapAutoFocus,
		};

		public CameraBarcodeReaderViewHandler() : base(CameraBarcodeReaderViewMapper)
		{
		}

		public CameraBarcodeReaderViewHandler(PropertyMapper mapper = null) : base(mapper ?? CameraBarcodeReaderViewMapper)
		{
		}

		CameraManager cameraManager;

		Readers.IBarcodeReader barcodeReader;

		protected Readers.IBarcodeReader BarcodeReader
			=> barcodeReader ??= Services.GetService<Readers.IBarcodeReader>();

		protected override NativePlatformCameraPreviewView CreatePlatformView()
		{
			if (cameraManager == null)
				cameraManager = new(MauiContext, VirtualView?.CameraLocation ?? CameraLocation.Rear);
			var v = cameraManager.CreateNativeView();
			return v;
		}

		protected override async void ConnectHandler(NativePlatformCameraPreviewView nativeView)
		{
			base.ConnectHandler(nativeView);

			if (await cameraManager.CheckPermissions())
				cameraManager.Connect();

			cameraManager.FrameReady += CameraManager_FrameReady;
		}

		protected override void DisconnectHandler(NativePlatformCameraPreviewView nativeView)
		{
			cameraManager.FrameReady -= CameraManager_FrameReady;

			cameraManager.Disconnect();

			base.DisconnectHandler(nativeView);
		}

		private void CameraManager_FrameReady(object sender, CameraFrameBufferEventArgs e)
		{
			VirtualView?.FrameReady(e);

			if (VirtualView.IsDetecting)
			{
				var barcodes = BarcodeReader.Decode(e.Data, (int)e.CropRectangle.Left, (int)e.CropRectangle.Top, (int)e.CropRectangle.Width, (int)e.CropRectangle.Height);
				if (barcodes?.Any() ?? false)
					VirtualView?.BarcodesDetected(new BarcodeDetectionEventArgs(barcodes));
			}
		}

		public static void MapOptions(CameraBarcodeReaderViewHandler handler, ICameraBarcodeReaderView cameraBarcodeReaderView)
			=> handler.BarcodeReader.Options = cameraBarcodeReaderView.Options;

        public static void MapIsDetecting(CameraBarcodeReaderViewHandler handler, ICameraBarcodeReaderView cameraBarcodeReaderView)
        {
			if (!cameraBarcodeReaderView.IsDetecting) return;
            if ((cameraBarcodeReaderView as View)?.Dispatcher is {} dispatcher && dispatcher.IsDispatchRequired)
            {
                dispatcher.Dispatch(handler.cameraManager.UpdateCamera);
				return;
            }
			handler.cameraManager.UpdateCamera();
        }

		public void Focus(Point point)
			=> cameraManager?.Focus(point);

		public void AutoFocus()
			=> cameraManager?.AutoFocus();

		public static void MapFocus(CameraBarcodeReaderViewHandler handler, ICameraBarcodeReaderView cameraBarcodeReaderView, object? parameter)
		{
			if (parameter is not Point point)
				throw new ArgumentException("Invalid parameter", "point");

			handler.Focus(point);
		}

		public static void MapAutoFocus(CameraBarcodeReaderViewHandler handler, ICameraBarcodeReaderView cameraBarcodeReaderView, object? parameters)
			=> handler.AutoFocus();
	}
}
