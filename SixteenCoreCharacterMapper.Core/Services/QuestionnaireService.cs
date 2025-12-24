using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using SixteenCoreCharacterMapper.Core.Models;
using SixteenCoreCharacterMapper.Core.Properties;

namespace SixteenCoreCharacterMapper.Core.Services
{
    public class QuestionnaireService : IQuestionnaireService
    {
        private const string ResourceName = "SixteenCoreCharacterMapper.Core.Data.questions.json";

        public async Task<List<PersonalityQuestion>> LoadQuestionsAsync()
        {
            var assembly = Assembly.GetExecutingAssembly();
            
            // Try to find the resource name if the namespace is different
            var resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith("questions.json"));

            if (resourceName == null)
            {
                return new List<PersonalityQuestion>();
            }

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream))
            {
                var json = await reader.ReadToEndAsync();
                var questions = JsonSerializer.Deserialize<List<PersonalityQuestion>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (questions != null)
                {
                    foreach (var question in questions)
                    {
                        if (!string.IsNullOrEmpty(question.ResourceKey))
                        {
                            var text = Strings.ResourceManager.GetString(question.ResourceKey);
                            question.Text = text ?? question.ResourceKey;
                        }
                    }
                }

                return questions ?? new List<PersonalityQuestion>();
            }
        }

        public Dictionary<string, double> CalculateScores(Dictionary<PersonalityQuestion, int> answers)
        {
            var scores = new Dictionary<string, double>();
            var traitSums = new Dictionary<string, int>();
            var traitCounts = new Dictionary<string, int>();

            foreach (var entry in answers)
            {
                var question = entry.Key;
                var answer = entry.Value; // 1 to 5

                if (answer < 1 || answer > 5) continue;

                var score = question.IsReverseKeyed ? (6 - answer) : answer;

                if (!traitSums.ContainsKey(question.TraitId))
                {
                    traitSums[question.TraitId] = 0;
                    traitCounts[question.TraitId] = 0;
                }

                traitSums[question.TraitId] += score;
                traitCounts[question.TraitId]++;
            }

            foreach (var traitId in traitSums.Keys)
            {
                var sum = traitSums[traitId];
                var count = traitCounts[traitId];

                if (count == 0)
                {
                    scores[traitId] = 0.5; // Default
                    continue;
                }

                // Min possible sum = 1 * count
                // Max possible sum = 5 * count
                double min = 1.0 * count;
                double max = 5.0 * count;

                // Normalize to 0.0 - 1.0
                double normalized = (sum - min) / (max - min);
                
                // Clamp just in case
                if (normalized < 0) normalized = 0;
                if (normalized > 1) normalized = 1;

                scores[traitId] = normalized;
            }

            return scores;
        }
    }
}
