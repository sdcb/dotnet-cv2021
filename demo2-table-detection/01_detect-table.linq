<Query Kind="Program">
  <NuGetReference>OpenCvSharp4</NuGetReference>
  <NuGetReference>OpenCvSharp4.runtime.win</NuGetReference>
  <Namespace>OpenCvSharp</Namespace>
  <Namespace>System.Net</Namespace>
</Query>

void Main()
{
	Environment.CurrentDirectory = Util.GetPassword("dotnet2021-cv-zhoujie-demo2");
	using Mat src = Cv2.ImRead(@".\resources\sharpness-2021-12-03.jpg");
	//var matTable = GetMatTable(src, debug: false);
	//Util.HorizontalRun(false, Image(src), Image(matTable)).Dump();
	var matTable = GetMatTable2(src, debug: false);
	matTable.Select(x => Visualize(x)).Dump();
}

object Visualize(MatRow r)
{
	object[] columnMats = r.ColumnMats.Select(x => Image(x)).ToArray();
	object[] children = r.ChildRows.Select(Visualize).ToArray();
	return Util.HorizontalRun(false, columnMats.Concat(children.Length > 0 ? new []{ children } : new object[0]));
}

MatRow[] GetMatTable2(Mat src, bool debug = false, string debugTitle = null)
{
	using Mat gray = src.CvtColor(ColorConversionCodes.RGB2GRAY);
	using Mat binaryInv = gray.Threshold(120, 255, ThresholdTypes.BinaryInv);
	using Mat binary = ~binaryInv;

	double[] colBlacks = GetBlacks(binaryInv, ReduceDimension.Row);
	int[] cols = CellSpan.Scan(colBlacks, threshold: 0.9).Select(x => x.Center).ToArray();

	int colFrom = 2, colTo = 3;
	int l2colFrom = 3, l2colTo = cols.Length - 1;
	
	MatRow[] left = GetTableFromCols(cols, 0, 2, binary, binaryInv, left: 0, top: 0);
	MatRow[] middle = GetTableFromCols(cols, colFrom, colTo, binary, binaryInv, left: 0, top: 0);
	MatRow[] right = GetTableFromCols(cols, l2colFrom, l2colTo, binary, binaryInv, left: 0, top: 0);
	
	foreach (MatRow item in left)
	{
		using (Mat destBinary = binary[item.Top, item.Bottom, cols[colFrom], cols[colTo]])
		using (Mat destBinaryInv = binaryInv[item.Top, item.Bottom, cols[colFrom], cols[colTo]])
		{
			item.ChildRows = middle.Where(x => x.CenterY >= item.Top && x.CenterY < item.Bottom).ToArray();
		}
		
		foreach (MatRow subItem in item.ChildRows)
		{
			using (Mat destBinary = binary[subItem.Top, subItem.Bottom, cols[l2colFrom], cols[l2colTo]])
			using (Mat destBinaryInv = binaryInv[subItem.Top, subItem.Bottom, cols[l2colFrom], cols[l2colTo]])
			{
				subItem.ChildRows = right.Where(x => x.CenterY >= subItem.Top && x.CenterY < subItem.Bottom).ToArray();
			}
		}
	}
	
	return left;
}

MatRow[] GetTableFromCols(int[] cols, int colFrom, int colTo, Mat binary, Mat binaryInv, int left, int top)
{
	using Mat half = binaryInv[0, binaryInv.Rows, cols[colFrom] - left, cols[colTo] - left];
	double[] rowBlacks = GetBlacks(half, ReduceDimension.Column);
	int[] rows = CellSpan.Scan(rowBlacks, threshold: 0.9).Select(x => x.Center).ToArray();

	return Enumerable
		.Range(0, rows.Length - 1)
		.Select(yi => new MatRow(
			Enumerable
			.Range(colFrom, colTo - colFrom)
			.Select(xi =>
			{
				var rect = new Rect(cols[xi] - left, rows[yi], cols[xi + 1] - cols[xi], rows[yi + 1] - rows[yi]);
				return binary[rect];
			})
			.ToArray(), cols[colFrom], top + rows[yi], top + rows[yi + 1]))
		.ToArray();
}

Mat[,] GetMatTable(Mat src, bool debug = false, string debugTitle = null)
{
	using Mat gray = src.CvtColor(ColorConversionCodes.RGB2GRAY);
	using Mat binaryInv = gray.Threshold(120, 255, ThresholdTypes.BinaryInv);
	using Mat binary = ~binaryInv;

	double[] colBlacks = GetBlacks(binaryInv, ReduceDimension.Row);
	int[] cols = CellSpan.Scan(colBlacks, threshold: 0.9).Select(x => x.Center).ToArray();

	using Mat half = binaryInv[0, binaryInv.Rows, cols[3], cols.Last()];
	double[] rowBlacks = GetBlacks(half, ReduceDimension.Column);
	int[] rows = CellSpan.Scan(rowBlacks, threshold: 0.9).Select(x => x.Center).ToArray();

	var table = new Mat[rows.Length - 1, cols.Length - 1];
	for (int yi = 0; yi < rows.Length - 1; ++yi)
	{
		for (int xi = 0; xi < cols.Length - 1; ++xi)
		{
			table[yi, xi] = binary[rows[yi] + 1, rows[yi + 1], cols[xi] + 1, cols[xi + 1]];
		}
	}
	return table;
}

static double[] GetBlacks(Mat src, ReduceDimension dimension)
{
	using Mat sum = src.Reduce(dimension, ReduceTypes.Sum, MatType.CV_64F);
	using Mat normalized = sum.Normalize(normType: NormTypes.INF);
	normalized.GetArray(out double[] blacks);
	return blacks;
}

static object Image(Mat src) => Util.Image(src.ToBytes(), Util.ScaleMode.Unscaled);
static object Image(Mat[,] src)
{
	int rows = src.GetLength(0);
	int cols = src.GetLength(1);
	var table = new object[rows, cols];
	for (int y = 0; y < rows; ++y)
	{
		for (int x = 0; x < cols; ++x)
		{
			table[y, x] = Image(src[y, x]);
		}
	}
	return table;
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

public record MatRow(Mat[] ColumnMats, int Left, int Top, int Bottom)
{
	public MatRow[] ChildRows { get; set; } = new MatRow[0];
	public int Height => Bottom - Top;
	public int CenterY => (Bottom + Top) / 2;
}

public record ResultRow(string[] ColumnTexts, ResultRow[] ChildRows)
{
	public int RowSpan => ChildRows.Length == 0 ? 1 : ChildRows.Sum(x => x.RowSpan);

	public static object Visualize(ResultRow[] src)
	{
		var sb = new StringBuilder();
		Visualize(sb, src);
		return Util.RawHtml(sb.ToString());

		static void Visualize(StringBuilder sb, ResultRow[] src, int depth = 0)
		{
			if (depth == 0) sb.AppendLine("<table>");
			for (int ri = 0; ri < src.Length; ++ri)
			{
				ResultRow r = src[ri];
				bool tr = depth == 0 || ri != 0;

				if (tr) sb.AppendLine("<tr>");
				foreach (string cell in r.ColumnTexts)
				{
					sb.AppendLine(@$"<td rowspan=""{r.RowSpan}"">{WebUtility.HtmlEncode(cell.Trim())}</td>");
				}
				Visualize(sb, r.ChildRows, depth + 1);
				if (tr) sb.AppendLine("</tr>");
			}
			if (depth == 0) sb.AppendLine("</table>");
		}
	}
}