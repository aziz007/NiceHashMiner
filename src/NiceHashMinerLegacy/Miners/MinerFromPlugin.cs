﻿using MinerPlugin;
using NiceHashMiner.Algorithms;
using NiceHashMinerLegacy.Common.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NiceHashMinerLegacy.Common.Device;
using CommonAlgorithm = NiceHashMinerLegacy.Common.Algorithm;
using NiceHashMiner.Plugin;
using NiceHashMiner.Configs;
using NiceHashMiner.Miners.IntegratedPlugins;
using NiceHashMiner.Devices;
using NiceHashMiner.Stats;
using NiceHashMinerLegacy.Common;

namespace NiceHashMiner.Miners
{
    // pretty much just implement what we need and ignore everything else
    public class MinerFromPlugin : Miner
    {
        private readonly IMinerPlugin _plugin;
        private readonly IMiner _miner;

        public MinerFromPlugin(string pluginUUID, List<MiningPair> miningPairs, string groupKey) : base(pluginUUID, miningPairs, groupKey)
        {
            _plugin = MinerPluginsManager.GetPluginWithUuid(pluginUUID);
            _miner = _plugin.CreateMiner();
        }

        public override async Task<ApiData> GetSummaryAsync()
        {
            IsUpdatingApi = true;
            var apiData = await _miner.GetMinerStatsDataAsync();
            IsUpdatingApi = false;

            // TODO workaround plugins should return this info
            // create empty stub if it is null
            if (apiData == null)
            {
                Logger.Debug(MinerTag(), "GetSummary returned null... Will create ZERO fallback");
                apiData = new ApiData();
            }
            if (apiData.AlgorithmSpeedsPerDevice == null)
            {
                apiData = new ApiData();
                var perDevicePowerDict = new Dictionary<string, int>();
                var perDeviceSpeedsDict = new Dictionary<string, IReadOnlyList<AlgorithmTypeSpeedPair>>();
                var perDeviceSpeeds = MiningPairs.Select(pair => (pair.Device.UUID, pair.Algorithm.IDs.Select(type => new AlgorithmTypeSpeedPair(type, 0d))));
                foreach (var kvp in perDeviceSpeeds)
                {
                    var uuid = kvp.Item1; // kvp.UUID compiler doesn't recognize ValueTypes lib???
                    perDeviceSpeedsDict[uuid] = kvp.Item2.ToList();
                    perDevicePowerDict[uuid] = 0;
                }
                apiData.AlgorithmSpeedsPerDevice = perDeviceSpeedsDict;
                apiData.PowerUsagePerDevice = perDevicePowerDict;
                apiData.PowerUsageTotal = 0;
                apiData.AlgorithmSpeedsTotal = perDeviceSpeedsDict.First().Value;
            }

            // TODO temporary here move it outside later
            MiningStats.UpdateGroup(apiData, _plugin.PluginUUID, _plugin.Name);

            return apiData;
        }

        // TODO this thing 
        public override void Start(string miningLocation, string username)
        {
            if (_isEnded) return;
            _miner.InitMiningLocationAndUsername(miningLocation, username);
            _miner.InitMiningPairs(MiningPairs);
            EthlargementIntegratedPlugin.Instance.Start(MiningPairs);
            _miner.StartMining();
            IsRunning = true;
        }

        public override void Stop()
        {
            // TODO thing about this case, closing opening on switching
            // EthlargementIntegratedPlugin.Instance.Stop(_miningPairs);
            MiningStats.RemoveGroup(MiningPairs.Select(pair => pair.Device.UUID), _plugin.PluginUUID);
            IsRunning = false;
            _miner.StopMining();
            //if (_miner is IDisposable disposableMiner)
            //{
            //    disposableMiner.Dispose();
            //}
        }
    }
}
