using System.Diagnostics;
using System.Security.Cryptography;
using Microsoft.VisualBasic;

namespace FifaCupDraw;

partial class Program
{
    public class CupTournament
    {
        public List<Round>? Rounds {get;set;}

        public List<Team>? Teams {get;set;}
        public TournamentStatus? Status { get; internal set; }

        public static CupTournament Create(List<Team> teams)
        {
            CupTournament cupTournament = new() { Teams = teams, Rounds = [] };

            int largestPowerOfTwo = (int)Math.Pow(2, (int)Math.Log(teams.Count, 2));
            var numberToEliminateInQualifyingRound = teams.Count - largestPowerOfTwo;
            cupTournament.Rounds.Add(new() { Name = "Qualifying",NumberOfCompetingTeamsNeeded = numberToEliminateInQualifyingRound * 2,NumberOfByesNeeded = teams.Count - (numberToEliminateInQualifyingRound * 2) });

            var roundTeamsCount = largestPowerOfTwo;

            while (roundTeamsCount != 1)
            {
                string roundName = roundTeamsCount switch
                {
                    2 => "Final",
                    4 => "Semi Finals",
                    8 => "Quarter Finals",
                    _ => $"Round of {roundTeamsCount}"
                };

                cupTournament.Rounds.Add(new Round() { Name = roundName, NumberOfCompetingTeamsNeeded = roundTeamsCount });
                roundTeamsCount /= 2;
            }

            return cupTournament;
        }

        public TournamentStatus GetTournamentStatus()
        {
            for(sbyte i=0; i<Rounds.Count; i++)
            {
                var round = Rounds[i];
                if(!round.IsRoundComplete()) 
                    return new TournamentStatus() { CurrentRound=i, IsComplete=false };
            }
            return new TournamentStatus() { IsComplete=true };
        }
    }

    public record TournamentStatus
    {
        public bool IsComplete {get;set;}
        public sbyte CurrentRound {get;set;}
    }
}
