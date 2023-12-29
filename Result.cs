namespace FifaCupDraw;

public record Result
{
    public required sbyte HomeScore { get; set; }
    public required sbyte AwayScore { get; set; }

    public override string ToString()
    {
        return @"{homeScore}-{awayScore}";
    }

    public ResultTeam MatchResult
    {
        get
        {
            if (HomeScore > AwayScore)
                return ResultTeam.HomeWin;
            if (HomeScore < AwayScore)
                return ResultTeam.AwayWin;

            return ResultTeam.Draw;
        }
    }

    public enum ResultTeam
    {
        HomeWin,
        AwayWin,
        Draw
    }
}