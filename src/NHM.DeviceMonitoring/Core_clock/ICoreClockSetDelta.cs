﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NHM.DeviceMonitoring.Core_clock
{
    public interface ICoreClockSetDelta
    {
        bool SetCoreClockDelta(int coreClock);
        bool ResetCoreClockDelta();
    }
}
