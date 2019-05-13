﻿using NTMiner.Core;
using NTMiner.Vms;

namespace NTMiner {
    public partial class AppContext {
        public static class ShowWindow {
            public static void EnvironmentVariableEdit(CoinKernelViewModel coinKernelVm, EnvironmentVariable environmentVariable) {
                Views.Ucs.EnvironmentVariableEdit.ShowWindow(coinKernelVm, environmentVariable);
            }

            public static void InputSegmentEdit(CoinKernelViewModel coinKernelVm, InputSegment segment) {
                Views.Ucs.InputSegmentEdit.ShowWindow(coinKernelVm, segment);
            }

            public static void CoinKernelEdit(FormType formType, CoinKernelViewModel source) {
                Views.Ucs.CoinKernelEdit.ShowWindow(formType, source);
            }
        }
    }
}
