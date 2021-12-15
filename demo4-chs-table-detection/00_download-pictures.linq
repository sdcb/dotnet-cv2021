<Query Kind="Program">
  <NuGetReference>Microsoft.Edge.SeleniumTools</NuGetReference>
  <NuGetReference Version="3.141.0">Selenium.WebDriver</NuGetReference>
  <Namespace>Microsoft.Edge.SeleniumTools</Namespace>
  <Namespace>System.IO.Compression</Namespace>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>System.Collections.ObjectModel</Namespace>
  <Namespace>OpenQA.Selenium</Namespace>
</Query>

async Task Main()
{
	using HttpClient http = new();

	using EdgeDriver driver = await WebDriverHelper.CreateEdge(headness: true);
	driver.Url = @"https://baijiahao.baidu.com/s?id=1660378579447159094&wfr=spider&for=pc";
	driver.Navigate();
	(string url, int i)[] imageUrls = driver.FindElementsByCssSelector("img[width='540']")
		.Select(x => x.GetAttribute("src"))
		.Select((x, i) => (x, i))
		.ToArray();

	Util.SetPassword("dotnet2021-cv-zhoujie-demo4", Util.CurrentQuery.Location);
	Environment.CurrentDirectory = Util.GetPassword("dotnet2021-cv-zhoujie-demo4");
	Directory.CreateDirectory("./resources");
	File.WriteAllText("./resources/.gitignore", "*.jpg");
	await Parallel.ForEachAsync(imageUrls, QueryCancelToken, async (url, ct) =>
	{
		string targetPath = $"./resources/{url.i:00}.jpg";
		if (!File.Exists(targetPath))
		{
			Console.WriteLine($"{url}...");
			Stream stream = await http.GetStreamAsync(url.url, ct);
			using FileStream file = File.OpenWrite(targetPath);
			await stream.CopyToAsync(file, ct);
		}
		else
		{
			Console.WriteLine($"{url} exists, skip");
		}
	});
	
	Process.Start(new ProcessStartInfo($@"{Environment.CurrentDirectory}\resources") { UseShellExecute = true });
}

public static class WebDriverHelper
{
	public static async Task<EdgeDriver> CreateEdge(bool headness = false)
	{
		string Version = "96.0.1054.43";
		string baseDirectory = @"C:\_\3rd\edge-driver";
		string DestinationDirectory = @$"{baseDirectory}\{Version}";
		string BinaryPath = @$"{DestinationDirectory}\msedgedriver.exe";

		async Task EnsureInstalled()
		{
			if (!File.Exists(BinaryPath))
			{
				Directory.CreateDirectory(DestinationDirectory.Dump());
				using var http = new HttpClient();
				using var stream = await http.GetStreamAsync(@$"https://msedgedriver.azureedge.net/{Version}/edgedriver_win64.zip".Dump("downloading driver..."));
				using var archive = new ZipArchive(stream);
				archive.ExtractToDirectory(DestinationDirectory);

				Debug.Assert(File.Exists(BinaryPath), "web driver download failed?");
				"driver downloaded and extracted".Dump();
			}
		}

		await EnsureInstalled();
		var options = new EdgeOptions { UseChromium = true };
		if (headness)
		{
			options.AddArguments("headless", "disable-gpu");
		}
		return new EdgeDriver(DestinationDirectory, options);
	}
}
