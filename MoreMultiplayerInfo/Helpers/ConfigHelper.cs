using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using StardewModdingAPI;

namespace MoreMultiplayerInfo.Helpers
{
    public class ConfigHelper
    {
        private static ModConfigOptions _configOptions;

        public static IModHelper Helper { get; set; }

        private static bool _configFileUpdated;

        public static ModConfigOptions GetOptions()
        {
            if (_configOptions == null || _configFileUpdated)
            {
                _configOptions = Helper.ReadConfig<ModConfigOptions>();
                _configFileUpdated = false;
            }

            return _configOptions;
        }

        public static void SaveOptions(object config)
        {
            Helper.WriteConfig(config);
            _configOptions = null;
        }

        static ConfigHelper()
        {
           // not working

            void OnChanged(object sender, FileSystemEventArgs e)
            {
                _configFileUpdated = true; 
                Console.WriteLine("[CHANGED]");
            }

            void Watch()
            {
                while (Helper == null) { }
                var watcher = new FileSystemWatcher(Helper.DirectoryPath, "config.json");
                watcher.NotifyFilter = NotifyFilters.LastWrite;
                watcher.Changed += new FileSystemEventHandler(OnChanged);
                watcher.EnableRaisingEvents = true;
            }

            Thread watch = new Thread(new ThreadStart(Watch));
            watch.Start();
        }

    }
}
