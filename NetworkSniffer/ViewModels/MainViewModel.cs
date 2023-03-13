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

namespace NetworkSniffer.ViewModels
{
    internal class MainViewModel : BaseViewModel
    {
        private IPNetworkInterface selectedInterface;
        private readonly object packetListLock = new object();
        private InterfaceMonitor monitor;
        public ObservableCollection<IPNetworkInterface> InterfaceList { get; private set; }

        private BaseViewModel _selectedViewModel = new SnifferViewModel();
        public BaseViewModel SelectedViewModel
        {
            get { return _selectedViewModel; }
            set
            {
                _selectedViewModel = value;
                OnPropertyChanged(nameof(SelectedViewModel));
            }
        }

        public IPNetworkInterface SelectedInterface
        {
            get
            {
                return selectedInterface;
            }
            set
            {
                selectedInterface = value;
                Console.WriteLine(selectedInterface.ToString());
                //RaisePropertyChanged("SelectedAddress");
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
                //RaisePropertyChanged("IsInterfaceChangeAllowed");
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
                //RaisePropertyChanged("IsStartEnabled");
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
                //RaisePropertyChanged("IsStopEnabled");
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

        public MainViewModel()
        {
            InterfaceList = new ObservableCollection<IPNetworkInterface>();
            GetInterfaces();
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

        /// <summary>
        /// Adds newly received packet to packet lists
        /// </summary>
        /// <param name="newPacket">Packet to be added to packet lists</param>
        private void ReceiveNewPacket(IPPacket newPacket)
        {
            newPacket.PacketID = (uint)PacketList.Count + 1;

            lock (PacketList)
            {
                PacketList.Add(newPacket);
            }
            IsClearEnabled = true;

            lock (filteredPacketList)
            {
                //AddToFilteredList(newPacket);
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
                Console.WriteLine(InterfaceList.Count);
            }
            else
            {
                selectedInterface = null;
            }

            //RaisePropertyChanged("SelectedInterface");
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

            //RaisePropertyChanged("SelectedInterface");
        }
    }
}
