using SixteenCoreCharacterMapper.Properties;
using System.Collections.Generic;
using System.Globalization;

namespace SixteenCoreCharacterMapper
{
    public static class TraitDefinitions
    {
        // Generate trait definitions dynamically so they reflect current resource culture
        // Use stable IDs for trait identity so TraitPositions are not affected by localization
        public static IEnumerable<Trait> All
        {
            get
            {
                return new List<Trait>
                {
                    new Trait("Warmth", Strings.Trait_Warmth_Name, Strings.Trait_Warmth_Description, Strings.Trait_Warmth_Low, Strings.Trait_Warmth_High),
                    new Trait("Intellect", Strings.Trait_Intellect_Name, Strings.Trait_Intellect_Description, Strings.Trait_Intellect_Low, Strings.Trait_Intellect_High),
                    new Trait("EmotionalStability", Strings.Trait_EmotionalStability_Name, Strings.Trait_EmotionalStability_Description, Strings.Trait_EmotionalStability_Low, Strings.Trait_EmotionalStability_High),
                    new Trait("Assertiveness", Strings.Trait_Assertiveness_Name, Strings.Trait_Assertiveness_Description, Strings.Trait_Assertiveness_Low, Strings.Trait_Assertiveness_High),
                    new Trait("Gregariousness", Strings.Trait_Gregariousness_Name, Strings.Trait_Gregariousness_Description, Strings.Trait_Gregariousness_Low, Strings.Trait_Gregariousness_High),
                    new Trait("Dutifulness", Strings.Trait_Dutifulness_Name, Strings.Trait_Dutifulness_Description, Strings.Trait_Dutifulness_Low, Strings.Trait_Dutifulness_High),
                    new Trait("SocialConfidence", Strings.Trait_SocialConfidence_Name, Strings.Trait_SocialConfidence_Description, Strings.Trait_SocialConfidence_Low, Strings.Trait_SocialConfidence_High),
                    new Trait("Sensitivity", Strings.Trait_Sensitivity_Name, Strings.Trait_Sensitivity_Description, Strings.Trait_Sensitivity_Low, Strings.Trait_Sensitivity_High),
                    new Trait("Distrust", Strings.Trait_Distrust_Name, Strings.Trait_Distrust_Description, Strings.Trait_Distrust_Low, Strings.Trait_Distrust_High),
                    new Trait("Imagination", Strings.Trait_Imagination_Name, Strings.Trait_Imagination_Description, Strings.Trait_Imagination_Low, Strings.Trait_Imagination_High),
                    new Trait("Reserve", Strings.Trait_Reserve_Name, Strings.Trait_Reserve_Description, Strings.Trait_Reserve_Low, Strings.Trait_Reserve_High),
                    new Trait("Anxiety", Strings.Trait_Anxiety_Name, Strings.Trait_Anxiety_Description, Strings.Trait_Anxiety_Low, Strings.Trait_Anxiety_High),
                    new Trait("Complexity", Strings.Trait_Complexity_Name, Strings.Trait_Complexity_Description, Strings.Trait_Complexity_Low, Strings.Trait_Complexity_High),
                    new Trait("Introversion", Strings.Trait_Introversion_Name, Strings.Trait_Introversion_Description, Strings.Trait_Introversion_Low, Strings.Trait_Introversion_High),
                    new Trait("Orderliness", Strings.Trait_Orderliness_Name, Strings.Trait_Orderliness_Description, Strings.Trait_Orderliness_Low, Strings.Trait_Orderliness_High),
                    new Trait("Emotionality", Strings.Trait_Emotionality_Name, Strings.Trait_Emotionality_Description, Strings.Trait_Emotionality_Low, Strings.Trait_Emotionality_High)
                };
            }
        }
    }
}
