<Query Kind="Program">
  <NuGetReference>OpenCvSharp4</NuGetReference>
  <NuGetReference>OpenCvSharp4.runtime.win</NuGetReference>
  <Namespace>OpenCvSharp</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

#load ".\01_detect-table"
#load "..\demo3-paddleocr\paddleocr"
#load "..\demo3-paddleocr\paddleocr-setup"
#load "..\demo3-paddleocr\paddleocr-all"
#load "..\demo3-paddleocr\paddleocr-detection"
#load "..\demo3-paddleocr\paddleocr-recognition"

async Task Main()
{
	Environment.CurrentDirectory = Util.GetPassword("dotnet2021-cv-zhoujie-demo4");
	await PaddleOcrHelper.SetupAsync(QueryCancelToken);
	
	using var ocr = new TableOCR();
	foreach (string file in Directory.EnumerateFiles(@".\resources", "*.jpg").OrderBy(x => x).Take(1))
	{
		if (QueryCancelToken.IsCancellationRequested) break;
		
		using Mat src = Cv2.ImRead(file);
		Mat[,] matTable = GetMatTable(src);
		Util.HorizontalRun(false, Image(matTable), ocr.Process(matTable, QueryCancelToken)).Dump();
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
	PaddleOcrAll _chs = new (PaddleOcrHelper.PPOcrV2.RootDirectory, PaddleOcrHelper.PaddleOcrKeys);
	
	public TableOCR()
	{
	}
	
	public string[,] Process(Mat[,] src, CancellationToken cancellationToken)
	{
		int rows = src.GetLength(0);
		int cols = src.GetLength(1);
		var result = new string[rows, cols];
		
		for (int y = 0; y < rows; ++y)
		{
			for (int x = 0; x < cols; ++x)
			{
				if (cancellationToken.IsCancellationRequested) break;
				
				Mat cell = src[y, x];
				if (y > 0 && (x == 3 || x == 4))
				{
					var r = _eng.Run(cell);
					result[y, x] = r.Text;
				}
				else
				{
					using Mat resized = cell.Resize(Size.Zero, 2, 2);
					var r = _chs.Run(resized);
					result[y, x] = r.Text;
				}
			}
		}
		return result;
	}

	public void Dispose()
	{
		_eng.Dispose();
	}
}