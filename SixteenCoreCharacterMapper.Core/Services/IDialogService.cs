using System.Threading.Tasks;

namespace SixteenCoreCharacterMapper.Core.Services
{
    public enum DialogResult
    {
        None,
        OK,
        Cancel,
        Yes,
        No
    }

    public interface IDialogService
    {
        void ShowMessage(string message, string title);
        bool Confirm(string message, string title);
        Task<bool> ConfirmAsync(string message, string title);
        DialogResult AskUnsavedChanges();
        Task<DialogResult> AskUnsavedChangesAsync();
        Task<string?> OpenFileAsync(string filter);
        Task<string?> SaveFileAsync(string filter, string defaultName);
    }
}
