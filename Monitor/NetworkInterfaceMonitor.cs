using System;
using System.Net.NetworkInformation;

namespace Monitor
{
    internal class NetworkInterfaceMonitor
    {
        /*
        public void GetAllInterfacesTraffic()
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
                return;

            NetworkInterface[] interfaces
                = NetworkInterface.GetAllNetworkInterfaces();

            foreach (NetworkInterface ni in interfaces)
            {
                Console.WriteLine("    Bytes Sent: {0} on interface {1}",
                    ni.GetIPv4Statistics().BytesSent, ni.Name);
                Console.WriteLine("    Bytes Received: {0} on interface {1}",
                    ni.GetIPv4Statistics().BytesReceived, ni.Name);
            }
        }
        */

        public string ChooseAndGetInterfaceTraffic()
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
                return null;

            NetworkInterface[] interfaces
              = NetworkInterface.GetAllNetworkInterfaces();

            //Console.WriteLine("Choose interface");
            int Counter = 0;
            string Response = "";
            foreach (NetworkInterface ni in interfaces)
            {
                Console.WriteLine("{0} Interface - {1}", Counter, ni.Name);
                Counter++;


                Console.WriteLine("Bytes Sent: {0} on interface {1}", ni.GetIPv4Statistics().BytesSent, ni.Name);
                Console.WriteLine("Bytes Received: {0} on interface {1}", ni.GetIPv4Statistics().BytesReceived, ni.Name);
                
                Response += $"Bytes Sent: {ni.GetIPv4Statistics().BytesSent} on interface {ni.Name} + \n";
                Response += $"Bytes Received: {ni.GetIPv4Statistics().BytesReceived} on interface {ni.Name}\n\n";
            }
            return Response;
        }
    }
}
