<Query Kind="Program">
  <NuGetReference>OpenCvSharp4</NuGetReference>
  <NuGetReference>OpenCvSharp4.runtime.win</NuGetReference>
  <Namespace>OpenCvSharp</Namespace>
</Query>

void Main()
{
	Environment.CurrentDirectory = Util.GetPassword("dotnet2021-cv-zhoujie-demo1");
	using Mat src = Cv2.ImRead(@".\resources\135mm-f1.8-GM_MTF_Average-1.png");
	Image(src).Dump();

	Util.HorizontalRun(true, MtfLines.All.Select(l => Image(l.GetBinary(src)))).Dump();

	static object Image(Mat src) => Util.Image(src.ToBytes(), Util.ScaleMode.Unscaled);
}

public record MtfLines(int Line, Scalar Color)
{	
	public static readonly MtfLines L10 = new MtfLines(10, new Scalar(35, 31, 224));
	public static readonly MtfLines L20 = new MtfLines(20, new Scalar(25, 153, 224));
	public static readonly MtfLines L30 = new MtfLines(30, new Scalar(15, 212, 31));
	public static readonly MtfLines L40 = new MtfLines(40, new Scalar(255, 172, 38));
	public static readonly MtfLines L50 = new MtfLines(50, new Scalar(224, 35, 86));

	//public Mat GetBinary(Mat src, int abs = 5)
	//{
	//	using Mat hlsSrc = src.CvtColor(ColorConversionCodes.RGB2HLS);
	//	Vec3b hls = Rgb2Hls(Color);
	//	Scalar from = new Scalar(hls.Item0 - abs, hls.Item1 - 100, 0);
	//	Scalar to = new Scalar(hls.Item0 + abs, hls.Item1 + 100, 256);
	//	return hlsSrc.InRange(from, to);
	//}

	//public Mat GetBinary(Mat src, int abs = 3)
	//{
	//	using Mat hsvSrc = src.CvtColor(ColorConversionCodes.RGB2HSV);
	//	Vec3b hsv = Rgb2Hsv(Color);
	//	Scalar from = new Scalar(hsv.Item0 - abs, 0, 1);
	//	Scalar to = new Scalar(hsv.Item0 + abs, 256, 255);
	//	return hsvSrc.InRange(from, to);
	//}

	public Mat GetBinary(Mat src, int abs = 40)
	{
		Scalar from = new Scalar(Color.Val0 - abs, Color.Val1 - abs, Color.Val2 - abs);
		Scalar to = new Scalar(Color.Val0 + abs, Color.Val1 + abs, Color.Val2 + abs);
		return src.InRange(from, to);
	}

	private static Vec3b Rgb2Hls(Scalar rgb)
	{
		using Mat mat = new(new Size(1, 1), MatType.CV_8UC3, (Scalar)rgb);
		using Mat hsl = mat.CvtColor(ColorConversionCodes.RGB2HLS);
		return hsl.At<Vec3b>(0);
	}

	private static Vec3b Rgb2Hsv(Scalar rgb)
	{
		using Mat mat = new(new Size(1, 1), MatType.CV_8UC3, (Scalar)rgb);
		using Mat hsl = mat.CvtColor(ColorConversionCodes.RGB2HSV);
		return hsl.At<Vec3b>(0);
	}

	public static MtfLines[] All => new[] { L10, L20, L30, L40, L50 };
}