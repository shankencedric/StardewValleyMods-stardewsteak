using MoreMultiplayerInfo.Helpers;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Network;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MoreMultiplayerInfo.EventHandlers
{
    public class ReadyCheckHandler
    {
        private readonly IModHelper _helper;

        private readonly IMonitor _monitor;

        private Dictionary<long, HashSet<string>> ReadyPlayers { get; set; }

        private Dictionary<string, HashSet<long>> ReadyChecks { get; set; }

        public ReadyCheckHandler(IMonitor monitor, IModHelper helper)
        {
            _helper = helper;
            _monitor = monitor;

            ReadyPlayers = new Dictionary<long, HashSet<string>>();
            ReadyChecks = new Dictionary<string, HashSet<long>>();
            
            helper.Events.GameLoop.UpdateTicked += UpdateReadyChecks;
        }

        // Returns if the field exists.
        // Stores the field's value to storage.
        private bool RetrieveFieldValue<T>(object obj, string name, ref T storage)
        {
            IReflectedField<T> field = _helper.Reflection.GetField<T>(obj, name, false);
            bool valid = field != null;
            if (valid)
                storage = field.GetValue();
            return valid;
        }


        private void UpdateReadyChecks(object sender, EventArgs e)
        {
            if (!Context.IsWorldReady) return;

            if (ReadyPlayers == null || ReadyChecks == null)
            {
                _monitor.Log($"Ready Players or Ready Checks was null!", LogLevel.Trace);
                ReadyPlayers = new Dictionary<long, HashSet<string>>();
                ReadyChecks = new Dictionary<string, HashSet<long>>();
            }

            var readyPlayersBefore = new Dictionary<long, HashSet<string>>(ReadyPlayers);

            UpdateReadyChecksAndReadyPlayers();

            WatchForReadyCheckChanges(readyPlayersBefore);
        }

        private void WarnIfIAmLastPlayerReady(string readyCheck)
        {
            var readyPlayers = ReadyChecks.GetOrCreateDefault(readyCheck);

            if (Game1.numberOfPlayers() - 1 == readyPlayers.Count && !readyPlayers.Contains(Game1.player.UniqueMultiplayerID))
            {
                _helper.SelfInfoMessage($"You are the last player not ready {GetFriendlyReadyCheckName(readyCheck)}...");
            }

        }

        private void WatchForReadyCheckChanges(Dictionary<long, HashSet<string>> readyPlayersBefore)
        {
            foreach (var player in readyPlayersBefore.Keys)
            {
                if (player == Game1.player.UniqueMultiplayerID) continue; /* Don't care about current player */

                ReadyPlayers.GetOrCreateDefault(player);
                
                var checksBefore = readyPlayersBefore[player];
                var checksNow = ReadyPlayers[player];

                var newCheck = checksNow.FirstOrDefault(c => !checksBefore.Contains(c));
                var removedCheck = checksBefore.FirstOrDefault(c => !checksNow.Contains(c));

                var _player = PlayerHelpers.GetPlayerWithUniqueId(player);
                if (_player == null) continue;
                var playerName = _player.Name;

                var options = ConfigHelper.GetOptions();

                if (newCheck != null && newCheck != "wakeup")
                {
                    if (options.ShowReadyInfoInChatBox)
                    {
                        _helper.SelfInfoMessage($"{playerName} is now ready {GetFriendlyReadyCheckName(newCheck)}.");
                    }

                    if (options.ShowLastPlayerReadyInfoInChatBox)
                    {
                        WarnIfIAmLastPlayerReady(newCheck);
                    }


                }

                if (removedCheck != null && removedCheck != "wakeup" && options.ShowReadyInfoInChatBox)
                {
                    _helper.SelfInfoMessage($"{playerName} is no longer ready {GetFriendlyReadyCheckName(removedCheck)}.");
                }
            }
        }

        private void UpdateReadyChecksAndReadyPlayers()
        {
            var readyChecks = GetReadyChecks();

            var readyChecksResult = new Dictionary<string, HashSet<long>>();
            var readyPlayersResult = new Dictionary<long, HashSet<string>>();

            var allPlayerIds = Game1.getAllFarmers()?.Select(f => f.UniqueMultiplayerID) ?? new List<long>();

            foreach (var player in allPlayerIds)
            {
                readyPlayersResult.Add(player, new HashSet<string>());
            }

            foreach (var readyCheck in readyChecks)
            {
                ProcessReadyCheck(readyCheck, readyChecksResult, readyPlayersResult);
            }

            ReadyChecks = readyChecksResult;
            ReadyPlayers = readyPlayersResult;
        }

        private void ProcessReadyCheck(object readyCheck, Dictionary<string, HashSet<long>> readyChecks, Dictionary<long, HashSet<string>> readyPlayers)
        {
            var readyCheckName = _helper.Reflection.GetProperty<string>(readyCheck, "Name").GetValue() ?? string.Empty;

            NetFarmerCollection readyPlayersCollection = null;
            if (!RetrieveFieldValue(readyCheck, "readyPlayers", ref readyPlayersCollection))
                readyPlayersCollection = new NetFarmerCollection();

            var readyPlayersIds = new HashSet<long>(readyPlayersCollection.Select(p => p.UniqueMultiplayerID).Distinct());

            readyChecks.Add(readyCheckName, readyPlayersIds);

            foreach (var playerId in readyPlayersIds)
            {
                readyPlayers.GetOrCreateDefault(playerId).Add(readyCheckName);
            }
        }

        private List<object> GetReadyChecks()
        {
            object readyChecksValue = null;
            RetrieveFieldValue(Game1.player.team, "readyChecks", ref readyChecksValue);
            var readyChecksValueType = readyChecksValue?.GetType();

            if (readyChecksValueType == null)
            {
                return Enumerable.Empty<object>().ToList();
            }
            else
            {
                var valuesProperty = readyChecksValueType.GetProperty("Values");
                if (valuesProperty == null)
                    return Enumerable.Empty<object>().ToList();
                return ((IEnumerable<object>)valuesProperty.GetValue(readyChecksValue)).ToList();
            }
        }

        public bool IsPlayerWaiting(long playerId)
        {
            return ReadyPlayers.GetOrCreateDefault(playerId).Any(r => r != "wakeup");
        }

        private string GetFriendlyReadyCheckName(string readyCheckName)
        {
            var map = new Dictionary<string, string>
            {
                { "festivalStart", $"for {Game1.CurrentEvent?.FestivalName ?? "the festival"}" },
                { "festivalEnd", "to leave" },
                { "sleep", "to sleep" },
                { "wakeup", "to wake up" },
                { "passOut", "to pass out" }
            };

            _monitor.Log($"Getting ready check friendly name: {readyCheckName}", LogLevel.Debug);

            if (map.ContainsKey(readyCheckName))
            {
                return map[readyCheckName];
            }

            return readyCheckName;
        }
    }
}
