using MoreMultiplayerInfo.EventHandlers;
using MoreMultiplayerInfo.Helpers;
using StardewModdingAPI;

namespace MoreMultiplayerInfo
{
    public class ModEntryHelper
    {
        public ModEntryHelper(IMonitor monitor, IModHelper modHelper)
        {
            var showIcon = new ShowPlayerIconHandler(monitor, modHelper);
            
            var playerWatcher = new PlayerStateWatcher(modHelper);

            //var _configHelper = new ConfigHelper();
            // var logHandler = new LogInputHandler(monitor, modHelper);
        }



    }
}