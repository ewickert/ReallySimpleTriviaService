// See https://aka.ms/new-console-template for more information
using System.IO.Abstractions;
using System.Runtime.CompilerServices;
using System.ServiceModel.Syndication;
using System.Text;
using System.Xml;
using CommandDotNet;
using CommandDotNet.Spectre;
using ReverseMarkdown;
using Spectre.Console;

namespace ReallySimpleTriviaService;
public class Program
{
    public static int Main(string[] args)
    {
        return new AppRunner<Program>()
            .UseSpectreAnsiConsole()
            .Run(args);
    }

    [DefaultCommand]
    [Command(Description = "Get news from a list of RSS feeds.")]
    public async Task<int> GetNewsAsync(
        IAnsiConsole stdout,
        string webhookUrl,
        string siteFile = "sites.txt")
    {
        var fs             = new FileSystem();
        var md             = new Converter();
        var last           = DateTimeOffset.MinValue;
        var acc            = new StringBuilder();
        var fields         = new Dictionary<string, string>();
        var postComponents = new List<string>();
        var client         = new HttpClient();
        if (!fs.File.Exists(siteFile))
        {
            stdout.MarkupLine($"[red]Error:[/] File {siteFile} does not exist.");
            return 1;
        }

        if (fs.File.Exists("last.txt"))
        {
            last = DateTimeOffset.Parse(fs.File.ReadAllText("last.txt"));
        }
        foreach (var feedURL in fs.File.ReadAllLines(siteFile))
        {
            using var reader = XmlReader.Create(feedURL);
            var feed = SyndicationFeed.Load(reader);
            foreach (var item in feed.Items)
            {
                if (item.PublishDate < last)
                {
                    continue;
                }
                acc.Append("- ");
                acc.Append(item.Title.Text);
                acc.Append(' ');
                acc.Append($"[(link)](<{item.Links[0].Uri.ToString()}>)");
                acc.AppendLine();

                if (acc.Length >= 1500)
                {
                    postComponents.Add(acc.ToString());
                    acc.Clear();
                }
            }
            if (acc.Length > 0)
            {
                postComponents.Add(acc.ToString());
            }
        }
        foreach (var post in postComponents)
        {
            stdout.Write(post);
        }

        fields.Add("username", "newsbot");
        fs.File.WriteAllText("last.txt", DateTimeOffset.Now.ToString());
        foreach (var post in postComponents)
        {
            fields["content"] = post;
            var content = new FormUrlEncodedContent(fields);
            var rsp = await client.PostAsync(webhookUrl, content);
            stdout.WriteLine(rsp.StatusCode.ToString());
        }
        return 0;
    }
}