using System;
using System.Drawing;
using ZXing.Net.Maui.Readers;

namespace ZXing.Net.Maui
{
	public class CameraFrameBufferEventArgs : EventArgs
	{
		public CameraFrameBufferEventArgs(PixelBufferHolder pixelBufferHolder) : base()
			=> Data = pixelBufferHolder;

		public readonly PixelBufferHolder Data;

		public RectangleF CropRectangle { get; set; } = RectangleF.Empty;
	}
}
