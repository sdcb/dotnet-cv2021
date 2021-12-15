<Query Kind="Program">
  <NuGetReference>SharpCompress</NuGetReference>
  <Namespace>System.IO.Compression</Namespace>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Runtime.InteropServices</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>SharpCompress.Readers</Namespace>
  <Namespace>SharpCompress.Archives</Namespace>
  <Namespace>SharpCompress.Common</Namespace>
</Query>

#load ".\paddleocr"

async Task Main()
{
	await PaddleOcrHelper.SetupAsync(QueryCancelToken);
}

public record PaddleOcrKnownModels(Uri DetectionModelUri, Uri ClassifierModelUri, Uri RecognitionModelUri, string RootDirectory)
{
	public async Task Ensure(Uri uri, string prefix, CancellationToken cancellationToken = default)
	{
		string directory = Path.Combine(RootDirectory, prefix);
		string paramsFile = Path.Combine(directory, "inference.pdiparams");
		Directory.CreateDirectory(directory);

		if (!File.Exists(paramsFile))
		{
			string localTarFile = Path.Combine(directory, uri.Segments.Last());
			if (!File.Exists(localTarFile))
			{
				Console.WriteLine($"Downloading {prefix} model from {uri}");
				await PaddleOcrHelper.DownloadFile(uri, localTarFile, cancellationToken);
			}

			Console.WriteLine($"Extracting {localTarFile} to {directory}");
			using (IArchive archive = ArchiveFactory.Open(localTarFile))
			{
				archive.WriteToDirectory(directory);
			}

			Console.WriteLine("Done");
			File.Delete(localTarFile);
		}
	}
	
	public string DetectionDirectory => Path.Combine(RootDirectory, "det");
	public string ClassifierDirectory => Path.Combine(RootDirectory, "cls");
	public string RecognitionDirectory => Path.Combine(RootDirectory, "rec");

	public async Task EnsureAll(CancellationToken cancellationToken = default)
	{
		await Ensure(DetectionModelUri, "det", cancellationToken);
		await Ensure(ClassifierModelUri, "cls", cancellationToken);
		await Ensure(RecognitionModelUri, "rec", cancellationToken);
	}
}

public class PaddleOcrHelper
{
	public const string PaddleOcrKeys = @"C:\_\3rd\paddle\models\keys-cn.txt";
	public const string PaddleOcrENKeys = @"C:\_\3rd\paddle\models\keys-en.txt";

	public static async Task SetupAsync(CancellationToken cancellationToken)
	{
		await EnsureCLib(cancellationToken);
		await PPOcrV2.EnsureAll(cancellationToken);
		await EnPPOcrMobileV2.EnsureAll(cancellationToken);
		await EnsureKeys(cancellationToken);
		//await PPOcrServerV2.EnsureAll(cancellationToken);
		//await PPOcrMobileV2.EnsureAll(cancellationToken);
	}

	public static PaddleOcrKnownModels PPOcrV2 = new PaddleOcrKnownModels(
		new Uri(@"https://paddleocr.bj.bcebos.com/PP-OCRv2/chinese/ch_PP-OCRv2_det_infer.tar"),
		new Uri(@"https://paddleocr.bj.bcebos.com/dygraph_v2.0/ch/ch_ppocr_mobile_v2.0_cls_infer.tar"),
		new Uri(@"https://paddleocr.bj.bcebos.com/PP-OCRv2/chinese/ch_PP-OCRv2_rec_infer.tar"),
		@"C:\_\3rd\paddle\models\ppocr-v2");

	public static PaddleOcrKnownModels PPOcrMobileV2 = new PaddleOcrKnownModels(
		new Uri(@"https://paddleocr.bj.bcebos.com/dygraph_v2.0/ch/ch_ppocr_mobile_v2.0_det_infer.tar"),
		new Uri(@"https://paddleocr.bj.bcebos.com/dygraph_v2.0/ch/ch_ppocr_mobile_v2.0_cls_infer.tar"),
		new Uri(@"https://paddleocr.bj.bcebos.com/dygraph_v2.0/ch/ch_ppocr_mobile_v2.0_rec_infer.tar"),
		@"C:\_\3rd\paddle\models\ppocr-mobile-v2");

	public static PaddleOcrKnownModels EnPPOcrMobileV2 = new PaddleOcrKnownModels(
		PPOcrV2.DetectionModelUri,
		PPOcrV2.ClassifierModelUri,
		new Uri(@"https://paddleocr.bj.bcebos.com/dygraph_v2.0/multilingual/en_number_mobile_v2.0_rec_infer.tar"),
		@"C:\_\3rd\paddle\models\en-ppocr-mobile-v2");

	public static PaddleOcrKnownModels PPOcrServerV2 = new PaddleOcrKnownModels(
		new Uri(@"https://paddleocr.bj.bcebos.com/dygraph_v2.0/ch/ch_ppocr_server_v2.0_det_infer.tar"),
		new Uri(@"https://paddleocr.bj.bcebos.com/dygraph_v2.0/ch/ch_ppocr_mobile_v2.0_cls_infer.tar"),
		new Uri(@"https://paddleocr.bj.bcebos.com/dygraph_v2.0/ch/ch_ppocr_server_v2.0_rec_infer.tar"),
		@"C:\_\3rd\paddle\models\ppocr-server-v2");

	internal static async Task DownloadFile(Uri uri, string localFile, CancellationToken cancellationToken)
	{
		using HttpClient http = new();

		HttpResponseMessage resp = await http.GetAsync(uri, cancellationToken);
		if (!resp.IsSuccessStatusCode)
		{
			throw new Exception($"Failed to download: {uri}, status code: {(int)resp.StatusCode}({resp.StatusCode})");
		}

		using (FileStream file = File.OpenWrite(localFile))
		{
			await resp.Content.CopyToAsync(file, cancellationToken);
		}
	}

	public static async Task EnsureKeys(CancellationToken cancellationToken)
	{
		if (!File.Exists(PaddleOcrKeys))
		{
			Uri uri = new Uri(@"https://raw.githubusercontent.com/PaddlePaddle/PaddleOCR/release/2.3/ppocr/utils/ppocr_keys_v1.txt");
			Console.Write($"{PaddleOcrKeys} not exists, downloading from {uri}... ");
			
			string directory = Path.GetDirectoryName(PaddleOcrKeys);
			Directory.CreateDirectory(directory);
			
			await DownloadFile(uri, PaddleOcrKeys, cancellationToken);
			Console.WriteLine("Done");
		}

		if (!File.Exists(PaddleOcrENKeys))
		{
			Uri uri = new Uri(@"https://raw.githubusercontent.com/PaddlePaddle/PaddleOCR/release/2.3/ppocr/utils/en_dict.txt");
			Console.Write($"{PaddleOcrENKeys} not exists, downloading from {uri}... ");

			string directory = Path.GetDirectoryName(PaddleOcrENKeys);
			Directory.CreateDirectory(directory);

			await DownloadFile(uri, PaddleOcrENKeys, cancellationToken);
			Console.WriteLine("Done");
		}
	}

	public static async Task EnsureCLib(CancellationToken cancellationToken)
	{
		if (!File.Exists(PdInvoke.PaddleInferenceCLib))
		{
			//Uri uri = new Uri(@"https://paddle-inference-lib.bj.bcebos.com/2.2.1/cxx_c/Windows/CPU/x86-64_vs2017_avx_openblas/paddle_inference_c.zip");
			Uri uri = new Uri(@"https://paddle-inference-lib.bj.bcebos.com/2.2.1/cxx_c/Windows/CPU/x86-64_vs2017_avx_mkl/paddle_inference_c.zip");
			string directory = Path.GetDirectoryName(PdInvoke.PaddleInferenceCLib);
			Directory.CreateDirectory(directory);
			string localZipFile = Path.Combine(directory, uri.Segments.Last());

			if (!File.Exists(localZipFile))
			{
				Console.Write($"{PdInvoke.PaddleInferenceCLib} not exists, downloading from {uri}... ");
				await DownloadFile(uri, localZipFile, cancellationToken);
				Console.WriteLine("Done");
			}

			using (ZipArchive zip = ZipFile.OpenRead(localZipFile))
			{
				foreach (ZipArchiveEntry entry in zip.Entries.Where(x => x.FullName.EndsWith(".dll")))
				{
					string localEntryDest = Path.Combine(directory, Path.GetFileName(entry.FullName));
					Console.Write($"Expand {entry.FullName} -> {localEntryDest}... ");
					using Stream stream = entry.Open();
					using FileStream localFile = File.OpenWrite(localEntryDest);
					await stream.CopyToAsync(localFile, cancellationToken);
					Console.WriteLine("Done");
				}
			}

			File.Delete(localZipFile);
		}
	}
}