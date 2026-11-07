using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using FitQuest;

static class Harness
{
    class Trial { public double Start, Dur; public string Label; public List<MotionEvent> Events = new List<MotionEvent>(); public List<double> Times = new List<double>(); }

    static void Main(string[] args)
    {
        float strict = args.Length > 1 ? float.Parse(args[1], CultureInfo.InvariantCulture) : 1f;
        var confusion = new Dictionary<string, Dictionary<string, int>>();
        var qualities = new Dictionary<string, List<float>>();
        var latencies = new List<double>();
        int files = 0;
        foreach (string path in args[0].Split(','))
        {
            files++;
            var clf = new PoseMotionClassifier { Strictness = strict };
            var frame = new PoseFrame();
            var outEvents = new List<MotionEvent>();
            var trials = new List<Trial>();
            Trial current = null;
            foreach (string line in File.ReadLines(path))
            {
                if (line.StartsWith("#LABEL|"))
                {
                    var p = line.Split('|');
                    current = new Trial { Start = double.Parse(p[1], CultureInfo.InvariantCulture), Dur = double.Parse(p[2], CultureInfo.InvariantCulture), Label = p[3] };
                    trials.Add(current);
                    continue;
                }
                int bar = line.IndexOf('|');
                double t = double.Parse(line.Substring(0, bar), CultureInfo.InvariantCulture);
                if (!PosePacket.TryParse(line.Substring(bar + 1), frame)) throw new Exception("parse");
                outEvents.Clear();
                clf.Feed(frame, t, outEvents);
                foreach (var e in outEvents)
                {
                    // событие относим к попытке, если оно в её окне (+0.4 с)
                    Trial owner = null;
                    foreach (var tr in trials) if (t >= tr.Start && t <= tr.Start + tr.Dur + 0.4) owner = tr;
                    if (owner == null) { Add(confusion, "Calibration/gap", e.Type + (e.Quality < 0.35f ? "(weak)" : "")); continue; }
                    owner.Events.Add(e); owner.Times.Add(t);
                }
            }
            foreach (var tr in trials)
            {
                string predicted = "None";
                for (int i = 0; i < tr.Events.Count; i++)
                {
                    var e = tr.Events[i];
                    if (e.IsUpdate) continue;
                    if (e.Quality >= 0.35f) { predicted = e.Type.ToString(); latencies.Add(tr.Times[i] - tr.Start); break; }
                    if (predicted == "None") predicted = "Weak" + e.Type;
                }
                // итоговое качество = максимум с уточнениями для засчитанного типа
                float q = 0f;
                foreach (var e in tr.Events) if (e.Type.ToString() == predicted) q = Math.Max(q, e.Quality);
                Add(confusion, tr.Label, predicted);
                if (!qualities.ContainsKey(tr.Label)) qualities[tr.Label] = new List<float>();
                if (q > 0) qualities[tr.Label].Add(q);
            }
            Console.WriteLine(path + ": recalibrations " + clf.Recalibrations);
        }
        Console.WriteLine();
        int correct = 0, total = 0;
        foreach (var kv in confusion)
        {
            int n = 0; foreach (var c in kv.Value) n += c.Value;
            string row = kv.Key.PadRight(16) + " n=" + n.ToString().PadLeft(3) + "  ";
            foreach (var c in kv.Value) row += c.Key + ":" + c.Value + "  ";
            if (qualities.ContainsKey(kv.Key) && qualities[kv.Key].Count > 0) { float s = 0; foreach (var x in qualities[kv.Key]) s += x; row += " avgQ=" + (s / qualities[kv.Key].Count).ToString("0.00"); }
            Console.WriteLine(row);
            if (kv.Key == "Calibration/gap") continue;
            total += n;
            foreach (var c in kv.Value) if (Ok(kv.Key, c.Key)) correct += c.Value;
        }
        latencies.Sort();
        Console.WriteLine();
        Console.WriteLine("Accuracy: " + correct + "/" + total + " = " + (100.0 * correct / total).ToString("0.0") + "%");
        if (latencies.Count > 0) Console.WriteLine("Latency from onset: median " + latencies[latencies.Count / 2].ToString("0.00") + " s");
    }

    static bool Ok(string label, string predicted)
    {
        if (label == "Idle") return predicted == "None" || predicted.StartsWith("Weak");
        if (label.StartsWith("Weak:")) return predicted == "None" || predicted.StartsWith("Weak");
        return predicted == label;
    }

    static void Add(Dictionary<string, Dictionary<string, int>> m, string a, string b)
    {
        if (!m.ContainsKey(a)) m[a] = new Dictionary<string, int>();
        m[a][b] = m[a].ContainsKey(b) ? m[a][b] + 1 : 1;
    }
}
