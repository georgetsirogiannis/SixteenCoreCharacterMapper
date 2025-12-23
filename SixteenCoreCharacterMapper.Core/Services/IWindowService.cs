using System.Threading.Tasks;
using SixteenCoreCharacterMapper.Core.Models;

namespace SixteenCoreCharacterMapper.Core.Services
{
    public interface IWindowService
    {
        Task<Character?> ShowEditCharacterDialogAsync(Character? character = null, bool isDarkMode = false);
    }
}
