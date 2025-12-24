using System.Collections.Generic;
using System.Threading.Tasks;
using SixteenCoreCharacterMapper.Core.Models;

namespace SixteenCoreCharacterMapper.Core.Services
{
    public interface IQuestionnaireService
    {
        Task<List<PersonalityQuestion>> LoadQuestionsAsync();
        Dictionary<string, double> CalculateScores(Dictionary<PersonalityQuestion, int> answers);
    }
}
