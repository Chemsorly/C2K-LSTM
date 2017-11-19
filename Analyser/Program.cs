using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SearchOption = System.IO.SearchOption;

namespace Analyser
{
    class Program
    {
        const double BucketGranularity = 0.05; //creates a bucket every 0.05 of completion
        const double FmetricBeta = 1;

        static void Main(string[] args)
        {
            System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;

            DirectoryInfo InFolder = new DirectoryInfo(@"Y:\Sicherung\Adrian\Sync\Sciebo\MA RNN-LSTM Results\raw");
            List<FileInfo> InFiles = InFolder.EnumerateFiles("*",SearchOption.AllDirectories).Where(t => t.Name.Contains(".csv") && !t.Name.Contains(".edited.csv")).ToList();

            foreach (var file in InFiles)
            {
                using (TextFieldParser parser = new TextFieldParser(file.FullName))
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
                        BucketList.Add(new Bucket() { BucketLevel = i,
                            Prediction_SP = new List<double>(),
                            Prediction_TS = new List<double>(),
                            ViolationStringsTS = new List<string>(),
                            ViolationStringsSP = new List<string>(),
                            PredictionAccuraciesSP = new List<double>(),
                            PredictionAccuraciesTS = new List<double>(),
                            DeviationsAbsoluteSP = new List<double>(),
                            DeviationsAbsoluteTS = new List<double>()
                        });

                    //fill buckets
                    foreach (var line in output)
                    {
                        //iterate until proper bucket found
                        for (int i = 0; i * BucketGranularity <= 1; i++)
                        {
                            if (line.Completion >= i * BucketGranularity && line.Completion < (i + 1) * BucketGranularity)
                            {
                                BucketList[i].Prediction_SP.Add(line.SumPrevious);
                                BucketList[i].Prediction_TS.Add(line.Timestamp);
                                BucketList[i].ViolationStringsSP.Add(line.Violation_StringSP);
                                BucketList[i].ViolationStringsTS.Add(line.Violation_StringTS);
                                BucketList[i].PredictionAccuraciesSP.Add(line.AccuracySumprevious);
                                BucketList[i].PredictionAccuraciesTS.Add(line.AccuracyTimestamp);
                                BucketList[i].DeviationsAbsoluteSP.Add(line.DeviationAbsoluteSumprevious);
                                BucketList[i].DeviationsAbsoluteTS.Add(line.DeviationAbsoluteTimestamp);
                                break;
                            }
                        }
                    }

                    //writelines
                    List<String> exportrows = new List<string>();
                    exportrows.Add("sequenceid," +
                                   "sequencelength," +
                                   "prefix,sumprevious," +
                                   "timestamp,completion," +
                                   "gt_sumprevious," +
                                   "gt_timestamp," +
                                   "gt_planned," +
                                   "gt_instance," +
                                   "prefix_activities," +
                                   "predicted_activities," +
                                   "accuracy_sumprevious," +
                                   "accuracy_timestamp," +
                                   "violation_effective," +
                                   "violation_predicted_sp," +
                                   "violation_predicted_ts," +
                                   "violation_string_sp," +
                                   "violation_string_ts," +
                                   "deviation_abs_sp," +
                                   "deviation_abs_ts");
                    foreach (var line in output)
                    {
                        exportrows.Add($"{line.SequenceID}," +
                                       $"{line.SequenceLength}," +
                                       $"{line.Prefix}," +
                                       $"{line.SumPrevious}," +
                                       $"{line.Timestamp}," +
                                       $"{line.Completion}," +
                                       $"{line.GT_SumPrevious}," +
                                       $"{line.GT_Timestamp}," +
                                       $"{line.GT_Planned}," +
                                       $"{line.GT_InstanceID}," +
                                       $"{line.PrefixActivities}," +
                                       $"{line.PredictedActivities}," +
                                       $"{line.AccuracySumprevious}," +
                                       $"{line.AccuracyTimestamp}," +
                                       $"{line.Violation_Effective}," +
                                       $"{line.Violation_PredictedSP}," +
                                       $"{line.Violation_PredictedTS}," +
                                       $"{line.Violation_StringSP}," +
                                       $"{line.Violation_StringTS}," +
                                       $"{line.DeviationAbsoluteSumprevious}," +
                                       $"{line.DeviationAbsoluteTimestamp}");
                    }

                    //add buckets
                    exportrows.Add("bucket_level," +
                                   "Count_SP," +
                                   "Count(TP)_SP," +
                                   "Count(FP)_SP," +
                                   "Count(TN)_SP," +
                                   "Count(FN)_SP," +
                                   "Count_TS," +
                                   "Count(TP)_TS," +
                                   "Count(FP)_TS," +
                                   "Count(TN)_TS," +
                                   "Count(FN)_TS," +
                                   "Median RelativeDeviation SP," +
                                   "Median RelativeDeviation TS," +
                                   "MSE_SP," +
                                   "RMSE_SP," +
                                   "MAE_SP," +
                                   "RSE_SP," +
                                   "RRSE_SP," +
                                   "RAE_SP," +
                                   "Precision_SP," +
                                   "Recall_SP," +
                                   "Specificity_SP," +
                                   "FalsePositiveRate_SP," +
                                   "NegativePredictedValue_SP," +
                                   "Accuracy_SP," +
                                   "F-Measure_SP," +
                                   "MSE_TS," +
                                   "RMSE_TS," +
                                   "MAE_TS," +
                                   "RSE_TS," +
                                   "RRSE_TS," +
                                   "RAE_TS,"+
                                   "Precision_TS," +
                                   "Recall_TS," +
                                   "Specificity_TS," +
                                   "FalsePositiveRate_TS," +
                                   "NegativePredictedValue_TS," +
                                   "Accuracy_TS," +
                                   "F-Measure_TS"
                                   );
                    foreach (var bucket in BucketList)
                        if (bucket.ViolationStringsTS.Any())
                            exportrows.Add($"{bucket.BucketLevel * BucketGranularity}," +
                                           $"{bucket.ViolationStringsSP.Count}," +
                                           $"{bucket.TPcountSP}," +
                                           $"{bucket.FPcountSP}," +
                                           $"{bucket.TNcountSP}," +
                                           $"{bucket.FNcountSP}," +
                                           $"{bucket.ViolationStringsTS.Count}," +
                                           $"{bucket.TPcountTS}," +
                                           $"{bucket.FPcountTS}," +
                                           $"{bucket.TNcountTS}," +
                                           $"{bucket.FNcountTS}," +
                                           $"{bucket.PredictionMedianSP}," +
                                           $"{bucket.PredictionMedianTS}," +
                                           $"{bucket.MSE_SP}," +
                                           $"{bucket.RMSE_SP}," +
                                           $"{bucket.MAE_SP}," +
                                           $"{bucket.RSE_SP}," +
                                           $"{bucket.RRSE_SP}," +
                                           $"{bucket.RAE_SP}," +
                                           $"{bucket.PrecisionSP}," +
                                           $"{bucket.RecallSP}," +
                                           $"{bucket.SpecificitySP}," +
                                           $"{bucket.FalsePositiveRateSP}," +
                                           $"{bucket.NegativePredictedValueSP}," +
                                           $"{bucket.AccuracySP}," +
                                           $"{bucket.FMeasureSP}," +
                                           $"{bucket.MSE_TS}," +
                                           $"{bucket.RMSE_TS}," +
                                           $"{bucket.MAE_TS}," +
                                           $"{bucket.RSE_TS}," +
                                           $"{bucket.RRSE_TS}," +
                                           $"{bucket.RAE_TS}," +
                                           $"{bucket.PrecisionTS}," +
                                           $"{bucket.RecallTS}," +
                                           $"{bucket.SpecificityTS}," +
                                           $"{bucket.FalsePositiveRateTS}," +
                                           $"{bucket.NegativePredictedValueTS}," +
                                           $"{bucket.AccuracyTS}," +
                                           $"{bucket.FMeasureTS}"
                                           );
                    exportrows.Add($"Total," +
                                   $"{BucketList.Sum(t => t.ViolationStringsSP.Count)}," +
                                   $"{BucketList.Sum(t => t.TPcountSP)}," +
                                   $"{BucketList.Sum(t => t.FPcountSP)}," +
                                   $"{BucketList.Sum(t => t.TNcountSP)}," +
                                   $"{BucketList.Sum(t => t.FNcountSP)}," +
                                   $"{BucketList.Sum(t => t.ViolationStringsTS.Count)}," +
                                   $"{BucketList.Sum(t => t.TPcountTS)}," +
                                   $"{BucketList.Sum(t => t.FPcountTS)}," +
                                   $"{BucketList.Sum(t => t.TNcountTS)}," +
                                   $"{BucketList.Sum(t => t.FNcountTS)}," +
                                   $"{BucketList.Where(t => t.ViolationStringsSP.Count > 0).Sum(t => t.PredictionMedianSP) / (double)BucketList.Count(t => t.ViolationStringsSP.Any())}," +
                                   $"{BucketList.Where(t => t.ViolationStringsTS.Count > 0).Sum(t => t.PredictionMedianTS) / (double)BucketList.Count(t => t.ViolationStringsTS.Any())}," +
                                   $"{BucketList.Where(t => t.ViolationStringsSP.Count > 0).Average(t => t.MSE_SP)}," +
                                   $"{BucketList.Where(t => t.ViolationStringsSP.Count > 0).Average(t => t.RMSE_SP)}," +
                                   $"{BucketList.Where(t => t.ViolationStringsSP.Count > 0).Average(t => t.MAE_SP)}," +
                                   $"{BucketList.Where(t => t.ViolationStringsSP.Count > 0).Average(t => t.RSE_SP)}," +
                                   $"{BucketList.Where(t => t.ViolationStringsSP.Count > 0).Average(t => t.RRSE_SP)}," +
                                   $"{BucketList.Where(t => t.ViolationStringsSP.Count > 0).Average(t => t.RAE_SP)}," +
                                   $"{BucketList.Where(t => t.ViolationStringsSP.Count > 0).Average(t => t.PrecisionSP)}," +
                                   $"{BucketList.Where(t => t.ViolationStringsSP.Count > 0).Average(t => t.RecallSP)}," +
                                   $"{BucketList.Where(t => t.ViolationStringsSP.Count > 0).Average(t => t.SpecificitySP)}," +
                                   $"{BucketList.Where(t => t.ViolationStringsSP.Count > 0).Average(t => t.FalsePositiveRateSP)}," +
                                   $"{BucketList.Where(t => t.ViolationStringsSP.Count > 0).Average(t => t.NegativePredictedValueSP)}," +
                                   $"{BucketList.Where(t => t.ViolationStringsSP.Count > 0).Average(t => t.AccuracySP)}," +
                                   $"{BucketList.Where(t => t.ViolationStringsSP.Count > 0).Average(t => t.FMeasureSP)}," +
                                   $"{BucketList.Where(t => t.ViolationStringsTS.Count > 0).Average(t => t.MSE_TS)}," +
                                   $"{BucketList.Where(t => t.ViolationStringsTS.Count > 0).Average(t => t.RMSE_TS)}," +
                                   $"{BucketList.Where(t => t.ViolationStringsTS.Count > 0).Average(t => t.MAE_TS)}," +
                                   $"{BucketList.Where(t => t.ViolationStringsTS.Count > 0).Average(t => t.RSE_TS)}," +
                                   $"{BucketList.Where(t => t.ViolationStringsTS.Count > 0).Average(t => t.RRSE_TS)}," +
                                   $"{BucketList.Where(t => t.ViolationStringsTS.Count > 0).Average(t => t.RAE_TS)}," +
                                   $"{BucketList.Where(t => t.ViolationStringsTS.Count > 0).Average(t => t.PrecisionTS)}," +
                                   $"{BucketList.Where(t => t.ViolationStringsTS.Count > 0).Average(t => t.RecallTS)}," +
                                   $"{BucketList.Where(t => t.ViolationStringsTS.Count > 0).Average(t => t.SpecificityTS)}," +
                                   $"{BucketList.Where(t => t.ViolationStringsTS.Count > 0).Average(t => t.FalsePositiveRateTS)}," +
                                   $"{BucketList.Where(t => t.ViolationStringsTS.Count > 0).Average(t => t.NegativePredictedValueTS)}," +
                                   $"{BucketList.Where(t => t.ViolationStringsTS.Count > 0).Average(t => t.AccuracyTS)}," +
                                   $"{BucketList.Where(t => t.ViolationStringsTS.Count > 0).Average(t => t.FMeasureTS)}"
                                   );



                    ////export as csv to match LSTM input examples
                    File.WriteAllLines($"{file.FullName.Replace(".csv","")}.edited.csv", exportrows);
                }
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
            public double DeviationAbsoluteSumprevious => SumPrevious - GT_SumPrevious;
            public double DeviationAbsoluteTimestamp => Timestamp - GT_Timestamp;
            public bool Violation_Effective => GT_Timestamp > GT_Planned;
            public bool Violation_PredictedTS => Timestamp > GT_Planned;
            public bool Violation_PredictedSP => SumPrevious > GT_Planned;
            public String Violation_StringTS => CalculateViolationString(Violation_Effective, Violation_PredictedTS);
            public String Violation_StringSP => CalculateViolationString(Violation_Effective, Violation_PredictedSP);
            
        }

        class Bucket
        {
            public int BucketLevel { get; set; }

            //counts
            public int TPcountSP => ViolationStringsSP.Count(t => t == "TP");
            public int FPcountSP => ViolationStringsSP.Count(t => t == "FP");
            public int TNcountSP => ViolationStringsSP.Count(t => t == "TN");
            public int FNcountSP => ViolationStringsSP.Count(t => t == "FN");
            public int TPcountTS => ViolationStringsTS.Count(t => t == "TP");
            public int FPcountTS => ViolationStringsTS.Count(t => t == "FP");
            public int TNcountTS => ViolationStringsTS.Count(t => t == "TN");
            public int FNcountTS => ViolationStringsTS.Count(t => t == "FN");


            public List<Double> Prediction_SP { get; set; }
            public List<Double> Prediction_TS { get; set; }
            public List<String> ViolationStringsTS { get; set; }
            public List<String> ViolationStringsSP { get; set; }
            public List<Double> PredictionAccuraciesSP { get; set; }
            public List<Double> PredictionAccuraciesTS { get; set; }
            public List<Double> DeviationsAbsoluteSP { get; set; }
            public List<Double> DeviationsAbsoluteTS { get; set; }
            //binary prediction
            public double PrecisionSP => (double)ViolationStringsSP.Count(t => t == "TP") / (double) ViolationStringsSP.Count(t => t == "TP" || t == "FP");
            public double RecallSP => (double)ViolationStringsSP.Count(t => t == "TP") / (double)ViolationStringsSP.Count(t => t == "TP" || t == "FN");
            public double SpecificitySP => (double)ViolationStringsSP.Count(t => t == "TN") / (double)ViolationStringsSP.Count(t => t == "TN" || t == "FP");
            public double FalsePositiveRateSP => (double)ViolationStringsSP.Count(t => t == "FP") / (double)ViolationStringsSP.Count(t => t == "FP" || t == "TN");
            public double NegativePredictedValueSP => (double)ViolationStringsSP.Count(t => t == "TN") / (double)ViolationStringsSP.Count(t => t == "TN" || t == "FP");
            public double AccuracySP => (double)ViolationStringsSP.Count(t => t == "TN" || t == "TP") / (double)ViolationStringsSP.Count;
            public double FMeasureSP => (Math.Pow(1 + FmetricBeta, 2) * PrecisionSP * RecallSP) / ((Math.Pow(FmetricBeta, 2) * PrecisionSP) + RecallSP);
            public double PrecisionTS => (double)ViolationStringsTS.Count(t => t == "TP") / (double)ViolationStringsTS.Count(t => t == "TP" || t == "FP");
            public double RecallTS => (double)ViolationStringsTS.Count(t => t == "TP") / (double)ViolationStringsTS.Count(t => t == "TP" || t == "FN");
            public double SpecificityTS => (double)ViolationStringsTS.Count(t => t == "TN") / (double)ViolationStringsTS.Count(t => t == "TN" || t == "FP");
            public double FalsePositiveRateTS => (double)ViolationStringsTS.Count(t => t == "FP") / (double)ViolationStringsTS.Count(t => t == "FP" || t == "TN");
            public double NegativePredictedValueTS => (double)ViolationStringsTS.Count(t => t == "TN") / (double)ViolationStringsTS.Count(t => t == "TN" || t == "FP");
            public double AccuracyTS => (double)ViolationStringsTS.Count(t => t == "TN" || t == "TP") / (double)ViolationStringsTS.Count;
            public double FMeasureTS => (Math.Pow(1 + FmetricBeta, 2) * PrecisionTS * RecallTS) / ((Math.Pow(FmetricBeta, 2) * PrecisionTS) + RecallTS);


            //regression prediction
            public double PredictionMedianSP => Median(PredictionAccuraciesSP.ToArray());
            public double PredictionMedianTS => Median(PredictionAccuraciesTS.ToArray());

            //numeric metrics
            public double MSE_SP => DeviationsAbsoluteSP.Sum(t => Math.Pow(t, 2)) / DeviationsAbsoluteSP.Count;
            public double RMSE_SP => Math.Sqrt(DeviationsAbsoluteSP.Sum(t => Math.Pow(t, 2)) / DeviationsAbsoluteSP.Count);
            public double MAE_SP => DeviationsAbsoluteSP.Sum(t => Math.Abs(t)) / DeviationsAbsoluteSP.Count;
            public double RSE_SP => DeviationsAbsoluteSP.Sum(t => Math.Pow(t, 2)) / (Prediction_SP.Sum(t => Math.Pow(t - Prediction_SP.Average(),2)));
            public double RRSE_SP => Math.Sqrt(DeviationsAbsoluteSP.Sum(t => Math.Pow(t, 2)) / (Prediction_SP.Sum(t => Math.Pow(t - Prediction_SP.Average(), 2))));
            public double RAE_SP => DeviationsAbsoluteSP.Sum(t => Math.Abs(t)) /
                                    (Prediction_SP.Sum(t => Math.Abs(t - Prediction_SP.Average())));

            public double MSE_TS => DeviationsAbsoluteTS.Sum(t => Math.Pow(t, 2)) / DeviationsAbsoluteTS.Count;
            public double RMSE_TS => Math.Sqrt(DeviationsAbsoluteTS.Sum(t => Math.Pow(t, 2)) / DeviationsAbsoluteTS.Count);
            public double MAE_TS => DeviationsAbsoluteTS.Sum(t => Math.Abs(t)) / DeviationsAbsoluteTS.Count;
            public double RSE_TS => DeviationsAbsoluteTS.Sum(t => Math.Pow(t, 2)) / (Prediction_TS.Sum(t => Math.Pow(t - Prediction_TS.Average(), 2)));
            public double RRSE_TS => Math.Sqrt(DeviationsAbsoluteTS.Sum(t => Math.Pow(t, 2)) / (Prediction_TS.Sum(t => Math.Pow(t - Prediction_TS.Average(), 2))));
            public double RAE_TS => DeviationsAbsoluteTS.Sum(t => Math.Abs(t)) /
                                    (Prediction_TS.Sum(t => Math.Abs(t - Prediction_TS.Average())));

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
