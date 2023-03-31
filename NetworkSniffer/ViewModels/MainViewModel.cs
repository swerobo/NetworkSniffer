using NetworkSniffer.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using NetworkSniffer.ViewModels;
using System.Windows.Data;
using NetworkSniffer.Commands;
using CommunityToolkit.Mvvm.Input;

namespace NetworkSniffer.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        #region Fields
        private SnifferViewModel snifferViewModel = new SnifferViewModel();
        private AnalyzerViewModel analyzerViewModel = new AnalyzerViewModel();
        private HelpViewModel helpViewModel = new HelpViewModel();

        private InterfaceMonitor monitor;
        private string filter;
        private readonly object packetListLock = new object();

        // List of supported protocols
        private List<string> protocolList;
        // This second list is for exlcuding protocols using filter option, exmaple: "!udp"
        private List<string> protocolListToExclude;

        // List of IP addresses from src/dest syntax
        private List<string> srcIPList;
        private List<string> destIPList;

        // List of Ports from sp/dp syntax
        private List<string> srcPortList;
        private List<string> destPortList;

        // List of Lengths from length>/length< syntax
        private List<string> higherLengthList;
        private List<string> lowerLengthList;
        #endregion

        private ViewModelBase _selectedViewModel;
        public ViewModelBase SelectedViewModel
        {
            get { return _selectedViewModel; }
            set
            {
                _selectedViewModel = value;
               OnPropertyChanged(nameof(SelectedViewModel));
            }
        }

        public MainViewModel()
        {
            filter = "";
            //OpenAnalyzer = new RelayCommand(() => OpenAnalyzerExecute());
            //OpenSniffer = new RelayCommand(() => OpenSnifferExecute());
            //OpenHelp = new RelayCommand(() => OpenHelpExecute());
            StartCapture = new RelayCommand(() => StartCaptureExecute());
            StopCapture = new RelayCommand(() => StopCaputureExecute());
            //ClearPacketList = new RelayCommand(() => ClearPacketListExecute());
            //ResetFilter = new RelayCommand(() => ResetFilterExecute());
            //ApplyFilter = new RelayCommand(() => ApplyFilterExecute());
            UpdateViewCommand = new UpdateViewCommand(this);
            RefreshInterfaceList = new RelayCommand(() => RefreshInterfaceListExecute());

            // Initializing the list of valid filter conditions
            protocolList = new List<string>();
            protocolListToExclude = new List<string>();
            srcIPList = new List<string>();
            destIPList = new List<string>();
            srcPortList = new List<string>();
            destPortList = new List<string>();
            higherLengthList = new List<string>();
            lowerLengthList = new List<string>();

            InterfaceList = new ObservableCollection<IPNetworkInterface>();
            PacketList = new ObservableCollection<IPPacket>();
            FilteredPacketList = new ObservableCollection<IPPacket>();
            //SelectedPacketTree = new ObservableCollection<IPPacket>();
            GetInterfaces();
        }

        public ICommand UpdateViewCommand { get; set; }

        public ObservableCollection<IPNetworkInterface> InterfaceList { get; private set; }

        private IPNetworkInterface selectedInterface;

        public IPNetworkInterface SelectedInterface
        {
            get
            {
                return selectedInterface;
            }
            set
            {
                selectedInterface = value;
                OnPropertyChanged(nameof(SelectedInterface));
            }
        }
        
        private bool isInterfaceChangeAllowed = true;
        /// <summary>
        /// Used to enable/disable capture interface change
        /// </summary>
        public bool IsInterfaceChangeAllowed
        {
            get
            {
                return isInterfaceChangeAllowed;
            }
            set
            {
                isInterfaceChangeAllowed = value;
                OnPropertyChanged(nameof(isInterfaceChangeAllowed));
            }
        }

        private bool isStartEnabled = true;
        /// <summary>
        /// Used to enable/disable capture start
        /// </summary>
        public bool IsStartEnabled
        {
            get
            {
                return isStartEnabled;
            }
            set
            {
                isStartEnabled = value;
                OnPropertyChanged(nameof(IsStartEnabled));
            }
        }

        private bool isStopEnabled = false;
        /// <summary>
        /// Used to enable/disable capture start
        /// </summary>
        public bool IsStopEnabled
        {
            get
            {
                return isStopEnabled;
            }
            set
            {
                isStopEnabled = value;
                OnPropertyChanged(nameof(IsStopEnabled));
            }
        }

        private bool isClearEnabled = false;
        /// <summary>
        /// Used to enable/disable clear button
        /// </summary>
        public bool IsClearEnabled
        {
            get
            {
                return isClearEnabled;
            }
            set
            {
                isClearEnabled = value;
               // RaisePropertyChanged("IsClearEnabled");
            }
        }

        public ICommand StartCapture { get; private set; }

        private void StartCaptureExecute()
        {
            if (SelectedInterface == null)
            {
                MessageBox.Show("Please select device address");
            }
            else if (!UserIdentityHandler.IsUserAdministrator())
            {
                MessageBox.Show("Please start program with administrator privileges");
            }
            else
            {
                try
                {
                    if (monitor == null)
                    {
                        monitor = new InterfaceMonitor(SelectedInterface.InterfaceAddress);
                        monitor.newPacketEventHandler += new InterfaceMonitor.NewPacketEventHandler(ReceiveNewPacket);
                        monitor.StartCapture();
                        StatsHandler.Timer.Start();
                        StatsHandler.StopWatch.Start();
                        IsInterfaceChangeAllowed = false;
                        IsStartEnabled = false;
                        IsStopEnabled = true;
                        
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "Could not start capture!");
                }
            }
        }

        public ICommand StopCapture { get; private set; }

        private void StopCaputureExecute()
        {
            if (monitor != null)
            {
                monitor.StopCapture();
                monitor = null;
                StatsHandler.Timer.Stop();
                StatsHandler.StopWatch.Stop();
                IsInterfaceChangeAllowed = true;
                IsStartEnabled = true;
                IsStopEnabled = false;
            }
            if (FilteredPacketList.Count == 0)
            {
                StatsHandler.StopWatch.Reset();
            }
        }

        /// <summary>
        /// Adds newly received packet to packet lists
        /// </summary>
        /// <param name="newPacket">Packet to be added to packet lists</param>
        private void ReceiveNewPacket(IPPacket newPacket)
        {
            Console.WriteLine("StartingCapture!");
            newPacket.PacketID = (uint)PacketList.Count + 1;
            lock (PacketList)
            {
                PacketList.Add(newPacket);
            }
            IsClearEnabled = true;

            lock (filteredPacketList)
            {
                AddToFilteredList(newPacket);
            }

            StatsHandler.UpdateStats(newPacket);
        }

        private ObservableCollection<IPPacket> packetList;
        /// <summary>
        /// Stores all captured packets
        /// </summary>
        public ObservableCollection<IPPacket> PacketList
        {
            get
            {
                return packetList;
            }
            set
            {
                packetList = value;
                // Enables access to packetList from different threads
                BindingOperations.EnableCollectionSynchronization(packetList, packetListLock);
            }
        }

        private ObservableCollection<IPPacket> filteredPacketList;
        /// <summary>
        /// Stores packets from PacketList filtered according to filter conditions
        /// </summary>
        public ObservableCollection<IPPacket> FilteredPacketList
        {
            get
            {
                return filteredPacketList;
            }
            set
            {
                filteredPacketList = value;
                // Enables access to packetList from different threads
                BindingOperations.EnableCollectionSynchronization(filteredPacketList, packetListLock);
            }
        }

        /// <summary>
        /// Gets IP interfaces which are up
        /// </summary>
        private void GetInterfaces()
        {
            foreach (NetworkInterface networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (networkInterface.OperationalStatus == OperationalStatus.Up)
                {
                    foreach (UnicastIPAddressInformation ip in networkInterface.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            InterfaceList.Add(new IPNetworkInterface
                            {
                                InterfaceAddress = ip.Address.ToString(),
                                InterfaceName = networkInterface.Name
                            });
                        }
                    }
                }
            }

            if (InterfaceList.Count > 0)
            {
                SelectedInterface = InterfaceList[0];
            }
            else
            {
                selectedInterface = null;
            }
            OnPropertyChanged(nameof(SelectedInterface));
        }

        public ICommand RefreshInterfaceList { get; private set; }

        private void RefreshInterfaceListExecute()
        {
            IPNetworkInterface prevSelectedAddress = SelectedInterface;

            InterfaceList.Clear();
            GetInterfaces();

            if (InterfaceList.Contains(prevSelectedAddress))
            {
                SelectedInterface = prevSelectedAddress;
            }
            else if (InterfaceList.Count > 0)
            {
                SelectedInterface = InterfaceList[0];
            }
            else
            {
                SelectedInterface = null;
            }
            OnPropertyChanged(nameof(SelectedInterface));
        }

        /// <summary>
        /// Decides whether newPacket should be added to FilteredPacketList or not
        /// </summary>
        /// <param name="newPacket">Packet to be processed and added to FilteredPacketList if it satisfies filter conditions</param>
        private void AddToFilteredList(IPPacket newPacket)
        {
            // If the filterString is empty, just add newPacket to the FilterPacketList
            if (string.IsNullOrEmpty(filter))
            {
                FilteredPacketList.Add(newPacket);
                return;
            }

            // If none of the substrings uses the proper syntax, ignore it and add packet
            // as if there was no filter at all.
            if (protocolList.Count == 0 && protocolListToExclude.Count == 0 && srcIPList.Count == 0 &&
                destIPList.Count == 0 && srcPortList.Count == 0 && destPortList.Count == 0 &&
                higherLengthList.Count == 0 && lowerLengthList.Count == 0)
            {
                FilteredPacketList.Add(newPacket);
                return;
            }

            // These are rules a newPacket must satisfy to be added in the FilteredPacketList.
            // By default all rules are true, so in case one of the condition list is empty
            // a newPacket could be added to FilteredList. Otherwise it is set to false once it
            // enters foreach loop where it must satisfy the conditon to be set to true
            bool IncludeProtocolRule = true;
            bool ExcludeProtocolRule = false;
            bool SrcIPRule = true;
            bool DstIPRule = true;
            bool SrcPortRule = true;
            bool DestPortRule = true;
            bool LowerLengthRule = true;
            bool HigherLengthRule = true;

            // Checking empty protocolList would change the default value of IncludeProtocolRule to false
            if (protocolList.Count != 0)
            {
                IncludeProtocolRule = ApplyProtocolRule(newPacket, protocolList);
            }

            if (protocolListToExclude.Count != 0)
            {
                ExcludeProtocolRule = ApplyProtocolRule(newPacket, protocolListToExclude);
            }

            foreach (string ip in srcIPList)
            {
                SrcIPRule = false;
                if (ip == newPacket.IPHeader[0].SourceIPAddress.ToString())
                {
                    SrcIPRule = true;
                    break;
                }
            }

            foreach (string ip in destIPList)
            {
                DstIPRule = false;
                if (ip == newPacket.IPHeader[0].DestinationIPAddress.ToString())
                {
                    DstIPRule = true;
                    break;
                }
            }

            foreach (string port in srcPortList)
            {
                SrcPortRule = false;
                if (newPacket.TCPPacket.Count > 0 &&
                    port == newPacket.TCPPacket[0].TCPHeader[0].SourcePort.ToString())
                {
                    SrcPortRule = true;
                    break;
                }
                else if (newPacket.UDPPacket.Count > 0 &&
                         port == newPacket.UDPPacket[0].UDPHeader[0].SourcePort.ToString())
                {
                    SrcPortRule = true;
                    break;
                }
            }

            foreach (string port in destPortList)
            {
                DestPortRule = false;
                if (newPacket.TCPPacket.Count > 0 &&
                    port == newPacket.TCPPacket[0].TCPHeader[0].DestinationPort.ToString())
                {
                    DestPortRule = true;
                    break;
                }
                else if (newPacket.UDPPacket.Count > 0 &&
                         port == newPacket.UDPPacket[0].UDPHeader[0].DestinationPort.ToString())
                {
                    DestPortRule = true;
                    break;
                }
            }

            ushort packetLength = newPacket.IPHeader[0].TotalLength;
            foreach (string LowerLength in lowerLengthList)
            {
                LowerLengthRule = false;
                ushort lowerLenght = ushort.Parse(LowerLength);

                if (lowerLenght > packetLength)
                {
                    LowerLengthRule = true;
                    break;
                }
            }

            foreach (string HigherLength in higherLengthList)
            {
                HigherLengthRule = false;
                ushort higherLenght = ushort.Parse(HigherLength);

                if (higherLenght < packetLength)
                {
                    HigherLengthRule = true;
                    break;
                }
            }

            // If newPacket satisfies all the filter rules, add it to filteredPacketList
            if (IncludeProtocolRule == true && ExcludeProtocolRule == false && SrcIPRule == true &&
                DstIPRule == true && SrcPortRule == true && DestPortRule == true &&
                LowerLengthRule == true && HigherLengthRule == true)
            {
                FilteredPacketList.Add(newPacket);
            }
        }

        /// <summary>
        /// Returns bool which indicates wheter the packet satisfies given protocol rule
        /// </summary>
        /// <param name="newPacket"></param>
        /// <param name="ProtocolList"></param>
        private bool ApplyProtocolRule(IPPacket newPacket, List<string> ProtocolList)
        {
            foreach (string protocol in ProtocolList)
            {
                if (protocol.Equals("UDP") && newPacket.UDPPacket.Count > 0)
                {
                    return true;
                }
                else if (protocol.Equals("TCP") && newPacket.TCPPacket.Count > 0)
                {
                    return true;
                }
                else if (protocol.Equals("IGMP") &&
                    newPacket.IPHeader[0].TransportProtocolName == "IGMP")
                {
                    return true;
                }
                else if (protocol.Equals("ICMP") &&
                    newPacket.IPHeader[0].TransportProtocolName == "ICMP")
                {
                    return true;
                }
                else if (protocol.Equals("RAMSES") &&
                    newPacket.UDPPacket.Count > 0 &&
                    (newPacket.UDPPacket[0].UDPHeader[0].DestinationPort >= 55000 &&
                    newPacket.UDPPacket[0].UDPHeader[0].DestinationPort <= 59999))
                {
                    return true;
                }
                else if (protocol.Equals("DNS") &&
                    newPacket.UDPPacket.Count > 0 &&
                    (newPacket.UDPPacket[0].UDPHeader[0].DestinationPort == 53 ||
                    newPacket.UDPPacket[0].UDPHeader[0].SourcePort == 53))
                {
                    return true;
                }
                else if (protocol.Equals("HTTPS") &&
                    ((newPacket.UDPPacket.Count > 0 &&
                     newPacket.UDPPacket[0].ApplicationProtocolType.PortName.Equals(protocol)) ||
                     (newPacket.TCPPacket.Count > 0 &&
                     newPacket.TCPPacket[0].ApplicationProtocolType.PortName.Equals(protocol))))
                {
                    return true;
                }
                else if (protocol.Equals("HTTP") &&
                    ((newPacket.UDPPacket.Count > 0 &&
                     newPacket.UDPPacket[0].ApplicationProtocolType.PortName.Equals(protocol)) ||
                     (newPacket.TCPPacket.Count > 0 &&
                     newPacket.TCPPacket[0].ApplicationProtocolType.PortName.Equals(protocol))))
                {
                    return true;
                }
                else if (protocol.Equals("SSH") &&
                    ((newPacket.UDPPacket.Count > 0 &&
                     newPacket.UDPPacket[0].ApplicationProtocolType.PortName.Equals(protocol)) ||
                     (newPacket.TCPPacket.Count > 0 &&
                     newPacket.TCPPacket[0].ApplicationProtocolType.PortName.Equals(protocol))))
                {
                    return true;
                }
                else if (protocol.Equals("IRC") &&
                    ((newPacket.UDPPacket.Count > 0 &&
                     newPacket.UDPPacket[0].ApplicationProtocolType.PortName.Equals(protocol)) ||
                     (newPacket.TCPPacket.Count > 0 &&
                     newPacket.TCPPacket[0].ApplicationProtocolType.PortName.Equals(protocol))))
                {
                    return true;
                }
            }

            return false;
        }

        //public ICommand OpenHelp { get; private set; }

        //private void OpenHelpExecute()
        //{
          //  SelectedViewModel = helpViewModel;
          //  OnPropertyChanged(nameof(SelectedViewModel));
        //}
    }
}
