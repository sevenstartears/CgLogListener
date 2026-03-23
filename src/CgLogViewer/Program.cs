using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using System.Windows.Forms;

namespace CgLogViewer
{
    static class Program
    {
        static Mutex mutex;

        [STAThread]
        static void Main()
        {
            bool npcDialogueMode = Environment.GetCommandLineArgs()
                .Any(arg => string.Equals(arg, "--npc-dialogue", StringComparison.OrdinalIgnoreCase));

            if (!npcDialogueMode)
            {
                bool createNew;
                var guidAttribute = (GuidAttribute)Attribute.GetCustomAttribute(
                    Assembly.GetExecutingAssembly(),
                    typeof(GuidAttribute));
                string guid = guidAttribute.Value;
                mutex = new Mutex(true, guid, out createNew);

                if (!createNew)
                {
                    MessageBox.Show("すでに起動中の CgLogViewer があります。", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    return;
                }

                mutex.ReleaseMutex();
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (npcDialogueMode)
            {
                Application.Run(CreateNpcDialogueForm());
                return;
            }

            Application.Run(new FormMain());
        }

        internal static bool IsCurrentProcessElevated()
        {
            using (var identity = WindowsIdentity.GetCurrent())
            {
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        internal static bool TryLaunchNpcDialogueAsAdministrator(IWin32Window owner)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = Application.ExecutablePath,
                    Arguments = "--npc-dialogue",
                    UseShellExecute = true,
                    Verb = "runas",
                    WorkingDirectory = Application.StartupPath
                };

                Process.Start(startInfo);
                return true;
            }
            catch
            {
                MessageBox.Show(owner, "管理者権限で NPC会話抽出 を起動できませんでした。", "起動エラー", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
        }

        static FormNpcDialogue CreateNpcDialogueForm()
        {
            var settings = Settings.GetInstance();
            var translationServices = new Dictionary<TranslationProvider, ITranslationService>
            {
                { TranslationProvider.DeepL, new DeepLTranslationService() },
                { TranslationProvider.Google, new GoogleTranslationService() },
                { TranslationProvider.OpenAI, new OpenAITranslationService() }
            };

            var form = new FormNpcDialogue(
                settings,
                message => translationServices[settings.TranslationProvider]
                    .TranslateToJapaneseAsync(GetSelectedTranslationApiKey(settings), message, CancellationToken.None, settings.OpenAIReasoningEffort),
                () => !string.IsNullOrWhiteSpace(GetSelectedTranslationApiKey(settings)),
                () => GetTranslationProviderLabel(settings));

            form.FormClosed += (sender, e) =>
            {
                foreach (var disposable in translationServices.Values.OfType<IDisposable>())
                {
                    disposable.Dispose();
                }
            };

            return form;
        }

        static string GetSelectedTranslationApiKey(Settings settings)
        {
            switch (settings.TranslationProvider)
            {
                case TranslationProvider.Google:
                    return settings.GoogleApiKey;
                case TranslationProvider.OpenAI:
                    return settings.OpenAIApiKey;
                default:
                    return settings.DeepLApiKey;
            }
        }

        static string GetTranslationProviderLabel(Settings settings)
        {
            switch (settings.TranslationProvider)
            {
                case TranslationProvider.Google:
                    return "Google Cloud Translation";
                case TranslationProvider.OpenAI:
                    return "OpenAI API";
                default:
                    return "DeepL API Free";
            }
        }
    }
}
