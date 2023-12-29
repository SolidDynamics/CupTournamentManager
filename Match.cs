namespace FifaCupDraw;

public class Match
{
    public required Team HomeTeam { get; set; }
    public required Team AwayTeam { get; set; }

    public Result MatchResult { get; set; }

    public Result PenaltiesResult { get; set; }

    public override string ToString()
    {
        return $"{HomeTeam} v {AwayTeam}";
    }

    public Team Winner
    {
        get
        {
            if (MatchResult is null)
                return null;
            switch (MatchResult.MatchResult)
            {
                case Result.ResultTeam.HomeWin:
                    return HomeTeam;
                case Result.ResultTeam.AwayWin:
                    return AwayTeam;
                case Result.ResultTeam.Draw:
                    return PenaltiesResult.MatchResult == Result.ResultTeam.HomeWin ? HomeTeam : AwayTeam;

            }

            throw new Exception("Could not determine result");
        }
    }
}
