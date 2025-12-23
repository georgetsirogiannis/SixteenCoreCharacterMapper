using System.Collections.Generic;
using System.Threading.Tasks;
using SixteenCoreCharacterMapper.Core.Models;

namespace SixteenCoreCharacterMapper.Core.Services
{
    public interface IImageExportService
    {
        Task ExportImageAsync(Project project, IEnumerable<Character> charactersToExport, bool isDarkMode, string filePath);
    }
}
