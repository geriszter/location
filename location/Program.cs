using System;
using System.Net.Sockets;
using System.IO;
using System.Linq;
public class location
{
    enum Style
    {
        h0,
        h1,
        h9,
        def
    }
    static void Main(string[] args)
    {
        try
        {
            string request = null;
            string address;
            int port;
            Style selectedStyle;

            args = GetStyle(args, out selectedStyle);
            args = GetAddress(args, out address);
            args = GetPort(args, out port); // if user enter string throw error

            TcpClient client = new TcpClient();
            client.Connect(address, port);
            client.ReceiveTimeout = 1000;
            client.SendTimeout = 1000;
            StreamWriter sw = new StreamWriter(client.GetStream());
            StreamReader sr = new StreamReader(client.GetStream());

            if (args.Length == 0)
            {
                Console.WriteLine("ERROR: no arguments supplied");
            }
            else
            {
                //-h1 means HTTP/1.1, -h0 means HTTP/1.0 and -h9 means HTTP/0.9 styles
                switch (selectedStyle)
                {
                    case Style.h0:
                        if (args.Length == 1)
                        {
                            request = ($"GET /?{args[0]} HTTP/1.0\r\n\r\n");
                        }
                        else if (args.Length > 1)
                        {
                            int locationLength = args[1].Length;
                            request = ($"POST /{args[0]} HTTP/1.0\r\nContent-Length: {locationLength}\r\n\r\n{args[1]}");
                        }
                        break;

                    case Style.h1:
                        if (args.Length == 1)
                        {
                            //sw.WriteLine($"GET /?name={args[0]} HTTP/1.1\r\nHost: {address}");
                            request = ($"GET /?name={args[0]} HTTP/1.1\r\nHost: {address}\r\n\r\n");
                        }
                        else if (args.Length > 1)
                        {
                            string locationAndName = $"name={args[0]}&location={args[1]}";
                            request = ($"POST / HTTP/1.1\r\nHost: {address}\r\nContent-Length: {locationAndName.Length}\r\n\r\n{locationAndName}");
                        }
                        break;

                    case Style.h9:
                        if (args.Length == 1)
                        {
                            request = ($"GET /{args[0]}\r\n");
                        }
                        else if (args.Length > 1)
                        {
                            request = ($"PUT /{args[0]}\r\n\r\n{args[1]}\r\n");
                        }
                        break;

                    case Style.def:
                        if (args.Length == 1)
                        {
                            request = (args[0]+"\r\n");
                        }
                        else if (args.Length > 1)
                        {
                            string location = args[1];
                            for (int i = 2; i < args.Length; i++)
                            {
                                location += " " + args[i];
                            }
                            location = location.Trim(new Char[] { '\"', '\'', '`', '\\', '.' });
                            request = (args[0] + " " + location+"\r\n");
                        }
                        break;
                }

                request = request.Trim(new Char[] { '\"', '\'', '`', '\\', '.' });

                sw.Write(request);
                sw.Flush();
            }


            bool html = false;
            string rawData = "";

            try
            {
                int num;
                while ((num = sr.Read()) > 0)
                {
                    rawData += ((char)num);
                }
            }
            catch
            {
                int pos = rawData.IndexOf("<html>");
                if (pos > 0)
                {
                    rawData = rawData.Remove(0, pos);
                    html = true;
                }
            }
            //try
            //{
            //    if(rawData == "")
            //    {
            //        rawData += sr.ReadToEnd();
            //    }
            //}
            //catch
            //{
            //}


            if ((rawData.Contains("404") && rawData.Contains("Not Found\r\n")) || rawData.Contains("ERROR:"))
            {
                Console.WriteLine("ERROR: no entries found");
            }
            else if (rawData.Contains("HTTP/0.9 200 OK\r\nContent-Type: text/plain\r\n\r\n") && args.Length > 1)
            {
                Console.WriteLine($"{args[0]} location changed to be {args[1]}");
            }
            else if (rawData.Contains("HTTP/0.9 200 OK\r\nContent-Type: text/plain") && rawData.Length >= 45)
            {
                string[] line = rawData.Trim().Split();
                string location = line[9];
                for (int i = 10; i < line.Length; i++)
                {
                    location += " " + line[i];
                }
                Console.WriteLine($"{args[0]} is {location}");
            }
            else if (rawData.Contains("HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\n\r\n"))
            {
                if (args.Length == 2)
                {
                    Console.WriteLine($"{args[0]} location changed to be {args[1]}");
                }
                else //GET
                {
                    if (!html)
                    {
                        string[] line = rawData.Trim().Split();
                        int indexOfSpace = Array.IndexOf(line, "");

                        string location = line[9];
                        for (int i = 10; i < line.Length; i++)
                        {
                            location += " " + line[i];
                        }
                        rawData = location;
                    }
                    Console.WriteLine($"{args[0]} is {rawData}");
                }
            }
            else if (rawData.Contains("HTTP/1.0 200 OK\r\nContent-Type: text/plain\r\n\r\n") && args.Length > 1)
            {
                Console.WriteLine($"{args[0]} location changed to be {args[1]}");
            }
            else if (rawData.Contains("HTTP/1.0 200 OK\r\nContent-Type: text/plain\r\n"))
            {
                string[] data = rawData.Split("\r\n");
                Console.WriteLine($"{args[0]} is {data[data.Length-2]}");
            }
            else if (rawData.Contains("OK\r\n"))
            {
                Console.WriteLine($"{args[0]} location changed to be {args[1]}");
            }
            else
            {
                Console.WriteLine($"{args[0]} is {rawData}");
            }

        }
        catch (Exception e)
        {
            Console.WriteLine("Connection error");
            Console.WriteLine(e);
        }
    }


    static string[] GetAddress(string[] args, out string address)
    {
        address = "whois.net.dcs.hull.ac.uk";

        int addressLocation = Array.IndexOf(args, "-h");
        if (addressLocation > -1)
        {
            address = args[addressLocation + 1];
            args = args.Where((array, i) => i != addressLocation + 1).ToArray();
            args = args.Where((array, i) => i != addressLocation).ToArray();
        }
        return args;
    }
    static string[] GetPort(string[] args, out int port)
    {

        port = 43;

        int portLocation = Array.IndexOf(args, "-p");
        if (portLocation > -1)
        {
            if (int.TryParse(args[portLocation + 1], out int p))
            {
                port = p;
                args = args.Where((array, i) => i != portLocation + 1).ToArray();
                args = args.Where((array, i) => i != portLocation).ToArray();
            }
            else
            {
                throw new ArgumentException("Incorrect port");
            }
        }
        return args;
    }

    static string[] GetStyle(string[] args, out Style selectedStyle)
    {
        selectedStyle = Style.def;

        if (Array.Exists(args, flag => flag == "-h0"))
        {
            int position = Array.IndexOf(args, "-h0");
            args = args.Where((array, i) => i != position).ToArray();
            selectedStyle = Style.h0;
        }
        else if (Array.Exists(args, flag => flag == "-h1"))
        {
            int position = Array.IndexOf(args, "-h1");
            args = args.Where((array, i) => i != position).ToArray();
            selectedStyle = Style.h1;
        }
        if (Array.Exists(args, flag => flag == "-h9"))
        {
            int position = Array.IndexOf(args, "-h9");
            args = args.Where((array, i) => i != position).ToArray();
            selectedStyle = Style.h9;
        }

        return args;
    }

}