using System.Threading.Tasks;
using SixteenCoreCharacterMapper.Core.Models;

namespace SixteenCoreCharacterMapper.Core.Services
{
    public interface IImageExportService
    {
        Task ExportImageAsync(Project project, bool isDarkMode, string filePath);
    }
}
