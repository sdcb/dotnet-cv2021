<Query Kind="Program">
  <NuGetReference>OpenCvSharp4</NuGetReference>
  <NuGetReference>OpenCvSharp4.runtime.win</NuGetReference>
  <Namespace>OpenCvSharp</Namespace>
  <RuntimeVersion>6.0</RuntimeVersion>
</Query>

#load ".\01-table-rect-area"

void Main()
{
	Directory.SetCurrentDirectory(Util.CurrentQuery.Location);

	using Mat src = Cv2.ImRead(@"..\resources\demo1.png");
	//Image(src).Dump("原始图片");
	using Mat gray = src.CvtColor(ColorConversionCodes.RGB2GRAY);
	Rect[] tableRects = GetTableRects(gray);
	DetectTableRowHeight(gray, tableRects[0]).Dump("Table RowHeight");
}

static int DetectTableRowHeight(Mat gray, Rect rect)
{
	using Mat roi = gray[rect.Top, rect.Bottom, rect.Left, rect.Left + 150];
	Image(roi).Dump();
	using Mat binary = roi.Threshold(90, 255, ThresholdTypes.BinaryInv);
	Image(binary).Dump();
	using Mat dilated = new();
	using (Mat ones = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(25, 10)))
	{
		Cv2.Dilate(binary, dilated, ones);
	}
	Image(dilated).Dump();
	Rect[] rects = dilated.FindContoursAsArray(RetrievalModes.List, ContourApproximationModes.ApproxSimple)
		.Select(x => Cv2.BoundingRect(x))
		.Where(x => x.Width > 30)
		.ToArray();
		
		
	double average = rects
		.Zip(rects.Skip(1))
		.Select(x => Math.Abs((x.First.Bottom + x.First.Top) / 2 - (x.Second.Bottom + x.Second.Top) / 2))
		.Average();

	return (int)Math.Round(average);
}
