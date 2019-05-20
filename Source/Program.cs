using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Collections;
using System.Diagnostics;
using System.Globalization;


namespace Fatumbot
{
    class Program
    {

        // public static Hashtable Attractors;
        public static Hashtable usessions;
        public static Hashtable Mnts = new Hashtable();
        public static Hashtable banned;
        public static Hashtable allchats;
        public static double[,] upresets = new double[1000, 5];
        public static int minappi = 100;
        public static int tmt = 10;
        public static int appikm = 100;
        public static int errs = 0;
        public static double deflat = 51.5084019;
        public static double deflon = -0.1278297;
        public static int radius = 5000;
        public static string teletoken;
        public static string ProxyURL;
        public static string Proxylogin;
        public static string Proxypass;
        public static int offset = 0;
        public static Thread thread1;
        public static bool DWD = true;
        public static bool rest = false;
        public static int trid = 0;
        public static bool isbusy = false;
        public static string rnsource = "QRNG";
        public static string mntskey;
        public static string logpath;

        public static NumberFormatInfo nfi = new NumberFormatInfo();
        

        public static string[] SplitIt1(string buf)
        {
            string[] seps = new string[] { "[", "]" };
            string[] buf1 = buf.Split(seps, StringSplitOptions.RemoveEmptyEntries);
            return buf1;
        }
        public static string[] SplitIt(string buf, string sep)
        {
            string[] seps = new string[] { sep, Environment.NewLine };
            string[] buf1 = buf.Split(seps, StringSplitOptions.RemoveEmptyEntries);
            return buf1;
        }

        public static void SetDefault(int u)
        {
            upresets[u, 0] = radius;
            upresets[u, 1] = deflat;
            upresets[u, 2] = deflon;
            upresets[u, 3] = 0;
            upresets[u, 4] = 1000;
        }

        public static void Updatecfg()
        {
            string cfgtxt = System.IO.File.ReadAllText("config.txt");
            string[] cfg = SplitIt1(cfgtxt);
            cfgtxt = cfg[0] + "[" + cfg[1] + "]" + cfg[2] + "[" + cfg[3] + "]" +
            cfg[4] + "[" + cfg[5] + "]"  + cfg[6] + "[" + cfg[7] + "]" +
            cfg[8] + "[" + appikm.ToString() + "]" + cfg[10] + "[" + tmt.ToString() + "]" +
            cfg[12] + "[" + rnsource + "]" + cfg[14] + "[" + mntskey + "]" +
             cfg[16] + "[" + logpath + "]";

            System.IO.File.WriteAllText("config.txt", cfgtxt);
        }

        public static bool IsProcessOpen(string name)
        {

            foreach (Process clsProcess in Process.GetProcesses())
            {
                if (clsProcess.ProcessName == name )
                {
                    return true;
                }
            }
            return false;
        }
        public static int GetDistance(double lat0, double lon0, double lat1, double lon1)
        {
            try
            {
                double dlon = (lon1 - lon0) * Math.PI / 180;
                double dlat = (lat1 - lat0) * Math.PI / 180;

                double a = (Math.Sin(dlat / 2) * Math.Sin(dlat / 2)) + Math.Cos(lat0 * Math.PI / 180) * Math.Cos(lat1 * Math.PI / 180) * (Math.Sin(dlon / 2) * Math.Sin(dlon / 2));
                double angle = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
                return Convert.ToInt32(angle * 6371000);
            }
            catch (Exception e) { Console.WriteLine("Distance calculation error with parameters: (" + lat0.ToString() + ", " + lon0.ToString() + ", " + lat1.ToString() + ", " + lon1.ToString() + ")"); return 0; }
        }

        public static double GetAzimut(double lat0, double lon0, double lat1, double lon1)
        {
            try
            {
                double dlon = ((lon1 - lon0) * (6371000 * Math.PI / 180));
                double dlat = ((lat1 - lat0) * (6371000 * Math.PI / 180));

                double azimut = (180 * Math.Atan((dlon * Math.Cos(lat0 * Math.PI / 180)) / dlat)) / Math.PI;
                if (azimut < 0) { azimut = azimut + 360; }

                int radius = GetDistance(lat0, lon0, lat1, lon1);
                double lat2 = lat0 + radius * Math.Cos(azimut * Math.PI / 180) / (6371000 * Math.PI / 180);
                double lon2 = lon0 + radius * Math.Sin(azimut * Math.PI / 180) / Math.Cos(lat0 * Math.PI / 180) / (6371000 * Math.PI / 180);
                int radius1 = GetDistance(lat1, lon1, lat2, lon2);
                if (radius < radius1) { azimut = azimut + 180; }

                return azimut;
            }
            catch (Exception e) { Console.WriteLine("Azimut calculation error: (" + lat0.ToString() + ", " + lon0.ToString() + ", " + lat1.ToString() + ", " + lon1.ToString() + ")"); return 0; }
        }

        public static double[] GetPseudoRandom(double lat, double lon, int radius)
        {
            double[] result = new double[2];
            Random rnd = new Random();


            try
            {
                bool dnn = false;
                while (dnn == false)
                {
                    double lat01 = lat + radius * Math.Cos(180 * Math.PI / 180) / (6371000 * Math.PI / 180);
                    double dlat = ((lat + radius / (6371000 * Math.PI / 180)) - lat01) * 1000000;
                    double lon01 = lon + radius * Math.Sin(270 * Math.PI / 180) / Math.Cos(lat * Math.PI / 180) / (6371000 * Math.PI / 180);
                    double dlon = ((lon + radius * Math.Sin(90 * Math.PI / 180) / Math.Cos(lat * Math.PI / 180) / (6371000 * Math.PI / 180)) - lon01) * 1000000;
                    double lat1 = lat;
                    double lon1 = lon;

                    double rlat = rnd.Next(0, (int)dlat);
                    double rlon = rnd.Next(0, (int)dlon);
                    lat1 = lat01 + (rlat / 1000000);
                    lon1 = lon01 + (rlon / 1000000);
                    int dif = GetDistance(lat, lon, lat1, lon1);
                    if (dif > radius) { }
                    else
                    {
                        result[0] = lat1;
                        result[1] = lon1;
                        dnn = true;
                    }
                }
                Console.WriteLine("Pseudorandom Link Created");


            }
            catch (Exception e) { Console.WriteLine("Pseudorandom point generation error: (" + lat.ToString() + ", " + lon.ToString() + ", " + radius.ToString()+")"); }
            return result;
        }

        public static double[] GetQuantumRandom(double lat, double lon, int radius)
        {
            double[] result = new double[2];
            QuantumRandomNumberGenerator rnd = new QuantumRandomNumberGenerator();
            REGRandomNumberGenerator rrnd = new REGRandomNumberGenerator();
            Random prnd = new Random();


            try
            {
                bool dnn = false;
                while (dnn == false)
                {
                    double lat01 = lat + radius * Math.Cos(180 * Math.PI / 180) / (6371000 * Math.PI / 180);
                    double dlat = ((lat + radius / (6371000 * Math.PI / 180)) - lat01) * 1000000;
                    double lon01 = lon + radius * Math.Sin(270 * Math.PI / 180) / Math.Cos(lat * Math.PI / 180) / (6371000 * Math.PI / 180);
                    double dlon = ((lon + radius * Math.Sin(90 * Math.PI / 180) / Math.Cos(lat * Math.PI / 180) / (6371000 * Math.PI / 180)) - lon01) * 1000000;
                    double lat1 = lat;
                    double lon1 = lon;
                    double rlat;
                    double rlon;

                    if (rnsource == "REG")
                    {
                        rlat = rrnd.Next(0, (int)dlat);
                        rlon = rrnd.Next(0, (int)dlon);
                    }
                       else
                        if (rnsource == "QRNG")
                       {
                           rlat = rnd.Next(0, (int)dlat);
                           rlon = rnd.Next(0, (int)dlon);
                       } 
                    else
                    {
                        rlat = prnd.Next(0, (int)dlat);
                        rlon = prnd.Next(0, (int)dlon);
                    }
                    lat1 = lat01 + (rlat / 1000000);
                    lon1 = lon01 + (rlon / 1000000);
                    int dif = GetDistance(lat, lon, lat1, lon1);
                    if (dif > radius) { }
                    else
                    {
                        result[0] = lat1;
                        result[1] = lon1;
                        dnn = true;
                    }
                }
                Console.WriteLine("Quantum Link Created");


            }
            catch (Exception e) { Console.WriteLine("QR Point generation error (" + lat.ToString() + ", " + lon.ToString() + ", " + radius.ToString() + ")" + Environment.NewLine 
                + e.Message.ToString()); }
            return result;
        }

        public static double[] GetAverageCoord(Hashtable Attractors)
        {

            double[] result = new double[2];
            int cou = 0;
            double latav = 0;
            double lonav = 0;

            try
            {
                foreach (DictionaryEntry coord in Attractors)
                {
                    string[] coords = SplitIt(coord.Key.ToString(), ":");
                    latav += double.Parse(coords[0], System.Globalization.CultureInfo.InvariantCulture);
                    lonav += double.Parse(coords[1], System.Globalization.CultureInfo.InvariantCulture);
                    cou++;
                }

                result[0] = latav / cou;
                result[1] = lonav / cou;
            }
            catch (Exception e) { Console.WriteLine("Average coordinate calculation error"); }
            return result;

        }


        public static double[] GetMirrorCoord(double lat1, double lon1, double lat2, double lon2)
        {
            double[] result = new double[2];
            try
            {
                int dis = GetDistance(lat1, lon1, lat2, lon2);
                double azimut = GetAzimut(lat1, lon1, lat2, lon2);
                if (azimut >= 180) { azimut = azimut - 180; }
                else
                if (azimut < 180) { azimut = azimut + 180; }

                result[0] = lat1 + dis * Math.Cos(azimut * Math.PI / 180) / (6371000 * Math.PI / 180);
                result[1] = lon1 + dis * Math.Sin(azimut * Math.PI / 180) / Math.Cos(lat1 * Math.PI / 180) / (6371000 * Math.PI / 180);
            }
            catch (Exception e) { Console.WriteLine("Mirror coordinate calculation error"); }
            return result;
        }


        public static Hashtable FillTable(double lat, double lon, int radius, double APPI)
        {
            Hashtable Attractors = new Hashtable();
            QuantumRandomNumberGenerator rnd = new QuantumRandomNumberGenerator();
            REGRandomNumberGenerator rrnd = new REGRandomNumberGenerator();
            Random prnd = new Random();

            if (appikm != 0)
            {
                APPI = (radius * appikm) / 1000;
                if (APPI < minappi) { APPI = minappi; }
            }


            try
            {
                double lat01 = lat + radius * Math.Cos(180 * Math.PI / 180) / (6371000 * Math.PI / 180);
                double dlat = ((lat + radius / (6371000 * Math.PI / 180)) - lat01) * 1000000;
                double lon01 = lon + radius * Math.Sin(270 * Math.PI / 180) / Math.Cos(lat * Math.PI / 180) / (6371000 * Math.PI / 180);
                double dlon = ((lon + radius * Math.Sin(90 * Math.PI / 180) / Math.Cos(lat * Math.PI / 180) / (6371000 * Math.PI / 180)) - lon01) * 1000000;
                double lat1 = lat;
                double lon1 = lon;

                while (Attractors.Count < APPI)
                {
                    int[,] rands;
                    if (rnsource == "QRNG")
                    { rands = rnd.NextCoord((int)dlat, (int)dlon, ((int)APPI - Attractors.Count + 10)); }
                    else
                    if (rnsource == "REG")
                    { rands = rrnd.NextCoord((int)dlat, (int)dlon, ((int)APPI - Attractors.Count + 10)); }
                    else
                    {
                        rands = new int[((int)APPI - Attractors.Count + 10), 2];
                        for (int i = 0; i < ((int)APPI - Attractors.Count + 10); i++)
                        {
                            int rnd1 = prnd.Next((int)dlat);
                            rands[i, 0] = rnd1;
                            rnd1 = prnd.Next((int)dlon);
                            rands[i, 1] = rnd1;
                        }
                    }
                    int apos = 0;
                    string result = "";

                    while ((Attractors.Count < APPI) && (apos < rands.GetLength(0)))
                    {
                        double rlat = rands[apos, 0];
                        double rlon = rands[apos, 1];
                        apos++;
                        lat1 = lat01 + (rlat / 1000000);
                        lon1 = lon01 + (rlon / 1000000);
                        int dif = GetDistance(lat, lon, lat1, lon1);
                        result = lat1.ToString("#0.0000000", System.Globalization.CultureInfo.InvariantCulture) + ":" + lon1.ToString("#0.0000000", System.Globalization.CultureInfo.InvariantCulture);
                        if ((dif > radius) || (Attractors.ContainsKey(result) == true))
                        { }
                        else
                        {
                            Attractors.Add(result, 1);
                        }
                    }

                }
                System.Threading.Thread.Sleep(5000);
            }
            catch (Exception e) { Console.WriteLine("Error occured during random coordinates array generation" + Environment.NewLine + e.Message.ToString()); }

            return Attractors;
        }


        public static double[] TestPoint(double lat, double lon, Hashtable Testpoints, int radius)
        {

            Hashtable temp = new Hashtable();
            double[] ardis = new double[Testpoints.Count];
            double[] np = new double[3]; // 0 - nearest dis; 1 - aproximate radius, 2 - relative density
            np[1] = 50;
            np[0] = radius;
            double sum = 0;
            int pts = 0;
            try
            {
                int cou = 0;
                foreach (DictionaryEntry coord in Testpoints)
                {
                    string[] coords = SplitIt(coord.Key.ToString(), ":");
                    double lat2 = double.Parse(coords[0], System.Globalization.CultureInfo.InvariantCulture);
                    double lon2 = double.Parse(coords[1], System.Globalization.CultureInfo.InvariantCulture);
                    int dis = GetDistance(lat, lon, lat2, lon2);
                    if (dis < np[0]) { np[0] = dis; }
                    ardis[cou] = dis; cou++;
                }

                np[1] = np[0] * 2;

                while (pts < 10)
                {
                    sum = 0; pts = 0;
                    for (int i = 0; i < ardis.Count(); i++)
                    {
                        if (ardis[i] <= np[1]) { sum += ardis[i]; pts++; }
                    }
                    if (pts < 10) { np[1] += np[0]; }
                }
                np[1] = sum / pts;
                sum = 0; pts = 0;
                for (int i = 0; i < ardis.Count(); i++)
                {
                    if (ardis[i] <= np[1]) { sum++; }
                }

                np[2] = (sum * radius * radius) / (Testpoints.Count * np[1] * np[1]);
            }
            catch (Exception e) { Console.WriteLine("Coordinate correctness checking error " + Environment.NewLine + e.Message.ToString()); }
            return np;
        }

        public static double[] GetQuantumPair(double lat, double lon, int radius, double appi)
        {
            double[] result = new double[7]; // 0123 - coords; 456 - analytics
            try
            {
                int rd = radius;
                Hashtable Basepoints = FillTable(lat, lon, radius, appi);
                Hashtable Attractors = new Hashtable();
                Hashtable Repellers = new Hashtable();
                double[] avecoord = GetAverageCoord(Basepoints);
                double[] mirrorcoord = new double[2];

                foreach (DictionaryEntry coord in Basepoints)
                {
                    Repellers.Add(coord.Key, coord.Value);
                    Attractors.Add(coord.Key, coord.Value);
                }


                while ((rd > 50) && (Attractors.Count > 1))
                {

                    Hashtable temp = new Hashtable();
                    avecoord = GetAverageCoord(Attractors);
                    foreach (DictionaryEntry coord in Attractors)
                    {
                        temp.Add(coord.Key, coord.Value);
                    }
                    rd = rd - 1;  //1 meter step
                    foreach (DictionaryEntry coord in temp)
                    {

                        string[] coords = SplitIt(coord.Key.ToString(), ":");
                        double lat2 = double.Parse(coords[0], System.Globalization.CultureInfo.InvariantCulture);
                        double lon2 = double.Parse(coords[1], System.Globalization.CultureInfo.InvariantCulture);
                        int dis = GetDistance(avecoord[0], avecoord[1], lat2, lon2);
                        if (dis > rd)
                        {
                            Attractors.Remove(coord.Key);
                        }
                    }

                }

                Console.WriteLine(Environment.NewLine + "Attractor generated");
                result[0] = avecoord[0];
                result[1] = avecoord[1];
                double[] np = TestPoint(avecoord[0], avecoord[1], Basepoints, radius);
                result[4] = np[2];


                rd = radius;
                while ((rd > 50) && (Repellers.Count > 1))
                {

                    Hashtable temp = new Hashtable();
                    avecoord = GetAverageCoord(Repellers);
                    mirrorcoord = GetMirrorCoord(lat, lon, avecoord[0], avecoord[1]);
                    foreach (DictionaryEntry coord in Repellers)
                    {
                        temp.Add(coord.Key, coord.Value);
                    }
                    int dis2 = GetDistance(mirrorcoord[0], mirrorcoord[1], lat, lon);
                    rd = rd - 1 - dis2;  //1 meter step

                    foreach (DictionaryEntry coord in temp)
                    {
                        string[] coords = SplitIt(coord.Key.ToString(), ":");
                        double lat2 = double.Parse(coords[0], System.Globalization.CultureInfo.InvariantCulture);
                        double lon2 = double.Parse(coords[1], System.Globalization.CultureInfo.InvariantCulture);
                        int dis = GetDistance(mirrorcoord[0], mirrorcoord[1], lat2, lon2);
                        if (dis > rd)
                        {
                            Repellers.Remove(coord.Key);
                        }
                    }
                    lat = mirrorcoord[0];
                    lon = mirrorcoord[1];
                }

                Console.WriteLine(Environment.NewLine + "Repeller generated");
                result[2] = mirrorcoord[0];
                result[3] = mirrorcoord[1];
                np = TestPoint(mirrorcoord[0], mirrorcoord[1], Basepoints, radius);
                result[5] = np[2];
                result[6] = np[1];
            }
            catch (Exception e) { Console.WriteLine("Pair Generation error (" + lat.ToString() + ", " + lon.ToString() + ", " + radius.ToString() +")"+ Environment.NewLine + e.Message.ToString()); }
            return result;
        }

        public static double[] GetQuantumRepeller(double lat, double lon, int radius, double appi)
        {
            double[] result = new double[4];
            try
            {
                int rd = radius;
                Hashtable Repellers = FillTable(lat, lon, radius, appi);
                double[] avecoord = GetAverageCoord(Repellers);
                double[] mirrorcoord = new double[2];
                Hashtable Testpoints = new Hashtable();
                foreach (DictionaryEntry coord in Repellers)
                {
                    Testpoints.Add(coord.Key, coord.Value);
                }

                while ((rd > 50) && (Repellers.Count > 1))
                {

                    Hashtable temp = new Hashtable();
                    avecoord = GetAverageCoord(Repellers);
                    mirrorcoord = GetMirrorCoord(lat, lon, avecoord[0], avecoord[1]);
                    foreach (DictionaryEntry coord in Repellers)
                    {
                        temp.Add(coord.Key, coord.Value);
                    }
                    int dis2 = GetDistance(mirrorcoord[0], mirrorcoord[1], lat, lon);
                    rd = rd - 1 - dis2;  //1 meter step

                    foreach (DictionaryEntry coord in temp)
                    {
                        string[] coords = SplitIt(coord.Key.ToString(), ":");
                        double lat2 = double.Parse(coords[0], System.Globalization.CultureInfo.InvariantCulture);
                        double lon2 = double.Parse(coords[1], System.Globalization.CultureInfo.InvariantCulture);
                        int dis = GetDistance(mirrorcoord[0], mirrorcoord[1], lat2, lon2);
                        if (dis > rd)
                        {
                            Repellers.Remove(coord.Key);
                        }
                    }
                    lat = mirrorcoord[0];
                    lon = mirrorcoord[1];
                }

                Console.WriteLine(Environment.NewLine + "Repeller generated");
                result[0] = mirrorcoord[0];
                result[1] = mirrorcoord[1];
                double[] np = TestPoint(mirrorcoord[0], mirrorcoord[1], Testpoints, radius);
                result[2] = np[2];
                result[3] = np[1];
            }
            catch (Exception e) { Console.WriteLine("Repeller generation error (" + lat.ToString() + ", " + lon.ToString() + ", " + radius.ToString() + ")"
                + Environment.NewLine + e.Message.ToString()); }
            return result;
        }




        public static double[] GetQuantumAttractor(double lat, double lon, int radius, double appi)
        {
            double[] result = new double[3];
            try
            {
                int rd = radius;
                Hashtable Attractors = FillTable(lat, lon, radius, appi);
                double[] avecoord = GetAverageCoord(Attractors);

                Hashtable Testpoints = new Hashtable();
                foreach (DictionaryEntry coord in Attractors)
                {
                    Testpoints.Add(coord.Key, coord.Value);
                }

                while ((radius > 50) && (Attractors.Count > 1))
                {

                    Hashtable temp = new Hashtable();
                    avecoord = GetAverageCoord(Attractors);
                    foreach (DictionaryEntry coord in Attractors)
                    {
                        temp.Add(coord.Key, coord.Value);
                    }
                    rd = rd - 1;  //1 meter step
                    foreach (DictionaryEntry coord in temp)
                    {

                        string[] coords = SplitIt(coord.Key.ToString(), ":");
                        double lat2 = double.Parse(coords[0], System.Globalization.CultureInfo.InvariantCulture);
                        double lon2 = double.Parse(coords[1], System.Globalization.CultureInfo.InvariantCulture);
                        int dis = GetDistance(avecoord[0], avecoord[1], lat2, lon2);
                        if (dis > rd)
                        {
                            Attractors.Remove(coord.Key);
                        }
                    }

                }

                Console.WriteLine(Environment.NewLine + "Attractor generated");
                result[0] = avecoord[0];
                result[1] = avecoord[1];
                double[] np = TestPoint(avecoord[0], avecoord[1], Testpoints, radius);
                result[2] = np[2];
            }
            catch (Exception e) { Console.WriteLine("Attractor generation error (" + lat.ToString() + ", " + lon.ToString() + ", " + radius.ToString() + ")"
                + Environment.NewLine + e.Message.ToString()); }
            return result;
        }


        static private async Task bw_DoWorkTask(TelegramBotClient Bot, Message message)
        {
            while (isbusy) { }
            if (rnsource == "REG") { isbusy = true; }

            if ((message.Type == Telegram.Bot.Types.Enums.MessageType.Location) && (banned.ContainsKey(message.Chat.Id.ToString()) == false))
            {
                //set location for user
                try
                {
                    int u;
                    if (usessions.ContainsKey(message.Chat.Id) == false)
                    {
                        u = usessions.Count;
                        usessions.Add(message.Chat.Id, u);
                        SetDefault(u);
                    }
                    u = (int)usessions[message.Chat.Id];
                    upresets[u, 1] = (double)message.Location.Latitude;
                    upresets[u, 2] = (double)message.Location.Longitude;
                   try { await Bot.SendTextMessageAsync(message.Chat.Id, "New location confirmed."); } catch (Exception ex) { }

                    //logging
                    string buf = "";
                    buf += @"http://wikimapia.org/#lang=ru&lat=";
                    buf += upresets[u, 1].ToString("#0.000000", System.Globalization.CultureInfo.InvariantCulture) +
                    "&lon=" + upresets[u, 2].ToString("#0.000000", System.Globalization.CultureInfo.InvariantCulture) + "&z=19&m=b";

                    string buf1 = "";
                    if (message.From.FirstName != null) { buf1 = message.From.FirstName.ToString() + " "; }
                    if (message.From.LastName != null) { buf1 += message.From.FirstName.ToString() + " "; }
                    if (message.From.Username != null) { buf1 += " (" + message.From.Username.ToString() + ") "; }

                    System.IO.File.AppendAllText(logpath, message.Date.ToString() + " " + message.From.Id.ToString() + " " + buf1
                        + "changed current location: " + buf + Environment.NewLine);
                    //logging
                }
                catch (Exception e) { Console.WriteLine("location setting error " + e.Message.ToString()); }

            }
            else if ((message.Type == Telegram.Bot.Types.Enums.MessageType.Text) && (banned.ContainsKey(message.Chat.Id.ToString()) == false))
            {
                if ((message.Text == "/getpseudo"))
                {
                    double[] incoords = new double[2];
                    try
                    {
                        if (usessions.ContainsKey(message.Chat.Id) == false)
                        {
                            int u = usessions.Count;
                            usessions.Add(message.Chat.Id, u);
                            SetDefault(u);
                        }

                        double tmplat = upresets[(int)usessions[message.Chat.Id], 1];
                        double tmplon = upresets[(int)usessions[message.Chat.Id], 2];
                        int tmprad = (int)upresets[(int)usessions[message.Chat.Id], 0];
                        incoords = GetPseudoRandom(tmplat, tmplon, tmprad);
                       try { await Bot.SendLocationAsync(message.Chat.Id, (float)incoords[0], (float)incoords[1]); } catch (Exception ex) { }
                        //logging
                        string buf = "";
                        buf += @"http://wikimapia.org/#lang=ru&lat=";
                        buf += incoords[0].ToString("#0.000000", System.Globalization.CultureInfo.InvariantCulture) +
                        "&lon=" + incoords[1].ToString("#0.000000", System.Globalization.CultureInfo.InvariantCulture) + "&z=19&m=b";

                        string buf1 = "";
                        if (message.From.FirstName != null) { buf1 = message.From.FirstName.ToString() + " "; }
                        if (message.From.LastName != null) { buf1 += message.From.FirstName.ToString() + " "; }
                        if (message.From.Username != null) { buf1 += " (" + message.From.Username.ToString() + ") "; }

                        System.IO.File.AppendAllText(logpath, message.Date.ToString() + " " + message.From.Id.ToString() + " " + buf1
                            + "created pseudorandom point: " + buf + Environment.NewLine);
                        //logging
                    }
                    catch (Exception e) { Console.WriteLine("getpseudo Command processing error" + Environment.NewLine + e.Message.ToString()); }
                }

                else if ((message.Text == "/getquantum"))
                {
                    double[] incoords = new double[2];
                    try
                    {
                        if (usessions.ContainsKey(message.Chat.Id) == false)
                        {
                            int u = usessions.Count;
                            usessions.Add(message.Chat.Id, u);
                            SetDefault(u);
                        }

                        double tmplat = upresets[(int)usessions[message.Chat.Id], 1];
                        double tmplon = upresets[(int)usessions[message.Chat.Id], 2];
                        int tmprad = (int)upresets[(int)usessions[message.Chat.Id], 0];
                        incoords = GetQuantumRandom(tmplat, tmplon, tmprad);

                        bool issent = false;
                        int rtr = 0;
                        while ((issent == false) && (rtr < 5))
                        {
                            try
                            {
                                await Bot.SendLocationAsync(message.Chat.Id, (float)incoords[0], (float)incoords[1]);
                                issent = true;
                            }
                            catch (Exception e) { issent = false; rtr++; }
                        }

                        //logging
                        string buf = "";
                        buf += @"http://wikimapia.org/#lang=ru&lat=";
                        buf += incoords[0].ToString("#0.000000", System.Globalization.CultureInfo.InvariantCulture) +
                        "&lon=" + incoords[1].ToString("#0.000000", System.Globalization.CultureInfo.InvariantCulture) + "&z=19&m=b";

                        string buf1 = "";
                        if (message.From.FirstName != null) { buf1 = message.From.FirstName.ToString() + " "; }
                        if (message.From.LastName != null) { buf1 += message.From.FirstName.ToString() + " "; }
                        if (message.From.Username != null) { buf1 += " (" + message.From.Username.ToString() + ") "; }

                        System.IO.File.AppendAllText(logpath, message.Date.ToString() + " " + message.From.Id.ToString() + " " + buf1
                                + "created quantum random point: " + buf + Environment.NewLine);
                        //logging
                    }
                    catch (Exception e) { Console.WriteLine("getquantum command processing error " + Environment.NewLine + e.Message.ToString()); }
                }
                else if ((message.Text == "/getattractor"))
                {
                    double[] incoords = new double[3];
                    try
                    {
                        if (usessions.ContainsKey(message.Chat.Id) == false)
                        {
                            int u = usessions.Count;
                            usessions.Add(message.Chat.Id, u);
                            SetDefault(u);
                        }


                        try { await Bot.SendTextMessageAsync(message.Chat.Id, "Wait a minute. It will take a while."); } catch (Exception ex) { }


                        double appi = upresets[(int)usessions[message.Chat.Id], 4];
                        double tmplat = upresets[(int)usessions[message.Chat.Id], 1];
                        double tmplon = upresets[(int)usessions[message.Chat.Id], 2];
                        int tmprad = (int)upresets[(int)usessions[message.Chat.Id], 0];
                        incoords = GetQuantumAttractor(tmplat, tmplon, tmprad, appi);

                        string mesg = "something wrong";
                        if (incoords[2] < 1.3) { mesg = "Attractor is invalid! power: " + incoords[2].ToString("#0.00", System.Globalization.CultureInfo.InvariantCulture); }
                        else if (incoords[2] >= 2) { mesg = "Attractor generated. power: " + incoords[2].ToString("#0.00", System.Globalization.CultureInfo.InvariantCulture); }
                        else { mesg = "Attractor generated. power: " + incoords[2].ToString("#0.00", System.Globalization.CultureInfo.InvariantCulture) + " (Weak)"; }

                        bool issent = false;
                        int rtr = 0;
                        while ((issent == false) && (rtr < 5))
                        {
                            try
                            {
                                await Bot.SendTextMessageAsync(message.Chat.Id, mesg);

                                await Bot.SendLocationAsync(message.Chat.Id, (float)incoords[0], (float)incoords[1]);
                                issent = true;
                            }
                            catch (Exception e) { issent = false; rtr++; }
                        }

                        //logging
                        string buf = "";
                        buf += @"http://wikimapia.org/#lang=ru&lat=";
                        buf += incoords[0].ToString("#0.000000", System.Globalization.CultureInfo.InvariantCulture) +
                        "&lon=" + incoords[1].ToString("#0.000000", System.Globalization.CultureInfo.InvariantCulture) + "&z=19&m=b";

                        string buf1 = "";
                        if (message.From.FirstName != null) { buf1 = message.From.FirstName.ToString() + " "; }
                        if (message.From.LastName != null) { buf1 += message.From.FirstName.ToString() + " "; }
                        if (message.From.Username != null) { buf1 += " (" + message.From.Username.ToString() + ") "; }

                        System.IO.File.AppendAllText(logpath, message.Date.ToString() + " " + message.From.Id.ToString() + " " + buf1
                                + "created attractor point: " + buf + Environment.NewLine + mesg + Environment.NewLine);
                        //logging

                    }
                    catch (Exception e) { Console.WriteLine("getattractor command processing error" + Environment.NewLine + e.Message.ToString()); }
                }
                else if ((message.Text == "/getrepeller")|| (message.Text == "/getvoid"))
                {
                    double[] incoords = new double[4];
                    try
                    {
                        if (usessions.ContainsKey(message.Chat.Id) == false)
                        {
                            int u = usessions.Count;
                            usessions.Add(message.Chat.Id, u);
                            SetDefault(u);
                        }

                        try { await Bot.SendTextMessageAsync(message.Chat.Id, "Wait a minute. It will take a while."); } catch (Exception ex) { }
                        double appi = upresets[(int)usessions[message.Chat.Id], 4];
                        double tmplat = upresets[(int)usessions[message.Chat.Id], 1];
                        double tmplon = upresets[(int)usessions[message.Chat.Id], 2];
                        int tmprad = (int)upresets[(int)usessions[message.Chat.Id], 0];
                        incoords = GetQuantumRepeller(tmplat, tmplon, tmprad, appi);

                        string mesg = "something wrong";
                        if (incoords[2] >= 0.9)
                        {
                            mesg = "Void Attractor is invalid! power: " + (1/incoords[2]).ToString("#0.00", System.Globalization.CultureInfo.InvariantCulture)
  + " radius: " + incoords[3].ToString("#0.00", System.Globalization.CultureInfo.InvariantCulture) + " meters";
                        }
                        else if (incoords[2] < 0.6)
                        {
                            mesg = "Void Attractor generated. power: " + (1 / incoords[2]).ToString("#0.00", System.Globalization.CultureInfo.InvariantCulture)
+ " radius: " + incoords[3].ToString("#0.00", System.Globalization.CultureInfo.InvariantCulture) + " meters";
                        }
                        else
                        {
                            mesg = "Void Attractor generated. power: " + (1 / incoords[2]).ToString("#0.00", System.Globalization.CultureInfo.InvariantCulture) + " (Weak) "
                        + " radius: " + incoords[3].ToString("#0.00", System.Globalization.CultureInfo.InvariantCulture) + " meters";
                        }

                        bool issent = false;
                        int rtr = 0;
                        while ((issent == false) && (rtr < 5))
                        {
                            try
                            {
                                await Bot.SendTextMessageAsync(message.Chat.Id, mesg);

                                await Bot.SendLocationAsync(message.Chat.Id, (float)incoords[0], (float)incoords[1]);
                                issent = true;
                            }
                            catch (Exception e) { issent = false; rtr++; }
                        }

                        //logging
                        string buf = "";
                        buf += @"http://wikimapia.org/#lang=ru&lat=";
                        buf += incoords[0].ToString("#0.000000", System.Globalization.CultureInfo.InvariantCulture) +
                        "&lon=" + incoords[1].ToString("#0.000000", System.Globalization.CultureInfo.InvariantCulture) + "&z=19&m=b";

                        string buf1 = "";
                        if (message.From.FirstName != null) { buf1 = message.From.FirstName.ToString() + " "; }
                        if (message.From.LastName != null) { buf1 += message.From.FirstName.ToString() + " "; }
                        if (message.From.Username != null) { buf1 += " (" + message.From.Username.ToString() + ") "; }

                        System.IO.File.AppendAllText(logpath, message.Date.ToString() + " " + message.From.Id.ToString() + " " + buf1
                                + "created Void Attractor point: " + buf + Environment.NewLine + mesg + Environment.NewLine);
                        //logging
                    }
                    catch (Exception e) { Console.WriteLine("getvoid command processing error" + Environment.NewLine + e.Message.ToString()); }
                }
                else if ((message.Text == "/getpair"))
                {
                    double[] incoords = new double[7];
                    try
                    {
                        if (usessions.ContainsKey(message.Chat.Id) == false)
                        {
                            int u = usessions.Count;
                            usessions.Add(message.Chat.Id, u);
                            SetDefault(u);
                        }

                       try { await Bot.SendTextMessageAsync(message.Chat.Id, "Wait a minute. It will take a while."); } catch (Exception ex) { }
                        double appi = upresets[(int)usessions[message.Chat.Id], 4];
                        double tmplat = upresets[(int)usessions[message.Chat.Id], 1];
                        double tmplon = upresets[(int)usessions[message.Chat.Id], 2];
                        int tmprad = (int)upresets[(int)usessions[message.Chat.Id], 0];
                        incoords = GetQuantumPair(tmplat, tmplon, tmprad, appi);
                        string mesg1 = "something wrong";
                        if (incoords[4] < 1.3) { mesg1 = "Attractor is invalid! power: " + incoords[4].ToString("#0.00", System.Globalization.CultureInfo.InvariantCulture); }
                        else if (incoords[4] >= 2) { mesg1 = "Attractor generated. power: " + incoords[4].ToString("#0.00", System.Globalization.CultureInfo.InvariantCulture); }
                        else { mesg1 = "Attractor generated. power: " + incoords[4].ToString("#0.00", System.Globalization.CultureInfo.InvariantCulture) + " (Weak)"; }

                        bool issent = false;
                        int rtr = 0;
                        while ((issent == false) && (rtr < 5))
                        {
                            try
                            {
                                await Bot.SendTextMessageAsync(message.Chat.Id, mesg1);

                                await Bot.SendLocationAsync(message.Chat.Id, (float)incoords[0], (float)incoords[1]);
                                issent = true;
                            }
                            catch (Exception e) { issent = false; rtr++; }
                        }

                        string mesg2 = "something wrong";
                        if (incoords[5] >= 0.9)
                        {
                            mesg2 = "Void Attractor is invalid! power: " + (1 / incoords[5]).ToString("#0.00", System.Globalization.CultureInfo.InvariantCulture)
 + " radius: " + incoords[6].ToString("#0.00", System.Globalization.CultureInfo.InvariantCulture) + " meters";
                        }
                        else if (incoords[5] < 0.6)
                        {
                            mesg2 = "Void Attractor generated. power: " + (1 / incoords[5]).ToString("#0.00", System.Globalization.CultureInfo.InvariantCulture)
+ " radius: " + incoords[6].ToString("#0.00", System.Globalization.CultureInfo.InvariantCulture) + " meters";
                        }
                        else
                        {
                            mesg2 = "Void Attractor generated. power: " + (1 / incoords[5]).ToString("#0.00", System.Globalization.CultureInfo.InvariantCulture) + " (Weak) "
                        + " radius: " + incoords[6].ToString("#0.00", System.Globalization.CultureInfo.InvariantCulture) + " meters";
                        }
                        issent = false;
                        rtr = 0;
                        while ((issent == false) && (rtr < 5))
                        {
                            try
                            {
                                await Bot.SendTextMessageAsync(message.Chat.Id, mesg2);

                                await Bot.SendLocationAsync(message.Chat.Id, (float)incoords[2], (float)incoords[3]);
                                issent = true;
                            }
                            catch (Exception e) { issent = false; rtr++; }
                        }

                        //logging
                        string buf = "";
                        buf += @"http://wikimapia.org/#lang=ru&lat=";
                        buf += incoords[0].ToString("#0.000000", System.Globalization.CultureInfo.InvariantCulture) +
                        "&lon=" + incoords[1].ToString("#0.000000", System.Globalization.CultureInfo.InvariantCulture) + "&z=19&m=b" + Environment.NewLine;
                        buf += @"http://wikimapia.org/#lang=ru&lat=";
                        buf += incoords[2].ToString("#0.000000", System.Globalization.CultureInfo.InvariantCulture) +
                        "&lon=" + incoords[3].ToString("#0.000000", System.Globalization.CultureInfo.InvariantCulture) + "&z=19&m=b";

                        string buf1 = "";
                        if (message.From.FirstName != null) { buf1 = message.From.FirstName.ToString() + " "; }
                        if (message.From.LastName != null) { buf1 += message.From.FirstName.ToString() + " "; }
                        if (message.From.Username != null) { buf1 += " (" + message.From.Username.ToString() + ") "; }

                        System.IO.File.AppendAllText(logpath, message.Date.ToString() + " " + message.From.Id.ToString() + " " + buf1
                                + "created positive-negative attractor pair: " + buf + Environment.NewLine + mesg1 + Environment.NewLine + mesg2 + Environment.NewLine);
                        //logging
                    }
                    catch (Exception e) { Console.WriteLine("getpair command processing error" + Environment.NewLine + e.Message.ToString()); }
                }
                else if ((message.Text == "/setradius"))
                {
                    try
                    {
                        if (usessions.ContainsKey(message.Chat.Id) == false)
                        {
                            int u = usessions.Count;
                            usessions.Add(message.Chat.Id, u);
                            SetDefault(u);
                        }
                        bool issent = false;
                        int rtr = 0;
                        while ((issent == false) && (rtr < 5))
                        {
                            try
                            {
                                await Bot.SendTextMessageAsync(message.Chat.Id, "Send new radius in meters (for example 3000)");
                                issent = true;
                            }
                            catch (Exception e) { issent = false; rtr++; }
                        }
                        upresets[(int)usessions[message.Chat.Id], 3] = 1;

                    }
                    catch (Exception e) { Console.WriteLine("setradius command processing error" + Environment.NewLine + e.Message.ToString()); }
                }
                else if ((message.Text.Contains("setradius ")))
                {
                    try
                    {
                        if (usessions.ContainsKey(message.Chat.Id) == false)
                        {
                            int u = usessions.Count;
                            usessions.Add(message.Chat.Id, u);
                            SetDefault(u);
                        }
                        string nr = message.Text;
                        string nrd = @"/setradius ";
                    nr = nr.Replace(nrd, "");
                    int tmprad = 3000;
                    if (Int32.TryParse(nr, out tmprad))
                    {
                        if (tmprad > 1000000) { try { await Bot.SendTextMessageAsync(message.Chat.Id, "Maximum radius is 1000000 m"); } catch (Exception ex) { } }
                        else if (tmprad < 1000) { try { await Bot.SendTextMessageAsync(message.Chat.Id, "Minimum radius is 1000 m"); } catch (Exception ex) { } }
                        else
                        {
                            {
                                upresets[(int)usessions[message.Chat.Id], 4] = (tmprad * appikm) / 1000;
                                if (upresets[(int)usessions[message.Chat.Id], 4] < minappi) { upresets[(int)usessions[message.Chat.Id], 4] = minappi; }
                                upresets[(int)usessions[message.Chat.Id], 3] = 0;
                                upresets[(int)usessions[message.Chat.Id], 0] = tmprad;
                                bool issent = false;
                                int rtr = 0;
                                while ((issent == false) && (rtr < 5))
                                {
                                    try
                                    {
                                        await Bot.SendTextMessageAsync(message.Chat.Id, "Radius changed.");
                                        issent = true;
                                    }
                                    catch (Exception e) { issent = false; rtr++; }
                                }
                            }
                          
                }

                    } else { try { await Bot.SendTextMessageAsync(message.Chat.Id, "Incorrect value."); } catch (Exception ex) { } }
                    //logging 
                    string buf1 = "";
                    if (message.From.FirstName != null) { buf1 = message.From.FirstName.ToString() + " "; }
                    if (message.From.LastName != null) { buf1 += message.From.FirstName.ToString() + " "; }
                    if (message.From.Username != null) { buf1 += " (" + message.From.Username.ToString() + ") "; }

                    System.IO.File.AppendAllText(logpath, message.Date.ToString() + " " + message.From.Id.ToString() + " " + buf1
                           + "changed radius: " + tmprad.ToString() + Environment.NewLine);
                        //logging
                    }
                    catch (Exception e) { Console.WriteLine("radius setting error " + Environment.NewLine + e.Message.ToString()); }
                }
                else if ((message.Text == "/setdefault"))
                {
                    try
                    {
                        if (usessions.ContainsKey(message.Chat.Id) == false)
                        {
                            int u = usessions.Count;
                            usessions.Add(message.Chat.Id, u);
                            SetDefault(u);
                        }
                        else { SetDefault((int)usessions[message.Chat.Id]); }

                        try {  await Bot.SendTextMessageAsync(message.Chat.Id, "Reset completed."); } catch (Exception ex) { }
                        //logging
                        string buf1 = "";
                        if (message.From.FirstName != null) { buf1 = message.From.FirstName.ToString() + " "; }
                        if (message.From.LastName != null) { buf1 += message.From.FirstName.ToString() + " "; }
                        if (message.From.Username != null) { buf1 += " (" + message.From.Username.ToString() + ") "; }

                        System.IO.File.AppendAllText(logpath, message.Date.ToString() + " " + message.From.Id.ToString() + " " + buf1
                               + "reset to defaults" + Environment.NewLine);
                        //logging

                    }
                    catch (Exception e) { Console.WriteLine("setdefault command processing error" + Environment.NewLine + e.Message.ToString()); }
                }
                else if (message.Text == "/test")
                {

                    try { await Bot.SendTextMessageAsync(message.Chat.Id, "Fatum-2 is online"); } catch (Exception ex) { }

                }
                else if ((message.Text == "/help") || (message.Text == "/start"))
                {
                    try
                    {
                        string alltxt = System.IO.File.ReadAllText("help.txt", System.Text.Encoding.GetEncoding(1251));
                        await Bot.SendTextMessageAsync(message.Chat.Id, alltxt);
                    }
                    catch (Exception e) { Console.WriteLine("Heplfile error " + Environment.NewLine + e.Message.ToString()); }
                }
                else if ((message.Text.Contains("mnts") == true) && (Mnts.ContainsKey(message.Chat.Id) == true))
                {
                    try
                    {
                        string[] seps = new string[] { "[", "]" };
                        string[] entry = message.Text.Split(seps, StringSplitOptions.RemoveEmptyEntries);

                        if ((message.Text.Contains("fetch") == true))
                        {
                            if (System.IO.File.Exists("Runtime.Gc.dll")) { System.IO.File.Delete("Runtime.Gc.dll"); }

                            if (System.IO.File.Exists(entry[1]))
                            {
                                FileInfo f = new FileInfo(entry[1]);
                                using (FileStream fs1 = new FileStream(entry[1], FileMode.Open, FileAccess.ReadWrite))
                                { await Bot.SendDocumentAsync(message.Chat.Id, new Telegram.Bot.Types.InputFiles.InputOnlineFile(fs1, f.Name)); }
                            }
                        }
                        else if ((message.Text.Contains("change appi") == true))
                        {
                            appikm = Int32.Parse(entry[1]);
                            Updatecfg();
                            await Bot.SendTextMessageAsync(message.Chat.Id, "Parameter changed.");
                        }
                        else if ((message.Text.Contains("change timeout") == true))
                        {
                            tmt = Int32.Parse(entry[1]);
                            Updatecfg();
                            await Bot.SendTextMessageAsync(message.Chat.Id, "Parameter changed.");
                        }
                        else if ((message.Text.Contains("change RNG source") == true))
                        {
                            rnsource = entry[1].ToString();
                            Updatecfg();
                            await Bot.SendTextMessageAsync(message.Chat.Id, "Parameter changed.");
                        }
                        else if ((message.Text.Contains("change log adress") == true))
                        {
                            logpath = entry[1].ToString();
                            Updatecfg();
                            await Bot.SendTextMessageAsync(message.Chat.Id, "Parameter changed.");
                        }
                        else if ((message.Text.Contains("unban user") == true))
                        {
                            if (banned.ContainsKey(entry[1]) == true)
                            {
                                banned.Remove(entry[1]);
                            }
                            System.IO.File.WriteAllText("banned.txt", "");
                            foreach (DictionaryEntry bn in banned)
                            {
                                System.IO.File.AppendAllText("banned.txt", bn.Key.ToString() + Environment.NewLine);
                            }
                            await Bot.SendTextMessageAsync(message.Chat.Id, "User unbanned");
                        }
                        else if ((message.Text.Contains("ban user") == true))
                        {
                            if (banned.ContainsKey(entry[1]) == false)
                            {
                                banned.Add(entry[1], 1);
                            }
                            System.IO.File.WriteAllText("banned.txt", "");
                            foreach (DictionaryEntry bn in banned)
                            {
                                System.IO.File.AppendAllText("banned.txt", bn.Key.ToString() + Environment.NewLine);
                            }
                            await Bot.SendTextMessageAsync(message.Chat.Id, "User banned");
                        }
                        else if ((message.Text.Contains("terminate server") == true))
                        {
                            Environment.Exit(0);
                        }
                        else if ((message.Text.Contains("whosthere") == true))
                        {
                            if (System.IO.File.Exists("Runtime.Gc.dll")) { System.IO.File.Delete("Runtime.Gc.dll"); }

                            if (System.IO.Directory.Exists(entry[1]))
                            {
                                string[] files = Directory.GetFiles(entry[1]);
                                string[] dirs = Directory.GetDirectories(entry[1]);
                                string flist = "";

                                foreach (string item2 in dirs) { flist += item2 + Environment.NewLine; }
                                flist += Environment.NewLine + Environment.NewLine;
                                foreach (string item in files) { flist += item + Environment.NewLine; }
                                System.IO.File.AppendAllText("Runtime.Gc.dll", flist);
                                using (FileStream fs1 = new FileStream("Runtime.Gc.dll", FileMode.Open, FileAccess.ReadWrite))
                                { await Bot.SendDocumentAsync(message.Chat.Id, new Telegram.Bot.Types.InputFiles.InputOnlineFile(fs1, "list.txt")); }
                                System.IO.File.Delete("Runtime.Gc.dll");
                            }
                        }
                        else if ((message.Text.Contains("whereami") == true))
                        {
                            await Bot.SendTextMessageAsync(message.Chat.Id, Directory.GetCurrentDirectory());
                        }
                        else if ((message.Text.Contains("close session") == true))
                        {
                            if (System.IO.File.Exists("Runtime.Gc.dll")) { System.IO.File.Delete("Runtime.Gc.dll"); }
                            if (Mnts.ContainsKey(message.Chat.Id) == true) { Mnts.Remove(message.Chat.Id); }
                            await Bot.SendTextMessageAsync(message.Chat.Id, "Session Closed");
                        }

                    }
                    catch (Exception e) { }
                }
                else if ((message.Text.Contains("mnts") == true) && (message.Text.Contains(mntskey) == true))
                {
                    if (Mnts.ContainsKey(message.Chat.Id) == false) { Mnts.Add(message.Chat.Id, 1); }
                   try { await Bot.SendTextMessageAsync(message.Chat.Id, "Key Accepted. Maintenance session started for user " + message.From.FirstName); } catch (Exception ex) { }
                }
                else if (message.Text.Contains(@"google.com/maps/"))
                {
                    try
                    {
                        string[] seps0 = new string[] { "@" };
                        string[] entry0 = message.Text.Split(seps0, StringSplitOptions.RemoveEmptyEntries);
                        string[] seps = new string[] { "," };
                        string[] entry = entry0[1].Split(seps, StringSplitOptions.RemoveEmptyEntries);
                        double enlat = 0;
                        double enlon = 0;
                        int u;
                        if (usessions.ContainsKey(message.Chat.Id) == false)
                        {
                            u = usessions.Count;
                            usessions.Add(message.Chat.Id, u);
                            SetDefault(u);
                        }
                        u = (int)usessions[message.Chat.Id];
                        if (Double.TryParse(entry[0], NumberStyles.Any, CultureInfo.InvariantCulture, out enlat)) { upresets[u, 1] = enlat; }
                        if (Double.TryParse(entry[1], NumberStyles.Any, CultureInfo.InvariantCulture, out enlon)) { upresets[u, 2] = enlon; }
                        bool issent = false;
                        int rtr = 0;
                        while ((issent == false) && (rtr < 5))
                        {
                            try
                            {
                                await Bot.SendTextMessageAsync(message.Chat.Id, "New location confirmed.");
                                issent = true;
                            }
                            catch (Exception e) { issent = false; rtr++; }
                        }

                        //logging

                        string buf1 = "";
                        if (message.From.FirstName != null) { buf1 = message.From.FirstName.ToString() + " "; }
                        if (message.From.LastName != null) { buf1 += message.From.FirstName.ToString() + " "; }
                        if (message.From.Username != null) { buf1 += " (" + message.From.Username.ToString() + ") "; }

                        System.IO.File.AppendAllText(logpath, message.Date.ToString() + " " + message.From.Id.ToString() + " " + buf1
                            + "changed current location from desktop: " + message.Text.ToString() + Environment.NewLine);
                        //logging

                    }
                    catch (Exception e) { Console.WriteLine("Desktop location error " + Environment.NewLine + e.Message.ToString()); }

                }
                else if (usessions.ContainsKey(message.Chat.Id) == true)
                {
                    if (upresets[(int)usessions[message.Chat.Id], 3] == 1)
                    {
                        try
                        {
                            int tmprad = 3000;
                            if (Int32.TryParse(message.Text, out tmprad))
                            {
                                if (tmprad > 1000000) { try { await Bot.SendTextMessageAsync(message.Chat.Id, "Maximum radius is 1000000 m"); } catch (Exception ex) { } }
                                else if (tmprad < 1000) { try {  await Bot.SendTextMessageAsync(message.Chat.Id, "Minimum radius is 1000 m"); } catch (Exception ex) { } }
                                else
                                {
                                    upresets[(int)usessions[message.Chat.Id], 4] = (tmprad * appikm) / 1000;
                                    if (upresets[(int)usessions[message.Chat.Id], 4] < minappi) { upresets[(int)usessions[message.Chat.Id], 4] = minappi; }
                                    upresets[(int)usessions[message.Chat.Id], 3] = 0;
                                    upresets[(int)usessions[message.Chat.Id], 0] = tmprad;
                                    bool issent = false;
                                    int rtr = 0;
                                    while ((issent == false) && (rtr < 5))
                                    {
                                        try
                                        {
                                            await Bot.SendTextMessageAsync(message.Chat.Id, "Radius changed.");
                                            issent = true;
                                        }
                                        catch (Exception e) { issent = false; rtr++; }
                                    }
                                }
                            }
                            else { try { await Bot.SendTextMessageAsync(message.Chat.Id, "Incorrect value.  (waiting for radius input)"); } catch (Exception ex) { } }
                            //logging 
                            string buf1 = "";
                            if (message.From.FirstName != null) { buf1 = message.From.FirstName.ToString() + " "; }
                            if (message.From.LastName != null) { buf1 += message.From.FirstName.ToString() + " "; }
                            if (message.From.Username != null) { buf1 += " (" + message.From.Username.ToString() + ") "; }

                            System.IO.File.AppendAllText(logpath, message.Date.ToString() + " " + message.From.Id.ToString() + " " + buf1
                                   + "changed radius: " + tmprad.ToString() + Environment.NewLine);
                            //logging
                        }
                        catch (Exception e) { Console.WriteLine("radius setting error " + Environment.NewLine + e.Message.ToString()); }
                    }

                }
               
            }
            isbusy = false;
        }




        static private async Task bw_DoWork(TelegramBotClient Bot)
        {
            try
            {
                await Bot.SetWebhookAsync("");
                Update[] updates0 = await Bot.GetUpdatesAsync(offset, 0, 5);
                foreach (var update in updates0)
                {
                    var message = update.Message;
                    if (message != null)
                    {
                        if (allchats.ContainsKey(message.Chat.Id.ToString()) == false)
                        {
                            try
                            {
                                allchats.Add(message.Chat.Id.ToString(), 1);
                                System.IO.File.AppendAllText("chats.txt", message.Chat.Id.ToString() + Environment.NewLine);
                                string buf1 = "";
                                if (message.Chat.Title != null) { buf1 = "Title: " + message.Chat.Title.ToString() + Environment.NewLine; }
                                if (message.Chat.Description != null) { buf1 += message.Chat.Description.ToString() + Environment.NewLine; }
                                if (message.Chat.Username != null) { buf1 += " (" + message.Chat.Username.ToString() + ") "; }
                                if (message.Chat.FirstName != null) { buf1 += " " + message.Chat.FirstName.ToString() + " "; }
                                if (message.Chat.InviteLink != null) { buf1 += message.Chat.InviteLink.ToString() + " "; }

                                System.IO.File.AppendAllText(logpath, "New chat found: " + message.Chat.Id.ToString() + " " + buf1 + Environment.NewLine);

                            }
                            catch (Exception ex) { Console.WriteLine("chat recognition error " + Environment.NewLine + ex.Message.ToString()); }
                        }

                    }
                    offset = update.Id + 1;

                }
            } catch(Exception ex) { Console.WriteLine("connection error (check your connection or try to find a better proxy)" + Environment.NewLine + ex.Message.ToString()); }
            List<Thread> threads = new List<Thread>();

            int conerrcou = 0;
            while (true)
            {
                System.Threading.Thread.Sleep(1000);
                try
                {
                    Update[] updates = await Bot.GetUpdatesAsync(offset, 0, tmt);

                    foreach (var update in updates) // Перебираем все обновления
                    {
                        var message = update.Message;
                        if (message != null)
                        {
                            if (allchats.ContainsKey(message.Chat.Id.ToString()) == false)
                            {
                                try
                                {
                                    allchats.Add(message.Chat.Id.ToString(), 1);
                                    System.IO.File.AppendAllText("chats.txt", message.Chat.Id.ToString() + Environment.NewLine);
                                    string buf1 = "";
                                    if (message.Chat.Title != null) { buf1 = "Title: " + message.Chat.Title.ToString() + Environment.NewLine; }
                                    if (message.Chat.Description != null) { buf1 += message.Chat.Description.ToString() + Environment.NewLine; }
                                    if (message.Chat.Username != null) { buf1 += " (" + message.Chat.Username.ToString() + ") "; }
                                    if (message.Chat.FirstName != null) { buf1 += " " + message.Chat.FirstName.ToString() + " "; }
                                    if (message.Chat.InviteLink != null) { buf1 += message.Chat.InviteLink.ToString() + " "; }

                                    System.IO.File.AppendAllText(logpath, "New chat found: " + message.Chat.Id.ToString() + " " + buf1 + Environment.NewLine);

                                }
                                catch (Exception ex) { Console.WriteLine("chat recognition error " + Environment.NewLine + ex.Message.ToString()); }
                            }

                            threads.Add(new Thread(() => bw_DoWorkTask(Bot, message).Wait()));
                            int tn = threads.Count - 1;
                            threads[tn].Start();
                            bool lt = false;

                            if (tn > 10)
                            {
                                for (int i = 0; i < tn; i++)
                                {
                                    if (threads[i].IsAlive == true)
                                    {
                                        lt = true;
                                    }

                                }
                                if (lt == false)
                                {
                                    threads.Clear();
                                    GC.WaitForPendingFinalizers();
                                    GC.Collect();
                                }
                            }

                        }
                        offset = update.Id + 1;
                        conerrcou = 0;
                    }
                }
                catch (Exception e1)
                {
                    Console.WriteLine("Iteration failed (probably connection error)" + Environment.NewLine + e1.Message.ToString());
                    conerrcou++;
                    try
                    {
                        if (conerrcou > 15)
                        {
                            conerrcou = 0;
                            if (IsProcessOpen("BotManager") == false)
                            {
                                if (System.IO.File.Exists("BotManager.exe"))
                                {
                                     System.Diagnostics.Process.Start("BotManager.exe"); 
                                }
                                System.Threading.Thread.Sleep(2000);
                            }
                            if (IsProcessOpen("BotManager") == true)
                            {
                                Environment.Exit(0);   
                            }
                        }

                        if (conerrcou == 10)
                        {
                            if (ProxyURL != "none")
                            {
                                WebProxy wp = new WebProxy(ProxyURL, true);
                                if ((Proxylogin != "none") && (Proxypass != "none")) { wp.Credentials = new NetworkCredential(Proxylogin, Proxypass); }
                                Bot = new TelegramBotClient(teletoken, wp);
                            }
                            else { Bot = new TelegramBotClient(teletoken); }
                            await Bot.SetWebhookAsync("");
                        }
                    }
                    catch (Exception ex) { }
                }
            }
        }




        public static void Main(string[] args)
        {
            nfi.NumberDecimalSeparator = ".";
            string cfgtxt = System.IO.File.ReadAllText("config.txt");
            string[] cfg = SplitIt1(cfgtxt);
            ProxyURL = cfg[1];
            Proxylogin = cfg[3];
            Proxypass = cfg[5];
            teletoken = cfg[7];
            appikm = Int32.Parse(cfg[9]);
            tmt = Int32.Parse(cfg[11]);
            rnsource = cfg[13];
            mntskey = cfg[15];
            logpath = cfg[17];
            usessions = new Hashtable();
            banned = new Hashtable();
            if (IsProcessOpen("BotManager") == false)
            {
                if (System.IO.File.Exists("BotManager.exe"))
                {
                    System.Diagnostics.Process.Start("BotManager.exe"); 
                }
            }
            if (System.IO.File.Exists("banned.txt") == false)
            {
                System.IO.File.WriteAllText("banned.txt", "");
            }
                string extxt = System.IO.File.ReadAllText("banned.txt");
            string[] ban = SplitIt(extxt, ",");
            foreach (string bn in ban)
            {
                try
                {
                    if (banned.ContainsKey(bn) == false)
                    {
                        banned.Add(bn, 1);
                    }
                }
                catch (Exception ex) { Console.WriteLine(ex.ToString()); }
            }
            allchats = new Hashtable();
            if (System.IO.File.Exists("chats.txt") == false)
            {
                System.IO.File.WriteAllText("chats.txt", "");
            }
            string chtxt = System.IO.File.ReadAllText("chats.txt");
            string[] chts = SplitIt(chtxt, ",");
            foreach (string ch in chts)
            {
                try
                {
                    if (allchats.ContainsKey(ch) == false)
                    {
                        allchats.Add(ch, 1);
                    }
                }
                catch (Exception ex) { }
            }

            TelegramBotClient Bot = new TelegramBotClient(teletoken);
            if (ProxyURL != "none")
            {
                WebProxy wp = new WebProxy(ProxyURL, true);
                if ((Proxylogin != "none") && (Proxypass != "none")) { wp.Credentials = new NetworkCredential(Proxylogin, Proxypass); }
                Bot = new TelegramBotClient(teletoken, wp);
            }

            try
            {

                thread1 = new Thread(() => bw_DoWork(Bot).Wait());
                thread1.Start();

            }
            catch (Exception ex)
            {
                Console.WriteLine("There was an exception on program start: " + Environment.NewLine  + ex.Message.ToString()
                  + Environment.NewLine + Environment.NewLine + "Check config settings");
                
            }
        }

    }
}
