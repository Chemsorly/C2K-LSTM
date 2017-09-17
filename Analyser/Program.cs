using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyser
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;

            const String InFile = @"D:\Desktop\Masterarbeit\CascadeResults_OutDataRAW.csv";
            const String OutFile = @"D:\Desktop\Masterarbeit\CascadeResults_OutDataEDITED.csv";
            const double BucketGranularity = 0.05; //creates a bucket every 0.05 of completion


            using (TextFieldParser parser = new TextFieldParser(InFile))
            {
                List<Line> output = new List<Line>();

                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(",");
                bool firstline = true;
                while (!parser.EndOfData)
                {
                    //rows
                    string[] fields = parser.ReadFields();

                    if (firstline || String.IsNullOrEmpty(fields[0]))
                    {
                        firstline = false;
                        continue;
                    }

                    //input
                    // 0 sequenceid int
                    // 1 sequencelength int
                    // 2 prefix int
                    // 3 sumprevious float
                    // 4 timestamp float
                    // 5 completion float range(0-1)
                    // 6 gt_sumprevious int
                    // 7 gt_timestamp int
                    // 8 gt_planned int
                    // 9 gt_instance int
                    //10 prefix_activities string
                    //11 predicted_activities string

                    //additional output
                    //12 accuracy_sumprevious float
                    //13 accuracy_timestamp float
                    //14 violation_effective bool
                    //15 violation_predicted bool
                    //16 violation_string string

                    Line line = new Line()
                    {
                        SequenceID = int.Parse(fields[0]),
                        SequenceLength = int.Parse(fields[1]),
                        Prefix = int.Parse(fields[2]),
                        SumPrevious = double.Parse(fields[3], CultureInfo.InvariantCulture),
                        Timestamp = double.Parse(fields[4], CultureInfo.InvariantCulture),
                        Completion = double.Parse(fields[5], CultureInfo.InvariantCulture),
                        GT_SumPrevious = int.Parse(fields[6]),
                        GT_Timestamp = int.Parse(fields[7]),
                        GT_Planned = int.Parse(fields[8]),
                        GT_InstanceID = int.Parse(fields[9]),
                        PrefixActivities = fields[10],
                        PredictedActivities = fields[11]
                    };
                    output.Add(line);

                    //calculate accuracy values
                    line.AccuracySumprevious = CalculateAccuracy(line.SumPrevious, line.GT_SumPrevious);
                    line.AccuracyTimestamp = CalculateAccuracy(line.Timestamp, line.GT_Timestamp);
                }

                //create buckets
                List<Bucket> BucketList = new List<Bucket>();
                for (int i = 0; i * BucketGranularity <= 1; i++)
                    BucketList.Add(new Bucket() {BucketLevel = i, ViolationStrings = new List<string>(), PredictionAccuraciesSP =  new List<double>(), PredictionAccuraciesTS = new List<double>()});

                //fill buckets
                foreach (var line in output)
                {
                    //iterate until proper bucket found
                    for (int i = 0; i * BucketGranularity <= 1; i++)
                    {
                        if (line.Completion >= i * BucketGranularity && line.Completion < (i + 1) * BucketGranularity)
                        {
                            BucketList[i].ViolationStrings.Add(line.Violation_String);
                            BucketList[i].PredictionAccuraciesSP.Add(line.AccuracySumprevious);
                            BucketList[i].PredictionAccuraciesTS.Add(line.AccuracyTimestamp);
                            break;
                        }
                    }
                }

                //writelines
                List<String> exportrows = new List<string>();
                exportrows.Add("sequenceid,sequencelength,prefix,sumprevious,timestamp,completion,gt_sumprevious,gt_timestamp,gt_planned,gt_instance,prefix_activities,predicted_activities,accuracy_sumprevious,accuracy_timestamp,violation_effective,violation_predicted,violation_string");
                foreach (var line in output)
                {
                    exportrows.Add($"{line.SequenceID},{line.SequenceLength},{line.Prefix},{line.SumPrevious},{line.Timestamp},{line.Completion},{line.GT_SumPrevious},{line.GT_Timestamp},{line.GT_Planned},{line.GT_InstanceID},{line.PrefixActivities},{line.PredictedActivities},{line.AccuracySumprevious},{line.AccuracyTimestamp},{line.Violation_Effective},{line.Violation_Predicted},{line.Violation_String}");
                }
                //add buckets
                exportrows.Add("bucket_level,(TN+TP) / Count, Count, Median Sumprevious Accuracy, Median Timestamp Accuracy");
                foreach (var bucket in BucketList)
                    if(bucket.ViolationStrings.Any())
                        exportrows.Add($"{bucket.BucketLevel * BucketGranularity}, {bucket.ViolationRatio},{bucket.ViolationStrings.Count},{bucket.PredictionMeanSP},{bucket.PredictionMeanTS}  ");
                exportrows.Add($"Total,{BucketList.Where(t => t.ViolationStrings.Count > 0).Sum(t => t.ViolationRatio) / (double)BucketList.Count(t => t.ViolationStrings.Any())},{BucketList.Sum(t => t.ViolationStrings.Count)},{BucketList.Where(t => t.ViolationStrings.Count > 0).Sum(t => t.PredictionMeanSP) / (double)BucketList.Count(t => t.ViolationStrings.Any())},{BucketList.Sum(t => t.ViolationStrings.Count)},{BucketList.Where(t => t.ViolationStrings.Count > 0).Sum(t => t.PredictionMeanTS) / (double)BucketList.Count(t => t.ViolationStrings.Any())}");

                ////export as csv to match LSTM input examples
                File.WriteAllLines(OutFile, exportrows);
            }
        }

        public static double CalculateAccuracy(double pInput, double pReference)
        {
            return pInput > pReference
                ? (pInput - (Math.Abs(pInput - pReference) * 2) )/ pReference
                : pInput / pReference;
        }

        public static String CalculateViolationString(bool pEffective, bool pPredicted)
        {
            if (pEffective && pPredicted)
                return "TP";
            if (!pEffective && pPredicted)
                return "FP";
            if (pEffective && !pPredicted)
                return "FN";
            if (!pEffective && !pPredicted)
                return "TN";

            return "oops";
        }

        class Line
        {
            //input
            public int SequenceID { get; set; }
            public int SequenceLength { get; set; }
            public int Prefix { get; set; }
            public double SumPrevious { get; set; }
            public double Timestamp { get; set; }
            public double Completion { get; set; }
            public int GT_SumPrevious { get; set; }
            public int GT_Timestamp { get; set; }
            public int GT_Planned { get; set; }
            public int GT_InstanceID { get; set; }
            public String PrefixActivities { get; set; }
            public String PredictedActivities { get; set; }

            //output
            public double AccuracySumprevious { get; set; }
            public double AccuracyTimestamp { get; set; }
            public bool Violation_Effective => GT_Timestamp > GT_Planned;
            public bool Violation_Predicted => Timestamp > GT_Planned;
            public String Violation_String => CalculateViolationString(Violation_Effective, Violation_Predicted);
        }

        class Bucket
        {
            public int BucketLevel { get; set; }

            public List<String> ViolationStrings { get; set; }
            public List<Double> PredictionAccuraciesSP { get; set; }
            public List<Double> PredictionAccuraciesTS { get; set; }
            //binary prediction
            public double ViolationRatio => (double) ViolationStrings.Count(t => t == "TN" || t == "TP") / (double) ViolationStrings.Count;

            //regression prediction
            public double PredictionMeanSP => Median(PredictionAccuraciesSP.ToArray());
            public double PredictionMeanTS => Median(PredictionAccuraciesTS.ToArray());
        }

        static double Median(double[] xs)
        {
            //https://stackoverflow.com/questions/4140719/calculate-median-in-c-sharp
            var ys = xs.OrderBy(x => x).ToList();
            double mid = (ys.Count - 1) / 2.0;
            return (ys[(int)(mid)] + ys[(int)(mid + 0.5)]) / 2;
        }
    }
}
