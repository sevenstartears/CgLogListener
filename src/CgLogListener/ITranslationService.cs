using System.Threading;
using System.Threading.Tasks;

namespace CgLogListener
{
    public interface ITranslationService
    {
        TranslationProvider Provider { get; }
        Task<string> TranslateToJapaneseAsync(string authKey, string text, CancellationToken cancellationToken);
    }
}
