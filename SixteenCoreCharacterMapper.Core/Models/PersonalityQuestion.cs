using System.Text.Json.Serialization;

namespace SixteenCoreCharacterMapper.Core.Models
{
    public class PersonalityQuestion
    {
        [JsonIgnore]
        public string Text { get; set; } = string.Empty;
        public string ResourceKey { get; set; } = string.Empty;
        public string TraitId { get; set; } = string.Empty;
        public bool IsReverseKeyed { get; set; }
    }
}
