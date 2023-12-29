using NLog;

namespace FifaCupDraw;

public class Round
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    public Round()
    {
        Teams = [];
        Matches = [];
        ByeTeams = [];
    }
    public string Name { get; set; }

    public List<Team> Teams { get; set; }

    public int NumberOfCompetingTeamsNeeded { get; set; }
    public int NumberOfByesNeeded { get; set; }

    public List<Match> Matches { get; set; }

    public List<Team> ByeTeams { get; set; }

    public bool RoundComplete { get; set; } = false;

    internal bool IsRoundComplete()
    {
        if (Teams is null || Teams.Count == 0)
            return false;

        if (Matches is null || Matches.Count == 0)
            return false;

        if ((Matches.Count * 2) + ByeTeams.Count != Teams.Count)
            return false;

        if (Matches.Where(m => m.Winner == null).Any())
        {
            return false;
        }

        return true;
    }

    internal void ProgressRound()
    {
        if (!HasTeamsAdded())
        {
            Log.Error("Add teams to the round before progressing");
            throw new Exception("Add teams to the round before progressing");
        }

        if (Matches is null || Matches.Count == 0)
        {
            Log.Info("No matches in round, conducting draw");
            DrawMatches();
            Log.Info("Draw complete");
            return;
        }

        if (!Matches.Where(m => m.Winner is null).Any())
        {
            Log.Info("Round is complete");
            RoundComplete = true;
            return;
        }

        Log.Info("Getting the next incomplete match");
        var nextMatch = Matches.Select((m, i) => new { match = m, index = i }).Where(m => m.match.Winner is null).First();
        Log.Info("Next incomplete match is {0}", nextMatch);

        var matchResult = GetScoreFromUser("match");

        Matches[nextMatch.index].MatchResult = matchResult;

        if (matchResult.MatchResult == Result.ResultTeam.Draw)
        {
            Result penaltiesResult = null;
            while (penaltiesResult is null || penaltiesResult.MatchResult == Result.ResultTeam.Draw)
            {
                penaltiesResult = GetScoreFromUser("penalties");
                if (penaltiesResult.MatchResult != Result.ResultTeam.Draw)
                {
                    Matches[nextMatch.index].PenaltiesResult = penaltiesResult;
                }
                else
                {
                    Log.Info("Penalties cannot end in a draw");
                }
            }
        }

        Result GetScoreFromUser(string description)
        {
            Console.WriteLine($"Enter the {description} score for {nextMatch.match} (e.g., 2-1):");

            bool validScore = false;
            while (!validScore)
            {
                string scoreInput = Console.ReadLine();
                if (TryParseScore(scoreInput, out sbyte homeScore, out sbyte awayScore))
                {
                    validScore = true;
                    return new Result() { AwayScore = awayScore, HomeScore = homeScore }; ;
                }
                else
                {
                    Console.WriteLine("Invalid score format. Please enter the score in the format 'homeScore-awayScore'.");
                }
            }
            throw new Exception();
        }
    }

    private void DrawMatches()
    {
        var numberOfTeams = Teams.Count;
        Log.Info($"{numberOfTeams} teams");

        if (numberOfTeams < NumberOfCompetingTeamsNeeded)
            throw new Exception("Wrong number of teams added to the round!");

        Log.Info($"{this.NumberOfCompetingTeamsNeeded} teams will be scheduled into matches");
        Log.Info($"{this.NumberOfByesNeeded} byes will be awarded");

        // remove Bye Teams
        ByeTeams = Teams.OrderBy(t => t.Rank).Take(NumberOfByesNeeded).ToList();

        var qualifyingRoundTeams = Teams.OrderByDescending(t => t.Rank).Take(NumberOfCompetingTeamsNeeded).OrderBy(t => t.Name).ToList();

        bool drawingHomeTeam = true;
        Team homeTeam = null;
        Team awayTeam = null;

        foreach (var number in SimulateDrawingNumbers(NumberOfCompetingTeamsNeeded))
        {
            var selectedTeam = qualifyingRoundTeams[number - 1];
            if (drawingHomeTeam)
            {
                homeTeam = selectedTeam;
            }
            else
            {
                awayTeam = selectedTeam;
                Matches.Add(new() { HomeTeam = homeTeam, AwayTeam = awayTeam });
                Console.Write(" v ");
            }

            Thread.Sleep(5);
            Console.Write($"({number}) {selectedTeam}");
            if (!drawingHomeTeam)
                Console.WriteLine();
            drawingHomeTeam = !drawingHomeTeam;
        }
    }

    internal bool HasTeamsAdded()
    {
        return !(Teams is null || Teams.Count == 0);
    }

    static IEnumerable<int> SimulateDrawingNumbers(int totalBalls)
    {
        // Fill the bowl with numbers from 1 to totalBalls
        List<int> ballBowl = new List<int>();
        for (int i = 1; i <= totalBalls; i++)
        {
            ballBowl.Add(i);
        }

        Random random = new();

        // Simulate drawing numbers until the bowl is empty
        while (ballBowl.Count > 0)
        {
            // Draw a random index from the remaining balls
            int index = random.Next(0, ballBowl.Count);

            // Get the drawn number
            int drawnNumber = ballBowl[index];

            // Remove the drawn number from the bowl
            ballBowl.RemoveAt(index);

            yield return drawnNumber;
            // Display the drawn number
        }
    }

    static bool TryParseScore(string input, out sbyte homeScore, out sbyte awayScore)
    {
        homeScore = awayScore = 0;

        string[] scoreParts = input.Split('-');

        if (scoreParts.Length == 2 && sbyte.TryParse(scoreParts[0], out homeScore) && sbyte.TryParse(scoreParts[1], out awayScore))
        {
            return true;
        }

        return false;
    }
}
