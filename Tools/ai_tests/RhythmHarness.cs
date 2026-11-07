using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using FitQuest;

/// <summary>Автотест RhythmPoseTracker: подаёт синтетические кадры, сравнивает нажатые стрелки с меткой.</summary>
static class RhythmHarness
{
    class Trial { public double Start, Dur; public string Label; public string Got = ""; public double First = -1; }

    static void Main(string[] args)
    {
        float strict = args.Length > 1 ? float.Parse(args[1], CultureInfo.InvariantCulture) : 1f;
        var confusion = new SortedDictionary<string, SortedDictionary<string, int>>();
        var latencies = new List<double>();
        int correct = 0, total = 0, stray = 0;
        foreach (string path in args[0].Split(','))
        {
            var tracker = new RhythmPoseTracker { Strictness = strict };
            var frame = new PoseFrame();
            var pressed = new List<int>();
            var trials = new List<Trial>();
            foreach (string line in File.ReadLines(path))
            {
                if (line.StartsWith("#LABEL|"))
                {
                    var p = line.Split('|');
                    trials.Add(new Trial { Start = double.Parse(p[1], CultureInfo.InvariantCulture), Dur = double.Parse(p[2], CultureInfo.InvariantCulture), Label = p[3] });
                    continue;
                }
                int bar = line.IndexOf('|');
                double t = double.Parse(line.Substring(0, bar), CultureInfo.InvariantCulture);
                if (!PosePacket.TryParse(line.Substring(bar + 1), frame)) throw new Exception("parse");
                pressed.Clear();
                tracker.Feed(frame, t, pressed);
                foreach (int lane in pressed)
                {
                    Trial owner = null;
                    foreach (var tr in trials) if (t >= tr.Start && t <= tr.Start + tr.Dur + 0.3) owner = tr;
                    if (owner == null) { stray++; continue; }
                    string c = "LDUR".Substring(lane, 1);
                    if (!owner.Got.Contains(c)) owner.Got += c;
                    if (owner.First < 0) owner.First = t - owner.Start;
                }
            }
            foreach (var tr in trials)
            {
                string got = tr.Got.Length == 0 ? "-" : Sort(tr.Got);
                if (!confusion.ContainsKey(tr.Label)) confusion[tr.Label] = new SortedDictionary<string, int>();
                confusion[tr.Label][got] = confusion[tr.Label].ContainsKey(got) ? confusion[tr.Label][got] + 1 : 1;
                total++;
                if (got == Sort(tr.Label)) { correct++; if (tr.First >= 0) latencies.Add(tr.First); }
            }
        }
        foreach (var kv in confusion)
        {
            int n = 0; foreach (var c in kv.Value) n += c.Value;
            string row = ("Ожидалось " + kv.Key).PadRight(16) + " n=" + n.ToString().PadLeft(3) + "  ";
            foreach (var c in kv.Value) row += c.Key + ":" + c.Value + "  ";
            Console.WriteLine(row);
        }
        latencies.Sort();
        Console.WriteLine();
        Console.WriteLine("Accuracy: " + correct + "/" + total + " = " + (100.0 * correct / total).ToString("0.0") + "%   stray presses: " + stray);
        if (latencies.Count > 0) Console.WriteLine("Latency from onset: median " + latencies[latencies.Count / 2].ToString("0.00") + " s");
    }

    static string Sort(string s)
    {
        if (s == "-") return s;
        char[] a = s.ToCharArray(); Array.Sort(a); return new string(a);
    }
}
