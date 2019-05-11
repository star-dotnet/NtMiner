﻿using NTMiner.Core;
using NTMiner.Core.Kernels;
using NTMiner.Profile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NTMiner {
    public partial class NTMinerRoot : INTMinerRoot {
        private static readonly string[] gpuIndexChars = new string[] { "a", "b", "c", "d", "e", "f", "g", "h" };
        public string BuildAssembleArgs() {
            if (!CoinSet.TryGetCoin(this.MinerProfile.CoinId, out ICoin mainCoin)) {
                return string.Empty;
            }
            ICoinProfile coinProfile = this.MinerProfile.GetCoinProfile(mainCoin.GetId());
            if (!PoolSet.TryGetPool(coinProfile.PoolId, out IPool mainCoinPool)) {
                return string.Empty;
            }
            if (!CoinKernelSet.TryGetCoinKernel(coinProfile.CoinKernelId, out ICoinKernel coinKernel)) {
                return string.Empty;
            }
            if (!KernelSet.TryGetKernel(coinKernel.KernelId, out IKernel kernel)) {
                return string.Empty;
            }
            if (!kernel.IsSupported(mainCoin)) {
                return string.Empty;
            }
            if (!KernelInputSet.TryGetKernelInput(kernel.KernelInputId, out IKernelInput kernelInput)) {
                return string.Empty;
            }
            ICoinKernelProfile coinKernelProfile = this.MinerProfile.GetCoinKernelProfile(coinProfile.CoinKernelId);
            string wallet = coinProfile.Wallet;
            string pool = mainCoinPool.Server;
            string poolKernelArgs = string.Empty;
            IPoolKernel poolKernel = PoolKernelSet.FirstOrDefault(a => a.PoolId == mainCoinPool.GetId() && a.KernelId == kernel.GetId());
            if (poolKernel != null) {
                poolKernelArgs = poolKernel.Args;
            }
            IPoolProfile poolProfile = MinerProfile.GetPoolProfile(mainCoinPool.GetId());
            string userName = poolProfile.UserName;
            string password = poolProfile.Password;
            if (string.IsNullOrEmpty(password)) {
                password = "x";
            }
            string kernelArgs = kernelInput.Args;
            string coinKernelArgs = coinKernel.Args;
            string customArgs = coinKernelProfile.CustomArgs;
            var argsDic = new Dictionary<string, string> {
                {"mainCoin", mainCoin.Code },
                {"wallet", wallet },
                {"userName", userName },
                {"password", password },
                {"host", mainCoinPool.GetHost() },
                {"port", mainCoinPool.GetPort().ToString() },
                {"pool", pool },
                {"worker", this.MinerProfile.MinerName }
            };// 这里不要考虑{logfile}，{logfile}往后推迟
            if (coinKernelProfile.IsDualCoinEnabled && kernelInput.IsSupportDualMine) {
                Guid dualCoinGroupId = coinKernel.DualCoinGroupId;
                if (dualCoinGroupId == Guid.Empty) {
                    dualCoinGroupId = kernelInput.DualCoinGroupId;
                }
                if (dualCoinGroupId != Guid.Empty) {
                    if (this.CoinSet.TryGetCoin(coinKernelProfile.DualCoinId, out ICoin dualCoin)) {
                        ICoinProfile dualCoinProfile = this.MinerProfile.GetCoinProfile(dualCoin.GetId());
                        if (PoolSet.TryGetPool(dualCoinProfile.DualCoinPoolId, out IPool dualCoinPool)) {
                            string dualWallet = dualCoinProfile.DualCoinWallet;
                            string dualPool = dualCoinPool.Server;
                            IPoolProfile dualPoolProfile = MinerProfile.GetPoolProfile(dualCoinPool.GetId());
                            string dualUserName = dualPoolProfile.UserName;
                            string dualPassword = dualPoolProfile.Password;
                            if (string.IsNullOrEmpty(dualPassword)) {
                                dualPassword = "x";
                            }
                            argsDic.Add("dualCoin", dualCoin.Code);
                            argsDic.Add("dualWallet", dualWallet);
                            argsDic.Add("dualUserName", dualUserName);
                            argsDic.Add("dualPassword", dualPassword);
                            argsDic.Add("dualHost", dualCoinPool.GetHost());
                            argsDic.Add("dualPort", dualCoinPool.GetPort().ToString());
                            argsDic.Add("dualPool", dualPool);

                            kernelArgs = kernelInput.DualFullArgs;
                            AssembleArgs(argsDic, ref kernelArgs, isDual: true);
                            AssembleArgs(argsDic, ref poolKernelArgs, isDual: true);
                            AssembleArgs(argsDic, ref customArgs, isDual: true);

                            string dualWeightArg;
                            if (!string.IsNullOrEmpty(kernelInput.DualWeightArg)) {
                                if (coinKernelProfile.IsAutoDualWeight && kernelInput.IsAutoDualWeight) {
                                    dualWeightArg = string.Empty;
                                }
                                else {
                                    dualWeightArg = $"{kernelInput.DualWeightArg} {Convert.ToInt32(coinKernelProfile.DualCoinWeight)}";
                                }
                            }
                            else {
                                dualWeightArg = string.Empty;
                            }

                            return $"{kernelArgs} {dualWeightArg} {poolKernelArgs} {customArgs}";
                        }
                    }
                }
            }
            AssembleArgs(argsDic, ref kernelArgs, isDual: false);
            AssembleArgs(argsDic, ref coinKernelArgs, isDual: false);
            AssembleArgs(argsDic, ref poolKernelArgs, isDual: false);
            AssembleArgs(argsDic, ref customArgs, isDual: false);
            string devicesArgs = string.Empty;
            if (!string.IsNullOrWhiteSpace(kernelInput.DevicesArg)) {
                List<int> useDevices = GetUseDevices();
                if (useDevices.Count != 0 && useDevices.Count != GpuSet.Count) {
                    string separator = kernelInput.DevicesSeparator;
                    if (kernelInput.DevicesSeparator == "space") {
                        separator = " ";
                    }
                    if (string.IsNullOrEmpty(separator)) {
                        List<string> gpuIndexes = new List<string>();
                        foreach (var index in useDevices) {
                            int i = index;
                            if (kernelInput.DeviceBaseIndex != 0) {
                                i = index + kernelInput.DeviceBaseIndex;
                            }
                            if (i > 9) {
                                gpuIndexes.Add(gpuIndexChars[i - 10]);
                            }
                            else {
                                gpuIndexes.Add(i.ToString());
                            }
                        }
                        devicesArgs = $"{kernelInput.DevicesArg} {string.Join(separator, gpuIndexes)}";
                    }
                    else {
                        devicesArgs = $"{kernelInput.DevicesArg} {string.Join(separator, useDevices)}";
                    }
                }
            }
            StringBuilder sb = new StringBuilder();
            sb.Append(kernelArgs);
            if (!string.IsNullOrEmpty(coinKernelArgs)) {
                sb.Append(" ").Append(coinKernelArgs);
            }
            if (!string.IsNullOrEmpty(poolKernelArgs)) {
                sb.Append(" ").Append(poolKernelArgs);
            }
            if (!string.IsNullOrEmpty(devicesArgs)) {
                sb.Append(" ").Append(devicesArgs);
            }
            if (!string.IsNullOrEmpty(customArgs)) {
                sb.Append(" ").Append(customArgs);
            }
            return sb.ToString();
        }

        private static void AssembleArgs(Dictionary<string, string> prms, ref string args, bool isDual) {
            if (string.IsNullOrEmpty(args)) {
                args = string.Empty;
                return;
            }
            args = args.Replace("{mainCoin}", prms["mainCoin"]);
            args = args.Replace("{wallet}", prms["wallet"]);
            args = args.Replace("{userName}", prms["userName"]);
            args = args.Replace("{password}", prms["password"]);
            args = args.Replace("{host}", prms["host"]);
            args = args.Replace("{port}", prms["port"]);
            args = args.Replace("{pool}", prms["pool"]);
            args = args.Replace("{worker}", prms["worker"]);
            args = args.Replace("{wallet}", prms["wallet"]);
            if (isDual) {
                args = args.Replace("{dualCoin}", prms["dualCoin"]);
                args = args.Replace("{dualWallet}", prms["dualWallet"]);
                args = args.Replace("{dualUserName}", prms["dualUserName"]);
                args = args.Replace("{dualPassword}", prms["dualPassword"]);
                args = args.Replace("{dualHost}", prms["dualHost"]);
                args = args.Replace("{dualPort}", prms["dualPort"]);
                args = args.Replace("{dualPool}", prms["dualPool"]);
            }
            // 这里不要考虑{logfile}，{logfile}往后推迟
        }
    }
}
