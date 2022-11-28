namespace ZXing.Net.Maui.Readers
{
	public interface IBarcodeReader
	{
		BarcodeReaderOptions Options { get; set; }

		BarcodeResult[] Decode(PixelBufferHolder image, int xOffset = 0, int yOffset = 0, int width = 0, int height = 0);
	}
}
