using System.Text.Json.Serialization;

namespace FifaCupDraw;

    public class Team
    {
         [JsonPropertyName("rank")]
        public int Rank { get; set; }
        
         [JsonPropertyName("name")]
        public string Name { get; set; }

        public string LogoBase64 { get; set; }

        public override string ToString(){
            return Name;
        }
    }

