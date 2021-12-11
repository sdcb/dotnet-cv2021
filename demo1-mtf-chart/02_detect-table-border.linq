<Query Kind="Program">
  <NuGetReference>OpenCvSharp4</NuGetReference>
  <NuGetReference>OpenCvSharp4.runtime.win</NuGetReference>
  <Namespace>OpenCvSharp</Namespace>
  <Namespace>static OpenCvSharp.Mat</Namespace>
</Query>

void Main()
{
	Environment.CurrentDirectory = Util.GetPassword("dotnet2021-cv-zhoujie-demo1");
	
	foreach (string path in Directory.EnumerateFiles(@".\resources", "*.png"))
	{
		using Mat src = Cv2.ImRead(path);
		Rect mainArea = GetMainArea(src, debug: true, debugTitle: Path.GetFileNameWithoutExtension(path));
	}
}

Rect GetMainArea(Mat src, bool debug = false, string debugTitle = null)
{
	Size size = src.Size();
	using Mat hls = src.CvtColor(ColorConversionCodes.RGB2HLS);
	using Mat grayInv = hls.InRange(new Scalar(0, 100, 0), new Scalar(255, 200, 1));

	double[] colBlacks = GetBlacks(grayInv, ReduceDimension.Row);
	double[] rowBlacks = GetBlacks(grayInv, ReduceDimension.Column);

	int[] rows = CellSpan.Scan(rowBlacks, threshold: 0.9).Select(x => x.Center).ToArray();
	int[] cols = CellSpan.Scan(colBlacks, threshold: 0.8).Select(x => x.Center).ToArray();

	if (debug)
	{
		var demoDc = new DumpContainer();
		var resultDc = new DumpContainer();
		//Util.HorizontalRun(false, Image(src), Image(grayInv), demoDc, resultDc).Dump(debugTitle);
		
		using Mat demo = src.Clone();
		UnsafeIndexer<Vec3b> toShowIndexer = demo.GetUnsafeGenericIndexer<Vec3b>();

		for (int y = 0; y < size.Height; ++y)
			for (int x = 0; x < colBlacks.Length; ++x)
			{
				if (colBlacks[x] > 0.8)
				{
					Vec3b p = toShowIndexer[y, x];
					toShowIndexer[y, x] = new Vec3b(p.Item0, (byte)(255 - 255 * colBlacks[x]), p.Item2);
				}
			}

		for (int y = 0; y < rowBlacks.Length; ++y)
		{
			for (int x = 0; x < size.Width; ++x)
			{
				if (rowBlacks[y] > 0.9)
				{
					Vec3b p = toShowIndexer[y, x];
					toShowIndexer[y, x] = new Vec3b(p.Item0, p.Item1, (byte)(255 - 255 * rowBlacks[y]));
				}
			}
		}
		
		demoDc.Content = Image(demo);
		using Mat resultMat = src[new Rect(cols[0], rows[0], cols[1] - cols[0], rows[1] - rows[0])];
		resultDc.Content = Image(resultMat);
		Util.HorizontalRun(false, Image(src), Image(grayInv), Image(demo), Image(resultMat)).Dump(debugTitle);
	}

	static double[] GetBlacks(Mat src, ReduceDimension dimension)
	{
		using Mat sum = src.Reduce(dimension, ReduceTypes.Sum, MatType.CV_64F);
		using Mat normalized = sum.Normalize(normType: NormTypes.INF);
		normalized.GetArray(out double[] blacks);
		return blacks;
	}
	
	return new Rect(cols[0], rows[0], cols[1] - cols[0], rows[1] - rows[0]);
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
	}
}

object Image(Mat src) => Util.Image(src.ToBytes(), Util.ScaleMode.Unscaled);