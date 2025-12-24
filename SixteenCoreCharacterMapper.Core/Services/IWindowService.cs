using System.Threading.Tasks;
using SixteenCoreCharacterMapper.Core.Models;

namespace SixteenCoreCharacterMapper.Core.Services
{
    public interface IWindowService
    {
        Task<Character?> ShowEditCharacterDialogAsync(Character? character = null, bool isDarkMode = false);
        Task<(Dictionary<string, double>? Scores, Dictionary<string, int>? Answers, List<string>? Exclusions)> ShowQuestionnaireAsync(Dictionary<string, int>? existingAnswers = null, List<string>? existingExclusions = null, bool isDarkMode = false);
    }
}
