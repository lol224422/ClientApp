// See https://aka.ms/new-console-template for more information
using Monitor;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

internal static class Source
{
    private delegate string MonitorFunctions();

    private static MonitorFunctions[] FunctionsArray =
    {
        new MonitorFunctions(GetMemoryStatistic),
        new MonitorFunctions(GetMachineName),
        new MonitorFunctions(GetInterfaceTraffic),
        new MonitorFunctions(GetCpuState),
        new MonitorFunctions(GetDiskSpace),
        new MonitorFunctions(GetServices)
    };
    private static LinuxEnvironmentStatistics Reader = new LinuxEnvironmentStatistics();
    private static string GetMemoryStatistic()
    {
        string MemoryUsage = Reader.GetMemoryStatus().Result;
        Console.WriteLine(MemoryUsage);
        return MemoryUsage;
    }

    private static string GetMachineName()
    {
        string MachineName = Environment.MachineName;
        Console.WriteLine($"Machine name - {MachineName}");
        return MachineName;
    }

    private static string GetInterfaceTraffic()
    {
        NetworkInterfaceMonitor interfaceMonitor = new NetworkInterfaceMonitor();
        return interfaceMonitor.ChooseAndGetInterfaceTraffic();
    }

    private static string GetCpuState()
    {
        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        CancellationToken CancellationToken = cancellationTokenSource.Token;
      
        string res =  Reader.MonitorCpuUsage(CancellationToken).Result;
        Console.WriteLine(res);
        return res + "%";
    }

    private static string GetDiskSpace()
    {
        var freeBytes = new DriveInfo("/").AvailableFreeSpace * 8 / 8000000000;
        var totalBytes = new DriveInfo("/").TotalSize * 8 / 8000000000;

        double Devision = (double)freeBytes / totalBytes;
        var PercentOfUsed = 100 - (Devision * 100);

        string Response = $"Free memory - " + freeBytes + "GB" + "\n" + "Total Size - " + totalBytes + "GB" + "\n" + "Memory used - " +(int)PercentOfUsed + "%";
        Console.WriteLine("Free space - " + freeBytes);
        Console.WriteLine("Total size - " + totalBytes);

        Console.WriteLine("====================");
        Console.WriteLine(Response);
        return Response;
    }

    private static string GetServices()
    {
        ProcessStartInfo startInfo = new ProcessStartInfo() {
            FileName = "/bin/bash",
            Arguments = "service --status-all",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        Process proc = new Process() { StartInfo = startInfo, };
        proc.Start();
        string res = proc.StandardOutput.ReadToEnd();
        proc.WaitForExit();
        Console.WriteLine(res);

        string FinalResult = "+ Running \n" + "- Stopped \n" + res;
        return FinalResult;
       
    }

    public static void Main()
    {
        //Conncetcion to main Server
        
        SendMessageFromSocket(11000);
        

        //

        while (true)
        {
            Console.WriteLine("\nTo check RAM usage, print 0");
            Console.WriteLine("To check machin name, print 1");
            Console.WriteLine("To check interface traffic, print 2");
            Console.WriteLine("To check CPU usage, print 3");
            Console.WriteLine("To check disk space, print 4");
            Console.WriteLine("To check service, print 5");
            Console.WriteLine("If you want to leav pring 123\n");

            var input = Console.ReadLine();
            if (input == "123")
                return;

            if (int.Parse(input) > FunctionsArray.Length - 1)
            {
                Console.WriteLine("There is no such function");
            }
            else
            {
                FunctionsArray[int.Parse(input)].Invoke();
            }
        }
    }

    private static void SendMessageFromSocket(int port)
    {


        // Соединяемся с удаленным устройством

        byte[] bytes = new byte[1024];

        // Устанавливаем удаленную точку для сокета
        //IPHostEntry ipHost = Dns.GetHostEntry("192.168.15.33");
        IPAddress ipAddr = IPAddress.Parse("192.168.15.14");
        IPEndPoint ipEndPoint = new IPEndPoint(ipAddr, port);

        Socket sender = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

        // Соединяем сокет с удаленной точкой
        sender.Connect(ipEndPoint);
        Console.WriteLine("Connecting with - {0} ", sender.RemoteEndPoint.ToString());

        while (true)
        {

          //  Console.WriteLine("waiting for response");
            int bytesRec = sender.Receive(bytes);

            string MainServerResponse = Encoding.UTF8.GetString(bytes, 0, bytesRec);

            Console.WriteLine("Server command: {0}", MainServerResponse);

            string Message = "";

            if (int.Parse(MainServerResponse) > FunctionsArray.Length - 1)
            {
                Message = "There is no such function";
            }
            else
            {
                Message =  FunctionsArray[int.Parse(MainServerResponse)].Invoke();
            }

            byte[] msg = Encoding.UTF8.GetBytes(Message);
            int bytesSent = sender.Send(msg);

            // Освобождаем сокет
            //   sender.Shutdown(SocketShutdown.Both);
            //   sender.Close();
        }
    }


}





