<Query Kind="Statements">
  <NuGetReference>AngleSharp</NuGetReference>
  <Namespace>AngleSharp.Html.Dom</Namespace>
  <Namespace>AngleSharp.Html.Parser</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>System.Net.Http</Namespace>
</Query>

using HttpClient http = new();

string html = await http.GetStringAsync(@"https://sonyalpha.blog/2019/11/10/which-lenses-to-maximise-the-potential-of-the-sony-a7riv/");
HtmlParser parser = new();
IHtmlDocument doc = await parser.ParseDocumentAsync(html);
string[] imageUrls = doc.QuerySelectorAll("section.entry figure:last-of-type ul li a")
	.Select(x => x.GetAttribute("href"))
	.ToArray();
Console.WriteLine($"Find {imageUrls.Length} pictures...");

Util.SetPassword("dotnet2021-cv-zhoujie-demo2", Util.CurrentQuery.Location);
Environment.CurrentDirectory = Util.GetPassword("dotnet2021-cv-zhoujie-demo2");
Directory.CreateDirectory("./resources");
File.WriteAllText("./resources/.gitignore", "*.jpg");
await Parallel.ForEachAsync(imageUrls, QueryCancelToken, async (url, ct) =>
{
	string targetPath = "./resources/" + new Uri(url).Segments.Last();
	if (!File.Exists(targetPath))
	{
		Stream stream = await http.GetStreamAsync(url, ct);
		using FileStream file = File.OpenWrite(targetPath);
		await stream.CopyToAsync(file, ct);
		Console.WriteLine($"{url}...");
	}
	else
	{
		Console.WriteLine($"{url} exists, skip");
	}
});

Process.Start(new ProcessStartInfo($@"{Environment.CurrentDirectory}\resources") { UseShellExecute = true });