using System.Text.Json;
using Microsoft.Extensions.Configuration;
using NLog;
using System.Text;
using HtmlAgilityPack;

namespace FifaCupDraw.LogoFetch;

public static class FootballTeamLogoFetcher
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public static async Task UpdateTeamLogosAsync(string filePath, string userDataFolder, string userAgent)
    {
        // Read the JSON file
        var jsonData = await File.ReadAllTextAsync(filePath);
        var teams = JsonSerializer.Deserialize<List<Team>>(jsonData);
        var notFoundTeams = new List<string>();

        Log.Info("Finding crest imagery for {0} teams", teams?.Count);

        // Define the PNGLogos directory path
        var directoryPath = Path.Combine(userDataFolder, "PNGLogos");
        Directory.CreateDirectory(directoryPath); // Ensure the directory exists
        Log.Info("PNG files will be stored in {0}", directoryPath);

        foreach (var team in teams)
        {
            Log.Info("Processing team {0}", team);
            var pngFilePath = Path.Combine(directoryPath, $"{team.Name.Replace(" ", "_")}.png");

            Log.Info("[{0}] PNG file path is {1}", team, pngFilePath);
            if (File.Exists(pngFilePath))
            {
                Log.Info("[{0}] PNG file already exists", team, pngFilePath);
            }
            else
            {

                Log.Info("[{0}] Starting PNG download process", team);
                LogoDownloadConnector logoDownloadConnector = new LogoDownloadConnector(userAgent);
                var pngContent = await logoDownloadConnector.FetchTeamLogoAsync(team.Name);
                if (pngContent is not null)
                {
                    Log.Info("[{0}] Saving team crest to ", pngFilePath);
                    await File.WriteAllBytesAsync(pngFilePath, pngContent);
                    //team.LogoBase64 = ConvertSvgToPngBase64(System.Text.Encoding.UTF8.GetBytes(svgContent));
                }
                else
                {
                    notFoundTeams.Add(team.Name);
                }
            }
        }

        // Write the modified data back to a new JSON file
        //await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(teams, new JsonSerializerOptions { WriteIndented = true }));

        // Record the teams for which logos weren't found
        await File.WriteAllLinesAsync(Path.Combine(directoryPath, "NotFoundTeams.txt"), notFoundTeams);

        GenerateHtmlForTeams(teams, Path.Combine(userDataFolder, "TeamsWithLogos.html"));

        Console.WriteLine("Modified JSON has been saved as 'ModifiedTeamsList.json'");
        Console.WriteLine("Teams without found logos have been listed in 'NotFoundTeams.txt'");
    }
/* /* 
    private static async Task SaveToPng(string pngPath, Team team, string svgContent)
    {
        var imageBytes = ConvertSvgToPngBase64(svgContent);
        string path = Path.Combine(pngPath, team.Name.Replace(" ", "_") + ".png");
        await File.WriteAllBytesAsync(path, imageBytes);
    } */

/*     private static async Task<string> DownloadSvgAsync(string pageUrl, string teamName)
    {
        try
        {

            // Attempt to fetch the page content
            var response = await client.GetAsync(pageUrl);

            // Check if the request was successful
            if (response.IsSuccessStatusCode)
            {

                Log.Info("[{0}] Successfully accessed linked page", teamName);

                var htmlContent = await response.Content.ReadAsStringAsync();
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(htmlContent);

                // Extract the SVG URL from the href attribute of the link surrounding the SVG image.
                var locator = "//div[@id='file']//a[@href]";

                Log.Info("[{0}] Attempting to find the link with locator {1}", teamName, locator);
                var svgLinkNode = htmlDoc.DocumentNode.SelectSingleNode(locator);
                if (svgLinkNode != null)
                {
                    var svgUrl = svgLinkNode.Attributes["href"].Value;

                    Log.Info("[{0}] Link found {1}", teamName, svgUrl);
                    if (!string.IsNullOrEmpty(svgUrl))
                    {
                        // Ensure the URL is in a correct format
                        svgUrl = svgUrl.StartsWith("//") ? "https:" + svgUrl : svgUrl;

                        // Download the SVG content

                        Log.Info("[{0}] Downloading content", teamName);
                        var svgResponse = await client.GetAsync(svgUrl);
                        if (svgResponse.IsSuccessStatusCode)
                        {
                            var svgContent = await svgResponse.Content.ReadAsStringAsync();

                            Log.Info("[{0}] Content downloaded", teamName);
                            return svgContent;
                        }
                    }
                }
                else
                {
                    Log.Info("[{0}] Node locator returned null", teamName);
                }
            }
        }
        catch (HttpRequestException e)
        {
            // Log the exception details for debugging
            Console.WriteLine($"Error fetching SVG: {e.Message}");
        }
        catch (Exception e)
        {
            // Handle other potential exceptions
            Console.WriteLine($"An error occurred: {e.Message}");
        }

        return null; // Return null if there was an error or no SVG found
    }


    private static byte[] ConvertSvgToPngBase64(string svgContent)
    {
        try
        {
            // Load the SVG content into an SVG document
            var byteArray = Encoding.UTF8.GetBytes(svgContent);
            using var stream = new MemoryStream(byteArray);
            var svgDocument = SvgDocument.Open<SvgDocument>(stream);

            // Render the SVG document to a bitmap
            using var bitmap = svgDocument.Draw();

            // Convert the bitmap to a PNG byte array
            using var memoryStream = new MemoryStream();
            bitmap.Save(memoryStream, ImageFormat.Png);
            byte[] pngBytes = memoryStream.ToArray();

            // Convert the PNG byte array to a Base64 string
            return pngBytes;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error converting SVG to PNG: " + ex.Message);
            throw;
        }
    } */ 

    public static void GenerateHtmlForTeams(List<Team> teams, string outputHtmlPath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<html>");
        sb.AppendLine("<head><title>Teams and Logos</title></head>");
        sb.AppendLine("<body>");
        sb.AppendLine("<h1>Teams and Logos</h1>");
        sb.AppendLine("<table>");

        // Variables to control the number of columns
        int columns = 4;
        int count = 0;

        foreach (var team in teams)
        {
            if (count % columns == 0)
            {
                if (count > 0) sb.AppendLine("</tr>");
                sb.AppendLine("<tr>");
            }

            var logoFilePath = $"PNGLogos/{team.Name.Replace(" ", "_")}.png";
            sb.AppendLine("<td>");
            sb.AppendLine($"<h2>{team.Name}</h2>");
            sb.AppendLine($"<img src=\"{logoFilePath}\" alt=\"Logo of {team.Name}\" style=\"width:100px; height:100px;\"/>");
            sb.AppendLine("</td>");

            count++;
        }

        sb.AppendLine("</tr>");
        sb.AppendLine("</table>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        File.WriteAllText(outputHtmlPath, sb.ToString());
    }
}