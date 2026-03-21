using System.Threading;
using System.Threading.Tasks;

namespace CgLogViewer
{
    public interface ITranslationService
    {
        TranslationProvider Provider { get; }
        Task<string> TranslateToJapaneseAsync(string authKey, string text, CancellationToken cancellationToken);
    }
}
