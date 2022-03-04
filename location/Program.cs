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
        Style selectedStyle;
        string request = null;
        string address;
        int port;
        string location = null;

        try
        {
            args = GetStyle(args, out selectedStyle);
            args = GetAddress(args, out address);
            args = GetTime(args, out int timeOut);
            args = GetPort(args, out port); // if user enter string throws error

            TcpClient client = new TcpClient();
            client.Connect(address, port);
            client.ReceiveTimeout = timeOut;
            client.SendTimeout = timeOut;
            StreamWriter sw = new StreamWriter(client.GetStream());
            StreamReader sr = new StreamReader(client.GetStream());

            //Checks if the user entered anything.
            if (args.Length == 0)
            {
                Console.WriteLine("ERROR: no arguments supplied");
            }
            else
            {
                //Concatenate the location if args contains it
                if (args.Length > 1)
                {
                    location = args[1];
                    for (int i = 2; i < args.Length; i++)
                    {
                        location += " " + args[i];
                    }
                }

                //-h1 means HTTP/1.1, -h0 means HTTP/1.0 and -h9 means HTTP/0.9 styles
                switch (selectedStyle)
                {
                    //h0 HTTP/1.0
                    case Style.h0:
                        if (args.Length == 1) //GET
                        {
                            request = ($"GET /?{args[0]} HTTP/1.0\r\n\r\n");
                        }
                        else if (args.Length > 1) //POST
                        {
                            int locationLength = location.Length;
                            request = ($"POST /{args[0]} HTTP/1.0\r\nContent-Length: {locationLength}\r\n\r\n{location}");
                        }
                        break;
                    //h1 HTTP/1.1
                    case Style.h1:
                        if (args.Length == 1) //GET
                        {
                            request = ($"GET /?name={args[0]} HTTP/1.1\r\nHost: {address}\r\n\r\n");
                        }
                        else if (args.Length > 1) //POST
                        {
                            string locationAndName = $"name={args[0]}&location={location}";
                            request = ($"POST / HTTP/1.1\r\nHost: {address}\r\nContent-Length: {locationAndName.Length}\r\n\r\n{locationAndName}");
                        }
                        break;
                    //h9 HTTP*0.9
                    case Style.h9:
                        if (args.Length == 1)
                        {
                            request = ($"GET /{args[0]}\r\n"); //GET
                        }
                        else if (args.Length > 1) //PUT
                        {
                            request = ($"PUT /{args[0]}\r\n\r\n{location}\r\n");
                        }
                        break;
                    //whois
                    case Style.def:
                        if (args.Length == 1) //GET
                        {
                            request = (args[0]+"\r\n");
                        }
                        else if (args.Length > 1) //SET
                        {
                            request = (args[0] + " " + location+"\r\n");
                        }
                        break;
                }

                sw.Write(request);
                sw.Flush();
            }

            //Removes special characters
            if (args.Length>1)
            {
                location = location.Trim(new Char[] { '\"', '\'', '`', '\\', '.' });
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


            string[] errorMessages =
            {
                "HTTP/1.1 404 Not Found\r\nContent-Type: text/plain\r\n\r\n\r\n",
                "HTTP/1.0 404 Not Found\r\nContent-Type: text/plain\r\n\r\n\r\n",
                "HTTP/0.9 404 Not Found\r\nContent-Type: text/plain\r\n\r\n\r\n",
                "ERROR: no entries found\r\n"
            };


            //Not Found, all protocols
            if (errorMessages.Contains(rawData))
            {
                Console.WriteLine(rawData);
            }
            //h9 HTTP/0.9
            else if (rawData.Contains("HTTP/0.9 200 OK\r\nContent-Type: text/plain\r\n\r\n") && args.Length > 1 && selectedStyle == Style.h9)
            {
                //GET
                if (args.Length == 1)
                {
                    string[] line = rawData.Trim().Split();
                    location = line[9];
                    for (int i = 10; i < line.Length; i++)
                    {
                        location += " " + line[i];
                    }
                    Console.WriteLine($"{args[0]} is {location}");
                }
                //PUT
                else 
                {
                    Console.WriteLine($"{args[0]} location changed to be {args[1]}");
                }
            }
            //h1 HTTP/1.1
            else if (rawData.Contains("HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\n\r\n") && selectedStyle == Style.h1)
            {
                //POST
                if (request.Contains("name=") && request.Contains("&location="))
                {
                    Console.WriteLine($"{args[0]} location changed to be {location}");
                }
                //GET
                else
                {
                    if (!html)
                    {
                        string[] line = rawData.Trim().Split();
                        int indexOfSpace = Array.IndexOf(line, "");

                        location = line[9];
                        for (int i = 10; i < line.Length; i++)
                        {
                            location += " " + line[i];
                        }
                        rawData = location;
                    }
                    Console.WriteLine($"{args[0]} is {rawData}");
                }
            }
            //h0 HTTP/1.0
            else if (rawData.Contains("HTTP/1.0 200 OK\r\nContent-Type: text/plain\r\n\r\n") && args.Length > 1 && selectedStyle == Style.h0)
            {
                //GET
                if (args.Length == 1)
                {
                    string[] data = rawData.Split("\r\n");
                    Console.WriteLine($"{args[0]} is {data[data.Length - 3]}");
                }
                //POST
                else 
                {
                    Console.WriteLine($"{args[0]} location changed to be {location}");
                }
            }
            //whois
            else if (rawData == "OK\r\n" && args.Length > 1 && selectedStyle == Style.def) //SET
            {
                Console.WriteLine($"{args[0]} location changed to be {location}");
            }
            else //GET
            {
                Console.WriteLine($"{args[0]} is {rawData}");
            }

        }
        catch (Exception e)
        {
            Console.WriteLine("Something went wrong with the connection:");
            Console.WriteLine(e);
        }
    }

    /// <summary>
    /// Sets the address.
    /// Default address is "whois.net.dcs.hull.ac.uk".
    /// If the array contains "-h" then changes the port then removes it from the array.
    /// </summary>
    /// <param name="args"></param>
    /// <param name="address"></param>
    /// <returns>An array without the address</returns>
    static string[] GetAddress(string[] args, out string address)
    {
        //default address
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

    /// <summary>
    /// Sets the port, the port by default is 43.
    /// If array contains "-p", then gets the port from an array, 
    /// then removes the protocol flag and the protocol.
    /// </summary>
    /// <param name="args">Command line argument</param>
    /// <param name="port">port number, throws error if string</param>
    /// <returns>The command line arg without the protocol and it's flag</returns>
    static string[] GetPort(string[] args, out int port)
    {
        //default port
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
                throw new ArgumentException($"Incorrect port: -p {args[portLocation + 1]}");
            }
        }
        return args;
    }

    /// <summary>
    /// Get the protocol from an array, then removes the protocol flag
    /// </summary>
    /// <param name="args"></param>
    /// <param name="selectedStyle"></param>
    /// <returns>Returns the array without the protocol flag</returns>
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
    /// <summary>
    /// Sets the timout time.
    /// Default timeout is 1000 (1 sec).
    /// If the array contains "-t" then sets it to 0.
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <param name="timeOut">time-out in millisec</param>
    /// <returns>The command line argument wihtout the "-t"</returns>
    private static string[] GetTime(string[] args, out int timeOut)
    {
        timeOut = 1000;

        if (args.Contains("-t"))
        {
            int position = Array.IndexOf(args, "-t");
            args = args.Where((array, i) => i != position).ToArray();
            timeOut = 0;
        }
        return args;
    }

}