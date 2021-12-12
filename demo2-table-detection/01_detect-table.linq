<Query Kind="Program">
  <NuGetReference>OpenCvSharp4</NuGetReference>
  <NuGetReference>OpenCvSharp4.runtime.win</NuGetReference>
  <Namespace>OpenCvSharp</Namespace>
</Query>

void Main()
{
	Environment.CurrentDirectory = Util.GetPassword("dotnet2021-cv-zhoujie-demo2");
	using Mat src = Cv2.ImRead(@".\resources\sharpness-2021-12-03.jpg");
	var matTable = GetMatTable(src, debug: false);
	Util.HorizontalRun(false, Image(matTable)).Dump();
	//var matTable = GetMatTable(src, debug: false);
	//Util.HorizontalRun(false, Image(matTable.left), Image(matTable.middle), Image(matTable.right)).Dump();
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

(Mat[,] left, Mat[,] middle, Mat[,] right) GetMatTable2(Mat src, bool debug = false, string debugTitle = null)
{
	using Mat gray = src.CvtColor(ColorConversionCodes.RGB2GRAY);
	using Mat binaryInv = gray.Threshold(120, 255, ThresholdTypes.BinaryInv);
	using Mat binary = ~binaryInv;

	double[] colBlacks = GetBlacks(binaryInv, ReduceDimension.Row);
	int[] cols = CellSpan.Scan(colBlacks, threshold: 0.9).Select(x => x.Center).ToArray();
	
	Mat[,] GetTableFromCols(int[] cols, int colFrom, int colTo)
	{
		using Mat half = binaryInv[0, binaryInv.Rows, cols[colFrom], cols[colTo]];
		double[] rowBlacks = GetBlacks(half, ReduceDimension.Column);
		int[] rows = CellSpan.Scan(rowBlacks, threshold: 0.9).Select(x => x.Center).ToArray();

		var table = new Mat[rows.Length - 1, colTo - colFrom];
		for (int yi = 0; yi < rows.Length - 1; ++yi)
		{
			for (int xi = colFrom; xi < colTo; ++xi)
			{
				table[yi, xi - colFrom] = binary[rows[yi] + 1, rows[yi + 1], cols[xi] + 1, cols[xi + 1]];
			}
		}
		return table;
	}
	
	return (
		GetTableFromCols(cols, 0, 2), 
		GetTableFromCols(cols, 2, 3),
		GetTableFromCols(cols, 3, cols.Length - 1));
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
