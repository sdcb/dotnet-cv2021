<Query Kind="Statements">
  <NuGetReference>AngleSharp</NuGetReference>
  <Namespace>AngleSharp.Html.Dom</Namespace>
  <Namespace>AngleSharp.Html.Parser</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>System.Net.Http</Namespace>
</Query>

Util.SetPassword("dotnet2021-cv-zhoujie-demo5", Util.CurrentQuery.Location);
Environment.CurrentDirectory = Util.GetPassword("dotnet2021-cv-zhoujie-demo5");
Directory.CreateDirectory("./resources");
File.WriteAllText("./resources/.gitignore", "*.webp");

using FileStream file = File.OpenWrite("./resources/demo.webp");
using HttpClient http = new();
HttpResponseMessage resp = await http.GetAsync(@"https://github.com/sdcb/blog-data/raw/master/2021/20211218-dotnet2021-cv-zhoujie/demo5-answer-sheet/demo.webp", QueryCancelToken);
resp.EnsureSuccessStatusCode();
using Stream httpStream = await resp.Content.ReadAsStreamAsync(QueryCancelToken);
await httpStream.CopyToAsync(file, QueryCancelToken);

Process.Start(new ProcessStartInfo($@"{Environment.CurrentDirectory}\resources") { UseShellExecute = true });