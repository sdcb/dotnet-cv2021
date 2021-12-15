<Query Kind="Program">
  <NuGetReference>OpenCvSharp4</NuGetReference>
  <NuGetReference>OpenCvSharp4.runtime.win</NuGetReference>
  <Namespace>OpenCvSharp</Namespace>
</Query>

void Main()
{
	Environment.CurrentDirectory = Util.GetPassword("dotnet2021-cv-zhoujie-demo5");
	using Mat src = Cv2.ImRead(@".\resources\demo.webp");
	using Mat corrected = PerspectiveCorrect(src, debug: true);
}

Mat PerspectiveCorrect(Mat src, bool debug = false)
{
	Point[] pentagon = GetPentagon(src, debug);
	Point[] quadrangle = ToQuadrangle(pentagon);
	Mat corrected = PerspectiveCorrect(src, quadrangle);
	
	if (debug)
	{
		using Mat pentagonDemo = src.Clone();
		pentagonDemo.DrawContours(new[] { pentagon }, -1, Scalar.Red, thickness: 10);

		using Mat polygonDemo = src.Clone();
		polygonDemo.DrawContours(new[] { quadrangle }, -1, Scalar.Red, thickness: 10);

		Util.HorizontalRun(false, ImageSmall(pentagonDemo), ImageSmall(polygonDemo), Image(corrected)).Dump();
	}
	return corrected;
}

Mat PerspectiveCorrect(Mat src, Point[] quadrangle)
{
	Point topLeft = quadrangle.MinBy(v => v.DistanceTo(new Point()));
	Point topRight = quadrangle.MinBy(v => v.DistanceTo(new Point(src.Width, 0)));
	Point bottomRight = quadrangle.MinBy(v => v.DistanceTo(new Point(src.Width, src.Height)));
	Point bottomLeft = quadrangle.MinBy(v => v.DistanceTo(new Point(0, src.Height)));
	var newSize = new Size(660, 950);
	using Mat perspectiveTransform = Cv2.GetPerspectiveTransform(
		new[] { topLeft, topRight, bottomRight, bottomLeft }.Select(v => new Point2f(v.X, v.Y)),
		new[] { new Point2f(0, 0), new Point2f(newSize.Width, 0), new Point2f(newSize.Width, newSize.Height), new Point2f(0, newSize.Height) });
	return src.WarpPerspective(perspectiveTransform, newSize, borderValue: Scalar.White);
}

Point[] ToQuadrangle(Point[] pentagon)
{
	List<LineSegmentPoint> lines = pentagon
		.Zip(pentagon.Skip(1))
		.Select(x => new LineSegmentPoint(x.First, x.Second))
		.ToList();
	lines.Add(new LineSegmentPoint(pentagon[pentagon.Length - 1], pentagon[0]));
	LineSegmentPoint minLine = lines.MinBy(v => v.Length());
	
	var result = new List<Point>(capacity: pentagon.Length - 1);
	foreach (Point point in pentagon)
	{
		if (point == minLine.P1)
		{
			LineSegmentPoint p1Line = lines.Single(l => l != minLine && (l.P1 == minLine.P1 || l.P2 == minLine.P1));
			LineSegmentPoint p2Line = lines.Single(l => l != minLine && (l.P1 == minLine.P2 || l.P2 == minLine.P2));
			Point p = LineSegmentPoint.LineIntersection(p1Line, p2Line).Value;
			result.Add(p);
		}
		else if (point != minLine.P2)
		{
			result.Add(point);
		}
	}
	return result.ToArray();
}

Point[] GetPentagon(Mat src, bool debug = false)
{
	using Mat gray = src.CvtColor(ColorConversionCodes.RGB2GRAY);
	using Mat binary = gray.Threshold(127, 255, ThresholdTypes.Binary);
	using Mat blur = binary.GaussianBlur(new Size(3, 3), 2, 2);
	using Mat canny = blur.Canny(60, 240, 3);
	using Mat dilated = new();
	using (Mat ones = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3)))
	{
		Cv2.Dilate(canny, dilated, ones);
	}

	Point[][] contours = dilated.FindContoursAsArray(RetrievalModes.External, ContourApproximationModes.ApproxNone);
	Point[] maxContour = contours
		.OrderByDescending(x => Cv2.ContourArea(x))
		.First();
	Point[] hull = Cv2.ConvexHull(maxContour);
	double epsilon = 0.01 * Cv2.ArcLength(maxContour, closed: true);
	Point[] approx = Cv2.ApproxPolyDP(hull, epsilon, closed: true);

	if (debug)
	{
		using Mat maxContourDemo = src.EmptyClone();
		maxContourDemo.DrawContours(new[] { maxContour }, -1, Scalar.Red, thickness: 10);

		using Mat pentagonDemo = src.Clone();
		pentagonDemo.DrawContours(new[] { approx }, -1, Scalar.Red, thickness: 10);

		Util.HorizontalRun(true, ImageSmall(dilated), ImageSmall(maxContourDemo), ImageSmall(pentagonDemo)).Dump();
	}

	return approx;
}

static object Image(Mat src, double scale = 1)
{
	using Mat scaled = src.Resize(Size.Zero, scale, scale, InterpolationFlags.Area);
	return Util.Image(scaled.ToBytes(), Util.ScaleMode.Unscaled);
}
static object ImageSmall(Mat src) => Image(src, scale: 0.2);