using Playnite.SDK.Plugins;
using Playnite.SDK.Models;
using System.Threading;
using System.Threading.Tasks;

namespace LocalLibrary
{
    public class LocalInstallController : InstallController
    {
        private CancellationTokenSource watcherToken;

        private LocalLibrary pluginInstance;

        public LocalInstallController(Game game, LocalLibrary instance) : base(game)
        {
            Name = "Install using LocalLibrary Plugin";
            pluginInstance = instance;
        }

        public override void Dispose()
        {
            watcherToken?.Cancel();
        }

        public override void Install(InstallActionArgs args)
        {
            StartInstallWatcher();
            pluginInstance.GameInstaller(Game, this);
        }

        public async void StartInstallWatcher()
        {
            watcherToken = new CancellationTokenSource();
            await Task.Run(async () =>
            {
                while (true)
                {
                    if (watcherToken.IsCancellationRequested)
                    {
                        return;
                    }

                    if (Game.InstallDirectory == null)
                    {
                        await Task.Delay(10000);
                        continue;
                    }
                    else
                    {
                        var installInfo = new GameInstallationData()
                        {
                            InstallDirectory = Game.InstallDirectory
                        };

                        InvokeOnInstalled(new GameInstalledEventArgs(installInfo));
                        return;
                    }
                }
            });
        }
    }

    public class LocalUninstallController : UninstallController
    {

        private CancellationTokenSource watcherToken;

        private LocalLibrary pluginInstance;

        public LocalUninstallController(Game game, LocalLibrary instance) : base(game)
        {
            Name = "Uninstall using LocalLibrary Plugin";
            pluginInstance = instance;
        }

        public override void Dispose()
        {
            watcherToken?.Cancel();
        }

        public override void Uninstall(UninstallActionArgs args)
        {
            pluginInstance.GameUninstaller(Game);
            StartUninstallWatcher();
        }

        public async void StartUninstallWatcher()
        {
            watcherToken = new CancellationTokenSource();
            await Task.Run(async () =>
            {
                while (true)
                {
                    if (watcherToken.IsCancellationRequested)
                    {
                        return;
                    }

                    if (Game.InstallDirectory != null)
                    {
                        await Task.Delay(10000);
                        continue;
                    }
                    else
                    {
                        InvokeOnUninstalled(new GameUninstalledEventArgs());
                        return;
                    }
                }
            });
        }
    }
} 