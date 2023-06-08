﻿using AutoDarkModeLib;
using AutoDarkModeSvc.Core;
using AutoDarkModeSvc.Events;
using AutoDarkModeSvc.Handlers;
using AutoDarkModeSvc.Interfaces;
using AutoDarkModeSvc.Modules;
using AutoDarkModeSvc.Timers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoDarkModeSvc.Governors
{
    internal class TimeSwitchGovernor : IAutoDarkModeGovernor
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public Governor Type => Governor.Default;
        private AdmConfigBuilder Builder { get; }
        private GlobalState State { get; } = GlobalState.Instance();
        bool init = true;

        /// <summary>
        /// Instantiates a new TimeSwitchModule.
        /// This module switches themes based on system time and sunrise/sunset
        /// </summary>
        /// <param name="name">unique name of the module</param>
        public TimeSwitchGovernor()
        {
            Builder = AdmConfigBuilder.Instance();
        }

        public GovernorEventArgs Run()
        {
            TimedThemeState ts = new();
            if (Builder.Config.AutoSwitchNotify.Enabled)
            {
                if (State.PostponeManager.Get(Helper.PostponeItemSessionLock) == null)
                {
                    if (!State.PostponeManager.IsGracePeriod && Helper.NowIsBetweenTimes(ts.NextSwitchTime.TimeOfDay, ts.CurrentSwitchTime.AddMilliseconds(2*TimerFrequency.Main).TimeOfDay))
                    {
                        ToastHandler.InvokeDelayAutoSwitchNotifyToast();
                        return new(true);
                    }
                }
            }

            TimeSpan windowStartSpan = ts.NextSwitchTime.AddMinutes(-TimerFrequency.Main).TimeOfDay;
            TimeSpan windowEndSpan = ts.CurrentSwitchTime.TimeOfDay;

            bool reportSwitchWindow = !init;

            if (!Helper.NowIsBetweenTimes(windowStartSpan, windowEndSpan))
            {
                reportSwitchWindow = false;
            }

            if (!State.PostponeManager.IsPostponed)
            {
                if (init) init = false;
                return new(reportSwitchWindow, new(SwitchSource.TimeSwitchModule, Theme.Automatic));
            }
            else
            {
                return new(reportSwitchWindow);
            }
        }

        public void EnableHook()
        {
            Logger.Info("time switch governor selected");
        }

        public void DisableHook()
        {
            // time switch governor doesn't need to do anything as it has no state
        }
    }
}
