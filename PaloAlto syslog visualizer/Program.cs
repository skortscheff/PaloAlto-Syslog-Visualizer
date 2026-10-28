using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PaloAlto_syslog_visualizer
{
    internal static class Program
    {
        static Thread receiveThread, parseThread, statusRefresh;
        static FormMain formMain;
        static UdpClient udpListener;
        // OS buffer for datagrams waiting to be read, the Windows default is too small for log bursts
        const int socketReceiveBufferSize = 32 * 1024 * 1024;
        // the receive thread only reads the socket, the parse thread fills the database
        static BlockingCollection<byte[]> receiveQueue;
        const int receiveQueueCapacity = 200000;
        // Thread.Abort is not supported on .NET Core, the threads stop by checking these flags
        static volatile bool capturing, refreshing;
        internal static bool IsCapturing => capturing;
        // guards the database slots and indexes between the parse thread and Search
        static public readonly object databaseLock = new object();
        static public StructEntryLog[] database;
        static public uint databaseSize = 100000;
        static public int databaseIndexLastItem = -1;
        static public uint databaseTotalWrite = 0;
        static public bool databaseOverwrite = false;

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            formMain = new FormMain();

            StartCapture();
            StartStatusRefresh();

            Application.Run(formMain);
        }

        internal static void resetDB()
        {
            lock (databaseLock)
            {
                database = new StructEntryLog[databaseSize];
                databaseIndexLastItem = -1;
                databaseTotalWrite = 0;
                databaseOverwrite = false;
            }
        }

        // keeps the logs already in the buffer, only resetDB empties it
        internal static void StartCapture()
        {
            if (capturing)
                return;
            if (database == null)
                resetDB();

            udpListener = new UdpClient(514);
            udpListener.Client.ReceiveBufferSize = socketReceiveBufferSize;
            receiveQueue = new BlockingCollection<byte[]>(receiveQueueCapacity);
            capturing = true;
            receiveThread = new Thread(ThreadReceive) { IsBackground = true, Priority = ThreadPriority.AboveNormal };
            receiveThread.Start();
            parseThread = new Thread(ThreadParse) { IsBackground = true };
            parseThread.Start();
            formMain.SetCaptureStatus(true);
        }

        internal static void StartStatusRefresh()
        {
            refreshing = true;
            statusRefresh = new Thread(ThreadRefresh) { IsBackground = true };
            statusRefresh.Start();
        }

        internal static void StopCapture()
        {
            if (!capturing)
                return;
            capturing = false;
            // closing the socket unblocks the pending Receive
            udpListener.Close();
            receiveThread.Join();
            // let the parse thread store what is still queued, then stop it
            receiveQueue.CompleteAdding();
            parseThread.Join();
            formMain.SetCaptureStatus(false);
        }
        internal static void StopRefresh()
        {
            refreshing = false;
        }

        public static void ThreadRefresh()
        {
            while(refreshing)
            {
                Thread.Sleep(1000);
                if (databaseOverwrite)
                    formMain.SetStatusValues(databaseSize, databaseTotalWrite);
                else if (databaseIndexLastItem > -1)
                    formMain.SetStatusValues((uint)databaseIndexLastItem+1, databaseTotalWrite);
                else
                    formMain.SetStatusValues(0, databaseTotalWrite);
            }
        }

        public static void ThreadReceive()
        {
            IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);

            while (capturing)
            {
                try
                {
                    receiveQueue.Add(udpListener.Receive(ref anyIP));
                }
                catch (Exception)
                {
                    ;
                }
            }
        }

        public static void ThreadParse()
        {
            string sReceive;

            foreach (byte[] bReceive in receiveQueue.GetConsumingEnumerable())
            {
                try
                {
                    sReceive = Encoding.ASCII.GetString(bReceive);

                    string[] receivedData = sReceive.Split(',');

                    // not a traffic log, skip it instead of leaving an empty slot
                    if (receivedData.Length < 54)
                        continue;

                    // Search reads the slots on the UI thread, never let it see a half written one
                    lock (databaseLock)
                    {
                        // circular buffer: after the last slot go back to the first one
                        int indexDB = databaseIndexLastItem + 1;
                        if (indexDB == databaseSize)
                        {
                            indexDB = 0;
                            databaseOverwrite = true;
                        }

                        database[indexDB].strAction = receivedData[30];
                        database[indexDB].strActionSource = receivedData[53];
                        database[indexDB].strApplication = receivedData[14];
                        database[indexDB].strBytes = receivedData[31];
                        database[indexDB].strBytesReceived = receivedData[32];
                        database[indexDB].strBytesSent = receivedData[33];
                        database[indexDB].strCategory = receivedData[37];
                        database[indexDB].strDentinationZone = receivedData[17];
                        database[indexDB].strDestinationAddress = receivedData[8];
                        database[indexDB].strDestinationPort = receivedData[25];
                        database[indexDB].strElapsedTime = receivedData[36];
                        database[indexDB].strFlags = receivedData[28];
                        database[indexDB].strInboundInterface = receivedData[18];
                        database[indexDB].strNatDestinationIP = receivedData[10];
                        database[indexDB].strNATDestinationPort = receivedData[27];
                        database[indexDB].strNATSourceIP = receivedData[9];
                        database[indexDB].strNATSourcePort = receivedData[26];
                        database[indexDB].strOutboundInterface = receivedData[19];
                        database[indexDB].strPackets = receivedData[34];
                        database[indexDB].strPacketsReceived = receivedData[45];
                        database[indexDB].strPacketsSent = receivedData[44];
                        database[indexDB].strProtocol = receivedData[29];
                        database[indexDB].strReceiveTime = receivedData[1];
                        database[indexDB].strRuleName = receivedData[11];
                        database[indexDB].strSessionEndReason = receivedData[46];
                        database[indexDB].strSessionID = receivedData[22];
                        database[indexDB].strSourceAddress = receivedData[7];
                        database[indexDB].strSourcePort = receivedData[24];
                        database[indexDB].strSourceUser = receivedData[13];
                        database[indexDB].strSourceZone = receivedData[16];

                        // publish the slot only once it is fully written
                        databaseIndexLastItem = indexDB;
                        databaseTotalWrite++;
                    }
                }
                catch (Exception)
                {
                    ;
                }
            }
        }
    }
}