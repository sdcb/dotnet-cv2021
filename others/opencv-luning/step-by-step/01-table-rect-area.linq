<Query Kind="Program">
  <NuGetReference>OpenCvSharp4</NuGetReference>
  <NuGetReference>OpenCvSharp4.runtime.win</NuGetReference>
  <Namespace>OpenCvSharp</Namespace>
  <RuntimeVersion>6.0</RuntimeVersion>
</Query>

void Main()
{
	Directory.SetCurrentDirectory(Util.CurrentQuery.Location);
	
	using Mat src = Cv2.ImRead(@"..\resources\demo1.png");
	//Image(src).Dump("原始图片");
	using Mat gray = src.CvtColor(ColorConversionCodes.RGB2GRAY);
	Rect[] tableRects = GetTableRects(gray);
}

static Rect[] GetTableRects(Mat gray)
{
	using Mat res = gray.Threshold(220, 255, ThresholdTypes.BinaryInv);
	//Image(res).Dump("二值化");
	using Mat dilated = new();
	using (Mat ones = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(5, 1)))
	{
		Cv2.Dilate(res, dilated, ones);
	}
	//Image(dilated).Dump("膨胀");

	Point[][] contours = dilated.FindContoursAsArray(RetrievalModes.List, ContourApproximationModes.ApproxSimple);
	Rect[] tableRects = contours.Select(x => Cv2.BoundingRect(x))
		.Where(x => x.Width > 400 && x.Height > 100)
		.ToArray();
	Util.HorizontalRun(false, DrawRects(gray, contours.Select(x => Cv2.BoundingRect(x))), DrawRects(gray, tableRects)).Dump();
	
	return tableRects;
}

static object Image(Mat mat) => Util.Image(mat.ToBytes(), Util.ScaleMode.Unscaled);
static object DrawRects(Mat mat, IEnumerable<Rect> rects)
{
	using Mat demo = mat.CvtColor(ColorConversionCodes.GRAY2RGB);
	foreach (Rect rect in rects)
	{
		Cv2.Rectangle(demo, rect, Scalar.Red, thickness: 5);
	}
	return Image(demo);
}