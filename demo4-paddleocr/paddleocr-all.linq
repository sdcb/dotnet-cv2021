<Query Kind="Program">
  <NuGetReference>OpenCvSharp4</NuGetReference>
  <NuGetReference>OpenCvSharp4.runtime.win</NuGetReference>
  <Namespace>OpenCvSharp</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>System.Windows.Forms</Namespace>
</Query>

#load ".\paddleocr"
#load ".\paddleocr-detection"
#load ".\paddleocr-recognition"
#load ".\paddleocr-setup"

async Task Main()
{
	await PaddleOcrHelper.SetupAsync(QueryCancelToken);
	using PaddleOcrAll all = new();
	using Mat src = Cv2.ImRead(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "xdr5480.jpg"));

	Image(src).Dump();
	all.Run(src).Text.Dump();

	object Image(Mat src) => Util.Image(src.ToBytes(), Util.ScaleMode.Unscaled);

	byte[] GetClipboardImage()
	{
		using var ms = new MemoryStream();
		Clipboard.GetImage().Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
		return ms.ToArray();
	}
}

public class PaddleOcrAll : IDisposable
{
	public PaddleOcrDetector Detector { get; }
	public PaddleOcrRecognizer Recognizer { get; }

	public PaddleOcrAll(string modelPath = @"C:\_\3rd\paddle\models\ppocr-v2", string labelFilePath = PaddleOcrHelper.PaddleOcrKeys)
	{
		Detector = new(Path.Combine(modelPath, "det"));
		Recognizer = new(Path.Combine(modelPath, "rec"), labelFilePath);
	}

	public PaddleOcrResult Run(Mat src)
	{
		Rect[] rects = Detector.Run(src);
		return new PaddleOcrResult(rects
			.Select(rect =>
			{
				PaddleOcrRecognizerResult result = Recognizer.Run(src[rect]);
				PaddleOcrResultRegion region = new(rect, result.Text, result.Score);
				return region;
			})
			.ToArray());
	}

	public PaddleOcrResult ConcurrentRun(Mat src)
	{
		Rect[] rects = Detector.ConcurrentRun(src);
		return new PaddleOcrResult(rects
			.AsParallel()
			.Select(rect =>
			{
				PaddleOcrRecognizerResult result = Recognizer.ConcurrentRun(src[rect]);
				PaddleOcrResultRegion region = new(rect, result.Text, result.Score);
				return region;
			})
			.ToArray());
	}

	public void Dispose()
	{
		Detector.Dispose();
		Recognizer.Dispose();
	}
}

public record PaddleOcrResult(PaddleOcrResultRegion[] Regions)
{
	public string Text => string.Join("\n", Regions
		.OrderBy(x => x.Rect.Y)
		.ThenBy(x => x.Rect.X)
		.Select(x => x.Text));
}

public record struct PaddleOcrResultRegion(Rect Rect, string Text, float Score);