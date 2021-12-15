<Query Kind="Program">
  <NuGetReference>OpenCvSharp4</NuGetReference>
  <NuGetReference>OpenCvSharp4.runtime.win</NuGetReference>
  <Namespace>OpenCvSharp</Namespace>
</Query>

#load ".\01_perspective-correct"

void Main()
{
	Environment.CurrentDirectory = Util.GetPassword("dotnet2021-cv-zhoujie-demo5");
	using Mat src = Cv2.ImRead(@".\resources\demo.webp");
	using Mat corrected = PerspectiveCorrect(src);
	using Mat correctedAgain = PerspectiveCorrectAgain(corrected);
	Util.HorizontalRun(false, Run(corrected), Run(correctedAgain)).Dump();

}

object Run(Mat src)
{
	using Mat gray = src.CvtColor(ColorConversionCodes.RGB2GRAY);
	using Mat binary = gray.Threshold(127, 255, ThresholdTypes.BinaryInv);
	CellSpan[] rows = GetRowSpans(binary);
	CellSpan[] cols = GetControlCols(binary, rows).ToArray();

	using Mat demo = src.Clone();
	for (int ri = 0; ri < rows.Length; ++ri)
	{
		if (ri == 12 || ri == 50) continue;
		CellSpan row = rows[ri];
		for (int ci = 0; ci < cols.Length; ++ci)
		{
			CellSpan col = cols[ci];

			Rect rect = new(col.Start, row.Start, col.Length, row.Length);
			using Mat roi = binary[rect];
			double val = 1.0 * roi.Sum().Val0 / 255 / rect.Width / rect.Height;
			if (val > 0.5)
			{
				//new { ri, ci, val }.Dump();
				//corrected.Rectangle(rect, Scalar.Blue);
				demo.Rectangle(rect, Scalar.Red, thickness: 2);
			}
		}
	}
	return Image(demo);
}

IEnumerable<CellSpan> GetControlCols(Mat binary, CellSpan[] rows)
{
	CellSpan[] cols1 = GetControlCols(binary, rows[12]);
	CellSpan[] cols2 = GetControlCols(binary, rows[50]);
	CellSpan left = new CellSpan(
		(cols1[0].Start + cols2[0].Start) / 2,
		(cols1[0].End + cols2[0].End) / 2);
	CellSpan right = new CellSpan(
		(cols1[2].Start + cols2[2].Start) / 2,
		(cols1[2].End + cols2[2].End) / 2);
	yield return left;

	double distance = right.Start - left.Start;
	double width = (left.Length + right.Length) / 2;

	int count = 21;
	for (int i = 1; i <= count; ++i)
	{
		int start = (int)(left.Start + i * distance / (count + 1));
		yield return new CellSpan(start, (int)(start + width));
	}
	yield return right;
}

Mat PerspectiveCorrectAgain(Mat src)
{
	using Mat gray = src.CvtColor(ColorConversionCodes.RGB2GRAY);
	using Mat binary = gray.Threshold(127, 255, ThresholdTypes.BinaryInv);
	CellSpan[] rows = GetRowSpans(binary);
	(Rect left, Rect right) GetControlPoint(Mat binary, CellSpan row)
	{
		CellSpan[] cols = GetControlCols(binary, row);
		Rect GetControlRect(int i)
		{
			Rect leftRect = Rect.FromLTRB(cols[i].Start - 10, row.Start - 10, cols[i].End + 10, row.End + 10);
			using Mat roi = binary[leftRect];
			Rect controlRect = roi.FindContoursAsArray(RetrievalModes.External, ContourApproximationModes.ApproxNone)
				.Select(Cv2.BoundingRect)
				.Where(x => x.Width > 10)
				.First();
			return controlRect + leftRect.TopLeft;
		}
		
		Rect left = GetControlRect(0);
		Rect right = GetControlRect(2);

		return (left, right);
	}
	
	(Rect topLeft, Rect topRight) = GetControlPoint(binary, rows[12]);
	(Rect bottomLeft, Rect bottomRight) = GetControlPoint(binary, rows[50]);
	//int width = (topLeft.Width + bottomLeft.Width + topRight.Width + bottomRight.Width) / 4;
	Point supposedTopLeft = new (topLeft.TopLeft.X, topRight.TopLeft.Y);
	Point supposedBottomLeft = new (topLeft.TopLeft.X, bottomRight.TopLeft.Y);

	Point[] originRect = new[] { topLeft.TopLeft, topRight.TopLeft, bottomRight.TopLeft, bottomLeft.TopLeft };
	Point[] destRect = new[] { supposedTopLeft, topRight.TopLeft, bottomRight.TopLeft, supposedBottomLeft };
	using Mat transform = Cv2.GetPerspectiveTransform(
		originRect.Select(x => (Point2f)x), 
		destRect.Select(x => (Point2f)x));
		
	//using Mat demo = src.Clone();
	//demo.DrawContours(new []{ originRect }, -1, Scalar.Red);
	//Image(demo).Dump();
	return src.WarpPerspective(transform, binary.Size(), InterpolationFlags.Area);
}

CellSpan[] GetControlCols(Mat binary, CellSpan controlRow)
{
	Size size = binary.Size();
	using Mat roi = binary[controlRow.Start, controlRow.End, 0, size.Width - 45];
	double[] colSums;
	{
		using Mat reduced = roi.Reduce(ReduceDimension.Row, ReduceTypes.Sum, MatType.CV_64F);
		using Mat normalized = reduced.Normalize(normType: NormTypes.INF);
		normalized.GetArray(out colSums);
	}
	return CellSpan.Scan(colSums)
		.Where(x => x.Length > 10)
		.ToArray();
}

CellSpan[] GetRowSpans(Mat binary)
{
	Size size = binary.Size();
	using Mat roi = binary[0, size.Height, size.Width - 45, size.Width - 10];
	double[] rowSums;
	{
		using Mat reduced = roi.Reduce(ReduceDimension.Column, ReduceTypes.Sum, MatType.CV_64F);
		using Mat normalized = reduced.Normalize(normType: NormTypes.INF);
		normalized.GetArray(out rowSums);
	}

	return CellSpan.Scan(rowSums).ToArray();
}

public record CellSpan(int Start, int End)
{
	public int Length => End - Start;
	public int Center => (Start + End) / 2;
	public bool Contains(int v) => Start <= v && v <= End;
	public static CellSpan operator +(CellSpan l, int d) => new CellSpan(l.Start + d, l.End + d);
	public static CellSpan operator -(CellSpan l, int d) => new CellSpan(l.Start - d, l.End - d);
	public static IEnumerable<CellSpan> Scan(double[] data, double threshold = 0.1)
	{
		int trackStart = -1;
		for (int i = 0; i < data.Length; ++i)
		{
			if (trackStart == -1 && data[i] > threshold)
			{
				trackStart = i;
			}
			else if (trackStart != -1 && data[i] <= threshold)
			{
				yield return new CellSpan(trackStart, i);
				trackStart = -1;
			}
		}
		if (trackStart != -1)
		{
			yield return new CellSpan(trackStart, data.Length - 1);
		}
	}
}