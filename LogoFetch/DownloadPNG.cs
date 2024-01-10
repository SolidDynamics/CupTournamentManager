using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using NLog;

namespace FifaCupDraw.LogoFetch;
public class LogoDownloadConnector
{

    private readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly HttpClient client = new();

    public LogoDownloadConnector(string userAgent)
    {

        if (!string.IsNullOrEmpty(userAgent))
        {
            Log.Info("Adding user agent header to HTTP file");
            client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
        }
        else
        {
            Log.Warn("No user agent header found");
        }
    }

     public async Task<byte[]?> FetchTeamLogoAsync(string teamName)
    {
        var initialLetter = teamName[0].ToString().ToUpper();
        var url = $"https://en.wikipedia.org/w/index.php?title=Category:English_football_logos&from={initialLetter}";

        Log.Info("[{0}] Loading links from {1}", teamName, url);
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(await client.GetStringAsync(url));

        var links = htmlDoc.DocumentNode.SelectNodes("//a[contains(@title, 'File:')]")
                                        .Select(node => new
                                        {
                                            Title = node.Attributes["title"].Value,
                                            Href = "https://en.wikipedia.org" + node.Attributes["href"].Value
                                        }).ToList();

        // Exclude list for known outdated terms
        var excludeTerms = new List<string> { "old", "former", "previous", "1960s", "2000","2013", "Girls", "Ladies" };

        // Filter out links that contain any of the exclude terms
        var filteredLinks = links.Where(link => !excludeTerms.Any(term => link.Title.Contains(term, StringComparison.OrdinalIgnoreCase))).ToList();

        // Perform fuzzy matching
        var bestMatch = FuzzySharp.Process.ExtractOne(teamName, filteredLinks.Select(link => link.Title));


        if (bestMatch != null)
        {
            Log.Info("[{0}] best match identified as {1}", teamName, bestMatch.Value);

            var pageUrl = links.FirstOrDefault(link => link.Title == bestMatch.Value)?.Href;
            if (pageUrl != null)
            {
                Log.Info("[{0}] Getting the response from {1}", teamName, pageUrl);

                var pngBytes = await DownloadPngPreviewAsync(pageUrl);
                // Fetch the SVG page and extract the SVG content
                //var svgContent = DownloadSvgAsync(pageUrl, teamName);
                return pngBytes;
            }
        }

        return null;
    }

    private async Task<byte[]> DownloadPngPreviewAsync(string teamPageUrl)
    {
        try
        {
            // Load the HTML content from the team's Wikipedia page
            var response = await client.GetAsync(teamPageUrl);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Failed to load page: {teamPageUrl}");
                return null;
            }

            var pageContent = await response.Content.ReadAsStringAsync();
            var doc = new HtmlDocument();
            doc.LoadHtml(pageContent);

            // Find the PNG preview image link (change the selector as needed)
            var pngLinkNode = doc.DocumentNode.SelectSingleNode("//a[contains(@class, 'mw-thumbnail-link') and contains(@href, 'png')]");
            if (pngLinkNode == null)
            {
                Console.WriteLine("PNG preview link not found on page.");
                return null;
            }

            var pngUrl = "https:" + pngLinkNode.GetAttributeValue("href", string.Empty);
            if (string.IsNullOrEmpty(pngUrl))
            {
                Console.WriteLine("Failed to find a valid PNG URL.");
                return null;
            }

            // Download the PNG image
            var pngResponse = await client.GetAsync(pngUrl);
            if (!pngResponse.IsSuccessStatusCode)
            {
                Console.WriteLine($"Failed to download PNG: {pngUrl}");
                return null;
            }

            var pngBytes = await pngResponse.Content.ReadAsByteArrayAsync();

            // Save the PNG to the specified output path
            /*  await File.WriteAllBytesAsync(filePath, pngBytes);
             Console.WriteLine($"PNG saved to {filePath}"); */
            return pngBytes;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
            return null;
        }
    }

   
}
