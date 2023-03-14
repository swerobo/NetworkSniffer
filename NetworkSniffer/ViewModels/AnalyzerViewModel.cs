﻿using NetworkSniffer.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkSniffer.ViewModels
{
    public class AnalyzerViewModel : BaseViewModel
    {
        #region Constructors
        /// <summary>
        /// Initializes new instance of the AnalyzerViewModel class
        /// </summary>
        public AnalyzerViewModel()
        {
            PacketLengthStats = StatsHandler.PacketLengthStats;

            TransportProtocolStats = StatsHandler.TransportProtocolStats;

            StatsHandler.Timer.Elapsed += Timer_Elapsed;
        }
        #endregion

        #region Properties
        /// <summary>
        /// List of packet length ranges with frequency of packets belonging to each range
        /// </summary>
        public ObservableCollection<PacketLengthCategory> PacketLengthStats { get; private set; }

        /// <summary>
        /// Stores frequencies of packets using particular transport protocol
        /// </summary>
        public ObservableCollection<TransportProtocolCategory> TransportProtocolStats { get; private set; }

        /// <summary>
        /// Time elapsed from the beginning of current capturing session
        /// </summary>
        private string capturingTime;
        public string CapturingTime
        {
            get
            {
                return capturingTime;
            }
            set
            {
                capturingTime = value;
                OnPropertyChanged(nameof(CapturingTime));
            }
        }

        private int packetsTotal;
        /// <summary>
        /// Total packets received in current session
        /// </summary>
        public int PacketsTotal
        {
            get
            {
                return packetsTotal;
            }
            set
            {
                packetsTotal = value;
                OnPropertyChanged(nameof(packetsTotal));
            }
        }

        private int bytesTotal;
        /// <summary>
        /// Total bytes received in current session
        /// </summary>
        public int BytesTotal
        {
            get
            {
                return bytesTotal;
            }
            set
            {
                bytesTotal = value;
                OnPropertyChanged(nameof(bytesTotal));
            }
        }

        private double averagePPS;
        /// <summary>
        /// Packets per second
        /// </summary>
        public double AveragePPS
        {
            get
            {
                return averagePPS;
            }
            set
            {
                averagePPS = value;
                OnPropertyChanged(nameof(averagePPS));
            }
        }

        private int averageBPS;
        /// <summary>
        /// Bytes per second
        /// </summary>
        public int AverageBPS
        {
            get
            {
                return averageBPS;
            }
            set
            {
                averageBPS = value;
                OnPropertyChanged(nameof(averageBPS));
            }
        }
        #endregion

        #region Event handlers
        /// <summary>
        /// Handles timer change by updating CapturingTime and statistics properties
        /// </summary>
        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            CapturingTime = StatsHandler.StopWatch.Elapsed.ToString().Substring(0, 12);
            PacketsTotal = StatsHandler.PacketsTotal;
            BytesTotal = StatsHandler.BytesTotal;
            if (StatsHandler.StopWatch.Elapsed.Seconds != 0)
            {
                AveragePPS = Math.Round((double)PacketsTotal / StatsHandler.StopWatch.Elapsed.Seconds, 3);
                AverageBPS = BytesTotal / StatsHandler.StopWatch.Elapsed.Seconds;
            }
        }
        #endregion
    }
}
