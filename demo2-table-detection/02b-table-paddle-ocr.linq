<Query Kind="Program">
  <NuGetReference>OpenCvSharp4</NuGetReference>
  <NuGetReference>OpenCvSharp4.runtime.win</NuGetReference>
  <Namespace>OpenCvSharp</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

#load ".\01_detect-table"
#load "..\demo4-paddleocr\paddleocr"
#load "..\demo4-paddleocr\paddleocr-setup"
#load "..\demo4-paddleocr\paddleocr-all"
#load "..\demo4-paddleocr\paddleocr-detection"
#load "..\demo4-paddleocr\paddleocr-recognition"

async Task Main()
{
	Environment.CurrentDirectory = Util.GetPassword("dotnet2021-cv-zhoujie-demo2");
	await PaddleOcrHelper.SetupAsync(QueryCancelToken);
	
	using var ocr = new TableOCR();
	foreach (string file in Directory.EnumerateFiles(@".\resources", "*.jpg").OrderBy(x => x).Take(1))
	{
		if (QueryCancelToken.IsCancellationRequested)
		{
			break;
		}
		
		using Mat src = Cv2.ImRead(file);
		//Mat[,] matTable = GetMatTable(src);
		//Mat[,] scaled = Scale(matTable, 2.0, 2.0);
		//Util.HorizontalRun(false, Image(src), ocr.Process(scaled, QueryCancelToken)).Dump();

		MatRow[] matTables = GetMatTable2(src).ToArray();
		ResultRow[] result = ocr.Process(matTables, QueryCancelToken);
		Util.HorizontalRun(false, Image(src), ResultRow.Visualize(result)).Dump();
	}
}

Mat[,] Scale(Mat[,] src, double fx, double fy)
{
	var result = new Mat[src.GetLength(0), src.GetLength(1)];
	for (int y = 0; y < src.GetLength(0); ++y)
	{
		for (int x = 0; x < src.GetLength(1); ++x)
		{
			Mat cell = src[y, x];
			Mat scaled = cell.Resize(Size.Zero, fx, fy);
			result[y, x] = scaled;
		}
	}
	return result;
}

public class TableOCR : IDisposable
{
	PaddleOcrAll _eng = new (PaddleOcrHelper.EnPPOcrMobileV2.RootDirectory, PaddleOcrHelper.PaddleOcrENKeys);
	
	public TableOCR()
	{
	}
	
	public string[,] Process(Mat[,] src, CancellationToken cancellationToken = default)
	{
		int rows = src.GetLength(0);
		int cols = src.GetLength(1);
		var result = new string[rows, cols];
		
		for (int y = 0; y < rows; ++y)
		{
			for (int x = 0; x < cols; ++x)
			{
				if (cancellationToken.IsCancellationRequested)
				{
					break;
				}
				
				using Mat cell = src[y, x];
				var r = _eng.Run(cell);
				result[y, x] = r.Text;
			}
		}
		return result;
	}

	public ResultRow[] Process(MatRow[] src, CancellationToken cancellationToken = default, int level = 0)
	{
		return src.Select((r, y) =>
		{
			string[] texts = r.ColumnMats
				.Select((m, mi) =>
				{
					if (cancellationToken.IsCancellationRequested) return null;
					int x = level switch
					{
						0 => mi,
						1 => mi + 2,
						2 => mi + 3,
						_ => throw new NotSupportedException()
					};

					double scaledRate = (y, x) switch
					{
						_ => 2,
					};

					using Mat scaled = m.Resize(Size.Zero, scaledRate, scaledRate);
					var r = _eng.Run(scaled);
					return r.Text;
				})
				.ToArray();

			return new ResultRow(texts, Process(r.ChildRows, cancellationToken, level + 1));
		}).ToArray();
	}

	public void Dispose()
	{
		_eng.Dispose();
	}
}