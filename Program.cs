using System.Text.Json;
using FifaCupDraw.LogoFetch;
using Microsoft.Extensions.Configuration;
using NLog;

namespace FifaCupDraw;

partial class Program
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private static readonly string UserDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "CupTournamentManagerData/");


    static void Main(string[] args)
    {
        LogManager.LoadConfiguration("nlog.config");

        //var teamFile = @"TestTeamsSet.json";
        var teamFile = $"FullTeamsList.json";

        var builder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.Secret.json", optional: true, reloadOnChange: true);
        IConfigurationRoot configuration = builder.Build();

        // Set the UserAgent from the configuration
        var userAgent = configuration["UserAgent"];

        FootballTeamLogoFetcher.UpdateTeamLogosAsync(teamFile, UserDataFolder, userAgent).Wait();

        Log.Info("Teams will be loaded from {0}", teamFile);
        var cupTournamentFileName = $"CupTournament_{Path.GetFileNameWithoutExtension(teamFile)}.json";
        var cupTournamentFilePath = Path.Combine(UserDataFolder, cupTournamentFileName);

        Log.Info("Cup Tournament File Path is {0}", cupTournamentFilePath);
        CupTournament cupTournament;
        if (!File.Exists(cupTournamentFilePath))
        {
            List<Team> teams = ReadTeamsFromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, teamFile));
            Log.Info("File does not already exist, creating a new tournament");
            cupTournament = CupTournament.Create(teams);
            var json = JsonSerializer.Serialize(cupTournament);
            File.WriteAllText(cupTournamentFilePath, json);
            Log.Info("New tournament saved");
        }
        else
        {
            string jsonContent = System.IO.File.ReadAllText(cupTournamentFilePath);
            cupTournament = JsonSerializer.Deserialize<CupTournament>(jsonContent);
            Log.Info("File found and loaded. Current tournement round is {0}", cupTournament.GetTournamentStatus().CurrentRound);
        }

        TournamentStatus tournamentStatus = cupTournament.GetTournamentStatus();

        while (!tournamentStatus.IsComplete)
        {
            Log.Info("Tournament is not complete, current round is {0}", tournamentStatus.CurrentRound);
            if (!cupTournament.Rounds[tournamentStatus.CurrentRound].HasTeamsAdded())
            {
                Log.Info("Current round has no teams, proceeding to draw");
                if (tournamentStatus.CurrentRound == 0)
                {
                    Log.Info("This is the first round, teams added from competition");
                    cupTournament.Rounds[tournamentStatus.CurrentRound].Teams.AddRange(cupTournament.Teams);
                }
                else
                {
                    Log.Info("This is a subsequent round, winners and byes from the previous round being added");
                    cupTournament.Rounds[tournamentStatus.CurrentRound].Teams.AddRange(cupTournament.Rounds[tournamentStatus.CurrentRound - 1].Matches.Select(m => m.Winner));
                    cupTournament.Rounds[tournamentStatus.CurrentRound].Teams.AddRange(cupTournament.Rounds[tournamentStatus.CurrentRound - 1].ByeTeams);
                }
            }

            Log.Info("Progressing the round...");
            cupTournament.Rounds[tournamentStatus.CurrentRound].ProgressRound();


            Log.Info("Saving the round...");
            var json = JsonSerializer.Serialize(cupTournament);
            File.WriteAllText(cupTournamentFilePath, json);

            tournamentStatus = cupTournament.GetTournamentStatus();
            Log.Info("Tournament status {0} and current round is {1}", tournamentStatus.IsComplete, tournamentStatus.CurrentRound);
        }

        LogManager.Shutdown();
    }


    static List<Team> ReadTeamsFromFile(string filePath)
    {
        try
        {
            // Read the JSON file content.
            string jsonContent = System.IO.File.ReadAllText(filePath);

            // Deserialize the JSON content into a List<Team>.

            List<Team> teams = JsonSerializer.Deserialize<List<Team>>(jsonContent);
            return teams;
        }
        catch (Exception ex)
        {
            Log.Info($"Error reading JSON file: {ex.Message}");
            return null;
        }
    }
}