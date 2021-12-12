<Query Kind="Program">
  <NuGetReference>Tesseract</NuGetReference>
  <Namespace>Tesseract</Namespace>
  <Namespace>InteropDotNet</Namespace>
  <Namespace>System.Windows.Forms</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>System.Net.Http</Namespace>
</Query>

async Task Main()
{
	await TesseractHelper.Fast.EnsureTrainedData(new[] { "chi_sim", "eng" }, QueryCancelToken);
	using TesseractEngine engine = new(TesseractHelper.Fast.DataPath, "chi_sim");
	//engine.SetVariable("tessedit_char_whitelist", "1234567890");
	using Pix pix = Pix.LoadFromMemory(GetClipboardImage());
	using Page page = engine.Process(pix, PageSegMode.SingleBlock);
	page.GetText().Dump();
}

private byte[] GetClipboardImage()
{
	using var ms = new MemoryStream();
	Clipboard.GetImage().Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
	return ms.ToArray();
}

public class TesseractHelper
{
	static TesseractHelper()
	{
		LibraryLoader.Instance.CustomSearchPath = new FileInfo(typeof(TesseractEngine).Assembly.Location).Directory.Parent.Parent.ToString();
	}

	public string DataPath { get; init; }
	private string DownloadLink { get; init; }
	
	public async Task EnsureTrainedData(string[] requestLanguages, CancellationToken cancellationToken = default)
	{
		Directory.CreateDirectory(DataPath);

		using HttpClient http = new();
		foreach (string language in requestLanguages)
		{
			string filePath = Path.Combine(DataPath, language + ".traineddata");
			if (!File.Exists(filePath))
			{
				string url = string.Format(DownloadLink, language);
				Console.Write($"Downloading {language} from {url}... ");
				using Stream stream = await http.GetStreamAsync(url, cancellationToken);
				using FileStream destFile = File.OpenWrite(filePath);
				await stream.CopyToAsync(destFile, cancellationToken);
				Console.WriteLine("Done");
			}
		}
	}
	
	public static readonly TesseractHelper Best = new TesseractHelper 
	{ 
		DataPath = @"C:\_\3rd\tesseract\traineddata-best", 
		DownloadLink = "https://github.com/tesseract-ocr/tessdata_best/raw/main/{0}.traineddata"
	};

	public static readonly TesseractHelper Fast = new TesseractHelper
	{
		DataPath = @"C:\_\3rd\tesseract\traineddata-fast",
		DownloadLink = "https://github.com/tesseract-ocr/tessdata_fast/raw/main/{0}.traineddata"
	};
}