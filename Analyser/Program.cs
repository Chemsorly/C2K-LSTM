using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using SearchOption = System.IO.SearchOption;

namespace Analyser
{
    class Program
    {
        private static readonly double BucketGranularity = 0.1; //creates a bucket every 0.05 of completion
        private static readonly double FmetricBeta = 1;

        //bucketing type: defines how results are bucketet
        //1 = normal bucketing over all results
        //2 = triple ranged: 0% - 50%, 50%, 50% - 100%
        private static readonly int BucketingType = 2;
        
        private static readonly int PlotModelWidth = 512;
        private static readonly int PlotModelHeight = 512;

        static void Main(string[] args)
        {
            System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;

            DirectoryInfo InFolder = new DirectoryInfo(@"Y:\Sicherung\Adrian\Sync\Sciebo\MA RNN-LSTM Results\raw");
            DirectoryInfo ResultsFolder = new DirectoryInfo(@"Y:\Sicherung\Adrian\Sync\Sciebo\MA RNN-LSTM Results");
            List<FileInfo> InFiles = InFolder.EnumerateFiles("*",SearchOption.AllDirectories).Where(t => t.Name.Contains(".csv") && !t.Name.Contains(".edited.csv")).ToList();

            int maxSequences = 0;
            List<Bucket> allBuckets = new List<Bucket>();
            List<String>[] allParameters = new List<String>[4];
            for(int i = 0; i < allParameters.Length; i++)
                allParameters[i] = new List<string>();

            #region init global models
            OxyPlot.PlotModel model_glob_precision_sp = new PlotModel() { Title = "Results: Precision SP" };
            model_glob_precision_sp.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0, Maximum = 0.95, Title = "Process completion" });
            model_glob_precision_sp.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1, Title = "Value" });
            model_glob_precision_sp.IsLegendVisible = true;

            OxyPlot.PlotModel model_glob_recall_sp = new PlotModel() { Title = "Results: Recall SP" };
            model_glob_recall_sp.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0, Maximum = 0.95, Title = "Process completion" });
            model_glob_recall_sp.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1, Title = "Value" });
            model_glob_recall_sp.IsLegendVisible = true;

            OxyPlot.PlotModel model_glob_speceficity_sp = new PlotModel() { Title = "Results: Speceficity SP" };
            model_glob_speceficity_sp.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0, Maximum = 0.95, Title = "Process completion" });
            model_glob_speceficity_sp.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1, Title = "Value" });
            model_glob_speceficity_sp.IsLegendVisible = true;

            OxyPlot.PlotModel model_glob_falsepositives_sp = new PlotModel() { Title = "Results: False Positives SP" };
            model_glob_falsepositives_sp.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0, Maximum = 0.95, Title = "Process completion" });
            model_glob_falsepositives_sp.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1, Title = "Value" });
            model_glob_falsepositives_sp.IsLegendVisible = true;

            OxyPlot.PlotModel model_glob_negativepredictions_sp = new PlotModel() { Title = "Results: Negative Predictions SP" };
            model_glob_negativepredictions_sp.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0, Maximum = 0.95, Title = "Process completion" });
            model_glob_negativepredictions_sp.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1, Title = "Value" });
            model_glob_negativepredictions_sp.IsLegendVisible = true;

            OxyPlot.PlotModel model_glob_accuracy_sp = new PlotModel() { Title = "Results: Accuracy SP" };
            model_glob_accuracy_sp.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0, Maximum = 0.95, Title = "Process completion" });
            model_glob_accuracy_sp.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1, Title = "Value" });
            model_glob_accuracy_sp.IsLegendVisible = true;

            //TS
            OxyPlot.PlotModel model_glob_precision_ts = new PlotModel() { Title = "Results: Precision TS" };
            model_glob_precision_ts.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0, Maximum = 0.95, Title = "Process completion" });
            model_glob_precision_ts.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1, Title = "Value" });
            model_glob_precision_ts.IsLegendVisible = true;

            OxyPlot.PlotModel model_glob_recall_ts = new PlotModel() { Title = "Results: Recall TS" };
            model_glob_recall_ts.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0, Maximum = 0.95, Title = "Process completion" });
            model_glob_recall_ts.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1, Title = "Value" });
            model_glob_recall_ts.IsLegendVisible = true;

            OxyPlot.PlotModel model_glob_speceficity_ts = new PlotModel() { Title = "Results: Speceficity TS" };
            model_glob_speceficity_ts.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0, Maximum = 0.95, Title = "Process completion" });
            model_glob_speceficity_ts.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1, Title = "Value" });
            model_glob_speceficity_ts.IsLegendVisible = true;

            OxyPlot.PlotModel model_glob_falsepositives_ts = new PlotModel() { Title = "Results: False Positives TS" };
            model_glob_falsepositives_ts.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0, Maximum = 0.95, Title = "Process completion" });
            model_glob_falsepositives_ts.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1, Title = "Value" });
            model_glob_falsepositives_ts.IsLegendVisible = true;

            OxyPlot.PlotModel model_glob_negativepredictions_ts = new PlotModel() { Title = "Results: Negative Predictions TS" };
            model_glob_negativepredictions_ts.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0, Maximum = 0.95, Title = "Process completion" });
            model_glob_negativepredictions_ts.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1, Title = "Value" });
            model_glob_negativepredictions_ts.IsLegendVisible = true;

            OxyPlot.PlotModel model_glob_accuracy_ts = new PlotModel() { Title = "Results: Accuracy TS" };
            model_glob_accuracy_ts.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0, Maximum = 0.95, Title = "Process completion" });
            model_glob_accuracy_ts.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1, Title = "Value" });
            model_glob_accuracy_ts.IsLegendVisible = true;

            OxyPlot.PlotModel model_glob_predictedsequences = new PlotModel() { Title = "Predicted sequences" };
            var model_glob_predictedsequences_cataxis = new CategoryAxis() {Position = AxisPosition.Bottom, Title = "Parameters", Angle = 45, FontSize = 8};
            model_glob_predictedsequences.Axes.Add(model_glob_predictedsequences_cataxis);
            model_glob_predictedsequences.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1, Title = "Percentage" });
            Dictionary<String,int> predictedSequences = new Dictionary<string, int>();

            OxyPlot.PlotModel model_glob_validsequences = new PlotModel(){Title = "Valid prediction sequences"};
            var model_glob_validsequences_cataxis = new CategoryAxis {Position = AxisPosition.Bottom, Title = "Parameters", Angle = 45, FontSize = 8 };
            model_glob_validsequences.Axes.Add(model_glob_validsequences_cataxis);
            model_glob_validsequences.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1, Title = "Percentage" });
            Dictionary<String, int> validSequences = new Dictionary<string, int>();

            //boxplots
            OxyPlot.PlotModel model_glob_boxplot_neurons_sp = new PlotModel() { Title = "Number of Neurons comparison (t >= 0.5)" };
            var model_glob_boxplot_neurons_sp_cataxis = new CategoryAxis { Position = AxisPosition.Bottom, Title = "Parameters", Angle = 45, FontSize = 8 };
            model_glob_boxplot_neurons_sp.Axes.Add(model_glob_boxplot_neurons_sp_cataxis);
            model_glob_boxplot_neurons_sp.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1, Title = "Value (Percentage)" });

            OxyPlot.PlotModel model_glob_boxplot_dropout_sp = new PlotModel() { Title = "Dropout comparison (t >= 0.5)" };
            var model_glob_boxplot_dropout_sp_cataxis = new CategoryAxis { Position = AxisPosition.Bottom, Title = "Parameters", Angle = 45, FontSize = 8 };
            model_glob_boxplot_dropout_sp.Axes.Add(model_glob_boxplot_dropout_sp_cataxis);
            model_glob_boxplot_dropout_sp.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1, Title = "Value (Percentage)" });

            OxyPlot.PlotModel model_glob_boxplot_patience_sp = new PlotModel() { Title = "Patience comparison (t >= 0.5)" };
            var model_glob_boxplot_patience_sp_cataxis = new CategoryAxis { Position = AxisPosition.Bottom, Title = "Parameters", Angle = 45, FontSize = 8 };
            model_glob_boxplot_patience_sp.Axes.Add(model_glob_boxplot_patience_sp_cataxis);
            model_glob_boxplot_patience_sp.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1, Title = "Value (Percentage)" });

            OxyPlot.PlotModel model_glob_boxplot_algorithm_sp = new PlotModel() { Title = "Algorithm comparison (t >= 0.5)" };
            var model_glob_boxplot_algorithm_sp_cataxis = new CategoryAxis() { Position = AxisPosition.Bottom, Title = "Parameters", Angle = 45, FontSize = 8 };
            model_glob_boxplot_algorithm_sp.Axes.Add(model_glob_boxplot_algorithm_sp_cataxis);
            model_glob_boxplot_algorithm_sp.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1, Title = "Value (Percentage)" });

            #endregion

            int counter = 0;
            foreach (var file in InFiles)
            {
                using (TextFieldParser parser = new TextFieldParser(file.FullName))
                {
                    List<Line> output = new List<Line>();

                    parser.TextFieldType = FieldType.Delimited;
                    parser.SetDelimiters(",");
                    bool firstline = true;
                    int rows = 0;
                    List<String> Parameters = ExtractParams(file.Name.Replace("results-", String.Empty).Replace(".csv", String.Empty));
                    //add to global
                    for (int i = 0; i < allParameters.Length; i++)
                        allParameters[i].Add(Parameters[i]);

                    while (!parser.EndOfData)
                    {
                        //rows
                        string[] fields = parser.ReadFields();
                        rows++;

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

                    //save longest sequence
                    if (rows > maxSequences)
                        maxSequences = rows;

                    //create buckets
                    List<Bucket> BucketList = new List<Bucket>();
                    for (int i = 0; i * BucketGranularity <= 1; i++)
                        BucketList.Add(new Bucket() { BucketLevel = i,
                            Parameters = Parameters,
                            Prediction_SP = new List<double>(),
                            Prediction_TS = new List<double>(),
                            ViolationStringsTS = new List<string>(),
                            ViolationStringsSP = new List<string>(),
                            PredictionAccuraciesSP = new List<double>(),
                            PredictionAccuraciesTS = new List<double>(),
                            DeviationsAbsoluteSP = new List<double>(),
                            DeviationsAbsoluteTS = new List<double>()
                        });

                    if (BucketingType == 1)
                    {
                        //fill buckets (classic)
                        foreach (var line in output)
                        {
                            //iterate until proper bucket found
                            for (int i = 0; i * BucketGranularity <= 1; i++)
                            {
                                if (line.Completion >= i * BucketGranularity &&
                                    line.Completion < (i + 1) * BucketGranularity)
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
                    }else if (BucketingType == 2)
                    {
                        //fill buckets (three ranged)
                        var midbucket =
                            BucketList.First(t => Math.Abs(t.BucketLevel * BucketGranularity - 0.5) < 0.001);
                        foreach (var line in output)
                        {
                            //case prediction = 13 (50% marker)
                            if (line.PredictedActivities[0] == '1' && line.PredictedActivities[0] == '3')
                            {
                                line.Completion = 0.5d;
                                midbucket.Prediction_SP.Add(line.SumPrevious);
                                midbucket.Prediction_TS.Add(line.Timestamp);
                                midbucket.ViolationStringsSP.Add(line.Violation_StringSP);
                                midbucket.ViolationStringsTS.Add(line.Violation_StringTS);
                                midbucket.PredictionAccuraciesSP.Add(line.AccuracySumprevious);
                                midbucket.PredictionAccuraciesTS.Add(line.AccuracyTimestamp);
                                midbucket.DeviationsAbsoluteSP.Add(line.DeviationAbsoluteSumprevious);
                                midbucket.DeviationsAbsoluteTS.Add(line.DeviationAbsoluteTimestamp);
                            }
                            //case prediction (suffix) contains 13 and prefix does not (<50%)
                            else if (line.PredictedActivities.Contains("13") && !line.PrefixActivities.Contains("13"))
                            {
                                var listout = line.PredictedActivities.Split(' ').ToList();
                                var indexout = listout.IndexOf("13");
                                var completion = (double) (line.Prefix) / (double) ((line.Prefix + indexout) * 2);
                                line.Completion = completion; //overwrite old values
                                //iterate until proper bucket found
                                for (int i = 0; i * BucketGranularity <= 1; i++)
                                {
                                    if (completion >= i * BucketGranularity && completion < (i + 1) * BucketGranularity)
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
                            //case prediction (suffix) does not contain 13 and prefix does (>50%)
                            else if (!line.PredictedActivities.Contains("13") && line.PrefixActivities.Contains("13"))
                            {
                                var listout = line.PrefixActivities.Split(' ').ToList();
                                var indexout = listout.IndexOf("13");
                                var completion = ((double) (line.Prefix - indexout) /
                                                  (double) ((line.Prefix - indexout +
                                                             line.PredictedActivities.Split(' ').Length) * 2) + 0.5d);
                                line.Completion = completion; //overwrite old values
                                //iterate until proper bucket found
                                for (int i = 0; i * BucketGranularity <= 1; i++)
                                {
                                    if (completion >= i * BucketGranularity && completion < (i + 1) * BucketGranularity)
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
                            } //else if case: not in range, ignore
                            else
                            {
                                //invalid sequence
                                line.Completion = -1d;
                            }
                        }
                    }
                    else
                        throw new Exception("unknown bucketing type defined");

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
                                   "deviation_abs_ts," +
                                   "valid_suffix");
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
                                       $"{line.DeviationAbsoluteTimestamp},"+
                                       $"{line.IsValidPrediction}");
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

                    //plot and export
                    //SP series
                    OxyPlot.PlotModel model_sp = new PlotModel() {Title = "Results" };
                    model_sp.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0, Maximum = 0.95, Title = "Process completion"});
                    model_sp.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1, Title = "Value"});
                    model_sp.IsLegendVisible = true;

                    var precisionSeries_sp = new LineSeries() {Title = "Precision_sp" };
                    var recallSeries_sp = new LineSeries() { Title = "Recall_sp" };
                    var speceficitySeries_sp = new LineSeries() { Title = "Speceficity_sp" };
                    var falsepositivesSeries_sp = new LineSeries() { Title = "False Positives_sp" };
                    var negativepredictedSeries_sp = new LineSeries() { Title = "Negative Predictions_sp" };
                    var accuracySeries_sp = new LineSeries() { Title = "Accuracy_sp" };

                    var precisionSeries_sp_global = new LineSeries() { Title = String.Join(" ", Parameters)};
                    var recallSeries_sp_global = new LineSeries() { Title = String.Join(" ", Parameters) };
                    var speceficitySeries_sp_global = new LineSeries() { Title = String.Join(" ", Parameters) };
                    var falsepositivesSeries_sp_global = new LineSeries() { Title = String.Join(" ", Parameters) };
                    var negativepredictedSeries_sp_global = new LineSeries() { Title = String.Join(" ", Parameters) };
                    var accuracySeries_sp_global = new LineSeries() { Title = String.Join(" ", Parameters) };

                    foreach (var bucket in BucketList)
                    {
                        //add to full list
                        allBuckets.Add(bucket);

                        precisionSeries_sp.Points.Add(new DataPoint(bucket.BucketLevel * BucketGranularity, bucket.PrecisionSP));
                        recallSeries_sp.Points.Add(new DataPoint(bucket.BucketLevel * BucketGranularity, bucket.RecallSP));
                        speceficitySeries_sp.Points.Add(new DataPoint(bucket.BucketLevel * BucketGranularity, bucket.SpecificitySP));
                        falsepositivesSeries_sp.Points.Add(new DataPoint(bucket.BucketLevel * BucketGranularity, bucket.FalsePositiveRateSP));
                        negativepredictedSeries_sp.Points.Add(new DataPoint(bucket.BucketLevel * BucketGranularity, bucket.NegativePredictedValueSP));
                        accuracySeries_sp.Points.Add(new DataPoint(bucket.BucketLevel * BucketGranularity, bucket.AccuracySP));

                        precisionSeries_sp_global.Points.Add(new DataPoint(bucket.BucketLevel * BucketGranularity, bucket.PrecisionSP));
                        recallSeries_sp_global.Points.Add(new DataPoint(bucket.BucketLevel * BucketGranularity, bucket.RecallSP));
                        speceficitySeries_sp_global.Points.Add(new DataPoint(bucket.BucketLevel * BucketGranularity, bucket.SpecificitySP));
                        falsepositivesSeries_sp_global.Points.Add(new DataPoint(bucket.BucketLevel * BucketGranularity, bucket.FalsePositiveRateSP));
                        negativepredictedSeries_sp_global.Points.Add(new DataPoint(bucket.BucketLevel * BucketGranularity, bucket.NegativePredictedValueSP));
                        accuracySeries_sp_global.Points.Add(new DataPoint(bucket.BucketLevel * BucketGranularity, bucket.AccuracySP));
                    }

                    model_sp.Series.Add(precisionSeries_sp);
                    model_sp.Series.Add(recallSeries_sp);
                    model_sp.Series.Add(speceficitySeries_sp);
                    model_sp.Series.Add(falsepositivesSeries_sp);
                    model_sp.Series.Add(negativepredictedSeries_sp);
                    model_sp.Series.Add(accuracySeries_sp);
                    using (var filestream = new FileStream($"{file.FullName.Replace(".csv", "")}.plot_sp.pdf", FileMode.OpenOrCreate))
                    {
                        OxyPlot.PdfExporter.Export(model_sp, filestream, PlotModelWidth, PlotModelHeight);
                        filestream.Close();
                    }
                    //add to global
                    model_glob_precision_sp.Series.Add(precisionSeries_sp_global);
                    model_glob_recall_sp.Series.Add(recallSeries_sp_global);
                    model_glob_speceficity_sp.Series.Add(speceficitySeries_sp_global);
                    model_glob_falsepositives_sp.Series.Add(falsepositivesSeries_sp_global);
                    model_glob_negativepredictions_sp.Series.Add(negativepredictedSeries_sp_global);
                    model_glob_accuracy_sp.Series.Add(accuracySeries_sp_global);

                    //TS series
                    OxyPlot.PlotModel model_ts = new PlotModel() { Title = "Results" };
                    model_ts.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0, Maximum = 0.95, Title = "Process completion" });
                    model_ts.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1, Title = "Value" });
                    model_ts.IsLegendVisible = true;

                    var precisionSeries_ts = new LineSeries() { Title = "Precision_ts" };
                    var recallSeries_ts = new LineSeries() { Title = "Recall_ts" };
                    var speceficitySeries_ts = new LineSeries() { Title = "Speceficity_ts" };
                    var falsepositivesSeries_ts = new LineSeries() { Title = "False Positives_ts" };
                    var negativepredictedSeries_ts = new LineSeries() { Title = "Negative Predictions_ts" };
                    var accuracySeries_ts = new LineSeries() { Title = "Accuracy_ts" };

                    var precisionSeries_ts_global = new LineSeries() { Title = String.Join(" ", Parameters) };
                    var recallSeries_ts_global = new LineSeries() { Title = String.Join(" ", Parameters) };
                    var speceficitySeries_ts_global = new LineSeries() { Title = String.Join(" ", Parameters) };
                    var falsepositivesSeries_ts_global = new LineSeries() { Title = String.Join(" ", Parameters) };
                    var negativepredictedSeries_ts_global = new LineSeries() { Title = String.Join(" ", Parameters) };
                    var accuracySeries_ts_global = new LineSeries() { Title = String.Join(" ", Parameters) };

                    foreach (var bucket in BucketList)
                    {
                        precisionSeries_ts.Points.Add(new DataPoint(bucket.BucketLevel * BucketGranularity, bucket.PrecisionTS));
                        recallSeries_ts.Points.Add(new DataPoint(bucket.BucketLevel * BucketGranularity, bucket.RecallTS));
                        speceficitySeries_ts.Points.Add(new DataPoint(bucket.BucketLevel * BucketGranularity, bucket.SpecificityTS));
                        falsepositivesSeries_ts.Points.Add(new DataPoint(bucket.BucketLevel * BucketGranularity, bucket.FalsePositiveRateTS));
                        negativepredictedSeries_ts.Points.Add(new DataPoint(bucket.BucketLevel * BucketGranularity, bucket.NegativePredictedValueTS));
                        accuracySeries_ts.Points.Add(new DataPoint(bucket.BucketLevel * BucketGranularity, bucket.AccuracyTS));

                        precisionSeries_ts_global.Points.Add(new DataPoint(bucket.BucketLevel * BucketGranularity, bucket.PrecisionTS));
                        recallSeries_ts_global.Points.Add(new DataPoint(bucket.BucketLevel * BucketGranularity, bucket.RecallTS));
                        speceficitySeries_ts_global.Points.Add(new DataPoint(bucket.BucketLevel * BucketGranularity, bucket.SpecificityTS));
                        falsepositivesSeries_ts_global.Points.Add(new DataPoint(bucket.BucketLevel * BucketGranularity, bucket.FalsePositiveRateTS));
                        negativepredictedSeries_ts_global.Points.Add(new DataPoint(bucket.BucketLevel * BucketGranularity, bucket.NegativePredictedValueTS));
                        accuracySeries_ts_global.Points.Add(new DataPoint(bucket.BucketLevel * BucketGranularity, bucket.AccuracyTS));
                    }

                    model_ts.Series.Add(precisionSeries_ts);
                    model_ts.Series.Add(recallSeries_ts);
                    model_ts.Series.Add(speceficitySeries_ts);
                    model_ts.Series.Add(falsepositivesSeries_ts);
                    model_ts.Series.Add(negativepredictedSeries_ts);
                    model_ts.Series.Add(accuracySeries_ts);
                    using (var filestream = new FileStream($"{file.FullName.Replace(".csv", "")}.plot_ts.pdf", FileMode.OpenOrCreate))
                    {
                        OxyPlot.PdfExporter.Export(model_ts, filestream, PlotModelWidth, PlotModelHeight);
                        filestream.Close();
                    }
                    //add to global
                    model_glob_precision_ts.Series.Add(precisionSeries_ts_global);
                    model_glob_recall_ts.Series.Add(recallSeries_ts_global);
                    model_glob_speceficity_ts.Series.Add(speceficitySeries_ts_global);
                    model_glob_falsepositives_ts.Series.Add(falsepositivesSeries_ts_global);
                    model_glob_negativepredictions_ts.Series.Add(negativepredictedSeries_ts_global);
                    model_glob_accuracy_ts.Series.Add(accuracySeries_ts_global);

                    //get valid/predicted sequences
                    validSequences.Add(String.Join(" ", Parameters), output.Count(t => t.IsValidPrediction));
                    predictedSequences.Add(String.Join(" ", Parameters), output.Count);
                }

                counter++;
                Console.WriteLine($"finished file {counter}");
            }

            #region print global
            using (var filestream = new FileStream($"{ResultsFolder.FullName}\\global_precision_sp.pdf", FileMode.OpenOrCreate))
            {
                OxyPlot.PdfExporter.Export(model_glob_precision_sp, filestream, PlotModelWidth, PlotModelHeight);
                filestream.Close();
            }
            using (var filestream = new FileStream($"{ResultsFolder.FullName}\\global_recall_sp.pdf", FileMode.OpenOrCreate))
            {
                OxyPlot.PdfExporter.Export(model_glob_recall_sp, filestream, PlotModelWidth, PlotModelHeight);
                filestream.Close();
            }
            using (var filestream = new FileStream($"{ResultsFolder.FullName}\\global_speceficity_sp.pdf", FileMode.OpenOrCreate))
            {
                OxyPlot.PdfExporter.Export(model_glob_speceficity_sp, filestream, PlotModelWidth, PlotModelHeight);
                filestream.Close();
            }
            using (var filestream = new FileStream($"{ResultsFolder.FullName}\\global_falsepositives_sp.pdf", FileMode.OpenOrCreate))
            {
                OxyPlot.PdfExporter.Export(model_glob_falsepositives_sp, filestream, PlotModelWidth, PlotModelHeight);
                filestream.Close();
            }
            using (var filestream = new FileStream($"{ResultsFolder.FullName}\\global_negativepredictions_sp.pdf", FileMode.OpenOrCreate))
            {
                OxyPlot.PdfExporter.Export(model_glob_negativepredictions_sp, filestream, PlotModelWidth, PlotModelHeight);
                filestream.Close();
            }
            using (var filestream = new FileStream($"{ResultsFolder.FullName}\\global_accuracy_sp.pdf", FileMode.OpenOrCreate))
            {
                OxyPlot.PdfExporter.Export(model_glob_accuracy_sp, filestream, PlotModelWidth, PlotModelHeight);
                filestream.Close();
            }

            //ts
            using (var filestream = new FileStream($"{ResultsFolder.FullName}\\global_precision_ts.pdf", FileMode.OpenOrCreate))
            {
                OxyPlot.PdfExporter.Export(model_glob_precision_ts, filestream, PlotModelWidth, PlotModelHeight);
                filestream.Close();
            }
            using (var filestream = new FileStream($"{ResultsFolder.FullName}\\global_recall_ts.pdf", FileMode.OpenOrCreate))
            {
                OxyPlot.PdfExporter.Export(model_glob_recall_ts, filestream, PlotModelWidth, PlotModelHeight);
                filestream.Close();
            }
            using (var filestream = new FileStream($"{ResultsFolder.FullName}\\global_speceficity_ts.pdf", FileMode.OpenOrCreate))
            {
                OxyPlot.PdfExporter.Export(model_glob_speceficity_ts, filestream, PlotModelWidth, PlotModelHeight);
                filestream.Close();
            }
            using (var filestream = new FileStream($"{ResultsFolder.FullName}\\global_falsepositives_ts.pdf", FileMode.OpenOrCreate))
            {
                OxyPlot.PdfExporter.Export(model_glob_falsepositives_ts, filestream, PlotModelWidth, PlotModelHeight);
                filestream.Close();
            }
            using (var filestream = new FileStream($"{ResultsFolder.FullName}\\global_negativepredictions_ts.pdf", FileMode.OpenOrCreate))
            {
                OxyPlot.PdfExporter.Export(model_glob_negativepredictions_ts, filestream, PlotModelWidth, PlotModelHeight);
                filestream.Close();
            }
            using (var filestream = new FileStream($"{ResultsFolder.FullName}\\global_accuracy_ts.pdf", FileMode.OpenOrCreate))
            {
                OxyPlot.PdfExporter.Export(model_glob_accuracy_ts, filestream, PlotModelWidth, PlotModelHeight);
                filestream.Close();
            }

            //prediction validity
            ColumnSeries validpredictionSeries = new ColumnSeries();
            model_glob_validsequences.Series.Add(validpredictionSeries);
            foreach (var entry in validSequences)
            {
                model_glob_validsequences_cataxis.ActualLabels.Add(entry.Key);
                validpredictionSeries.Items.Add(new ColumnItem() {Value = (double)entry.Value / (double)maxSequences });
            }
            using (var filestream = new FileStream($"{ResultsFolder.FullName}\\global_valid_sequences.pdf", FileMode.OpenOrCreate))
            {
                OxyPlot.PdfExporter.Export(model_glob_validsequences, filestream, PlotModelWidth *3, PlotModelHeight);
                filestream.Close();
            }

            ColumnSeries predictionsSeries = new ColumnSeries();
            model_glob_predictedsequences.Series.Add(predictionsSeries);
            foreach (var entry in predictedSequences)
            {
                model_glob_predictedsequences_cataxis.ActualLabels.Add(entry.Key);
                predictionsSeries.Items.Add(new ColumnItem() {Value = (double)entry.Value / (double)maxSequences });
            }
            using (var filestream = new FileStream($"{ResultsFolder.FullName}\\global_predicted_sequences.pdf", FileMode.OpenOrCreate))
            {
                OxyPlot.PdfExporter.Export(model_glob_predictedsequences, filestream, PlotModelWidth * 3, PlotModelHeight);
                filestream.Close();
            }
            #endregion

            #region boxplots
            var boxplotSeries_sp_neurons = new BoxPlotSeries() {};
            model_glob_boxplot_neurons_sp.Series.Add(boxplotSeries_sp_neurons);
            var parameters = allParameters[0].Distinct().Select(t => Double.Parse(t)).ToList();
            parameters.Sort();
            for (int i = 0; i < parameters.Count; i++)
            {
                model_glob_boxplot_neurons_sp_cataxis.ActualLabels.Add($"Precision {parameters[i]}");
                boxplotSeries_sp_neurons.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[0] == parameters[i].ToString()).Select(t => t.PrecisionSP).ToList(), i));
            }
            for (int i = 0; i < parameters.Count; i++)
            {
                model_glob_boxplot_neurons_sp_cataxis.ActualLabels.Add($"Recall {parameters[i]}");
                boxplotSeries_sp_neurons.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[0] == parameters[i].ToString()).Select(t => t.RecallSP).ToList(), i + (parameters.Count * 1)));
            }
            for (int i = 0; i < parameters.Count; i++)
            {
                model_glob_boxplot_neurons_sp_cataxis.ActualLabels.Add($"Speceficity {parameters[i]}");
                boxplotSeries_sp_neurons.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[0] == parameters[i].ToString()).Select(t => t.SpecificitySP).ToList(), i + (parameters.Count * 2)));
            }
            for (int i = 0; i < parameters.Count; i++)
            {
                model_glob_boxplot_neurons_sp_cataxis.ActualLabels.Add($"FalsePositive {parameters[i]}");
                boxplotSeries_sp_neurons.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[0] == parameters[i].ToString()).Select(t => t.FalsePositiveRateSP).ToList(), i + (parameters.Count * 3)));
            }
            for (int i = 0; i < parameters.Count; i++)
            {
                model_glob_boxplot_neurons_sp_cataxis.ActualLabels.Add($"NegativePredicted {parameters[i]}");
                boxplotSeries_sp_neurons.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[0] == parameters[i].ToString()).Select(t => t.NegativePredictedValueSP).ToList(), i + (parameters.Count * 4)));
            }
            for (int i = 0; i < parameters.Count; i++)
            {
                model_glob_boxplot_neurons_sp_cataxis.ActualLabels.Add($"Accuracy {parameters[i]}");
                boxplotSeries_sp_neurons.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[0] == parameters[i].ToString()).Select(t => t.AccuracySP).ToList(), i + (parameters.Count * 5)));
            }
            using (var filestream = new FileStream($"{ResultsFolder.FullName}\\global_boxplot_neurons.pdf",FileMode.OpenOrCreate))
            {
                OxyPlot.PdfExporter.Export(model_glob_boxplot_neurons_sp, filestream, PlotModelWidth * 3, PlotModelHeight);
                filestream.Close();
            }

            var boxplotSeries_sp_dropout = new BoxPlotSeries() { };
            model_glob_boxplot_dropout_sp.Series.Add(boxplotSeries_sp_dropout);
            parameters = allParameters[1].Distinct().Select(t => Double.Parse(t)).ToList();
            parameters.Sort();
            for (int i = 0; i < parameters.Count; i++)
            {
                model_glob_boxplot_dropout_sp_cataxis.ActualLabels.Add($"Precision {parameters[i]}");
                boxplotSeries_sp_dropout.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[1] == parameters[i].ToString()).Select(t => t.PrecisionSP).ToList(), i));
            }
            for (int i = 0; i < parameters.Count; i++)
            {
                model_glob_boxplot_dropout_sp_cataxis.ActualLabels.Add($"Recall {parameters[i]}");
                boxplotSeries_sp_dropout.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[1] == parameters[i].ToString()).Select(t => t.RecallSP).ToList(), i + (parameters.Count * 1)));
            }
            for (int i = 0; i < parameters.Count; i++)
            {
                model_glob_boxplot_dropout_sp_cataxis.ActualLabels.Add($"Speceficity {parameters[i]}");
                boxplotSeries_sp_dropout.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[1] == parameters[i].ToString()).Select(t => t.SpecificitySP).ToList(), i + (parameters.Count * 2)));
            }
            for (int i = 0; i < parameters.Count; i++)
            {
                model_glob_boxplot_dropout_sp_cataxis.ActualLabels.Add($"FalsePositive {parameters[i]}");
                boxplotSeries_sp_dropout.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[1] == parameters[i].ToString()).Select(t => t.FalsePositiveRateSP).ToList(), i + (parameters.Count * 3)));
            }
            for (int i = 0; i < parameters.Count; i++)
            {
                model_glob_boxplot_dropout_sp_cataxis.ActualLabels.Add($"NegativePredicted {parameters[i]}");
                boxplotSeries_sp_dropout.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[1] == parameters[i].ToString()).Select(t => t.NegativePredictedValueSP).ToList(), i + (parameters.Count * 4)));
            }
            for (int i = 0; i < parameters.Count; i++)
            {
                model_glob_boxplot_dropout_sp_cataxis.ActualLabels.Add($"Accuracy {parameters[i]}");
                boxplotSeries_sp_dropout.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[1] == parameters[i].ToString()).Select(t => t.AccuracySP).ToList(), i + (parameters.Count * 5)));
            }
            using (var filestream = new FileStream($"{ResultsFolder.FullName}\\global_boxplot_dropout.pdf", FileMode.OpenOrCreate))
            {
                OxyPlot.PdfExporter.Export(model_glob_boxplot_dropout_sp, filestream, PlotModelWidth * 3, PlotModelHeight);
                filestream.Close();
            }

            var boxplotSeries_sp_patience = new BoxPlotSeries() { };
            model_glob_boxplot_patience_sp.Series.Add(boxplotSeries_sp_patience);
            parameters = allParameters[2].Distinct().Select(t => Double.Parse(t)).ToList();
            parameters.Sort();
            for (int i = 0; i < parameters.Count; i++)
            {
                model_glob_boxplot_patience_sp_cataxis.ActualLabels.Add($"Precision {parameters[i]}");
                boxplotSeries_sp_patience.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[2] == parameters[i].ToString()).Select(t => t.PrecisionSP).ToList(), i));
            }
            for (int i = 0; i < parameters.Count; i++)
            {
                model_glob_boxplot_patience_sp_cataxis.ActualLabels.Add($"Recall {parameters[i]}");
                boxplotSeries_sp_patience.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[2] == parameters[i].ToString()).Select(t => t.RecallSP).ToList(), i + (parameters.Count * 1)));
            }
            for (int i = 0; i < parameters.Count; i++)
            {
                model_glob_boxplot_patience_sp_cataxis.ActualLabels.Add($"Speceficity {parameters[i]}");
                boxplotSeries_sp_patience.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[2] == parameters[i].ToString()).Select(t => t.SpecificitySP).ToList(), i + (parameters.Count * 2)));
            }
            for (int i = 0; i < parameters.Count; i++)
            {
                model_glob_boxplot_patience_sp_cataxis.ActualLabels.Add($"FalsePositive {parameters[i]}");
                boxplotSeries_sp_patience.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[2] == parameters[i].ToString()).Select(t => t.FalsePositiveRateSP).ToList(), i + (parameters.Count * 3)));
            }
            for (int i = 0; i < parameters.Count; i++)
            {
                model_glob_boxplot_patience_sp_cataxis.ActualLabels.Add($"NegativePredicted {parameters[i]}");
                boxplotSeries_sp_patience.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[2] == parameters[i].ToString()).Select(t => t.NegativePredictedValueSP).ToList(), i + (parameters.Count * 4)));
            }
            for (int i = 0; i < parameters.Count; i++)
            {
                model_glob_boxplot_patience_sp_cataxis.ActualLabels.Add($"Accuracy {parameters[i]}");
                boxplotSeries_sp_patience.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[2] == parameters[i].ToString()).Select(t => t.AccuracySP).ToList(), i + (parameters.Count * 5)));
            }
            using (var filestream = new FileStream($"{ResultsFolder.FullName}\\global_boxplot_patience.pdf", FileMode.OpenOrCreate))
            {
                OxyPlot.PdfExporter.Export(model_glob_boxplot_patience_sp, filestream, PlotModelWidth * 3, PlotModelHeight);
                filestream.Close();
            }

            var boxplotSeries_sp_algorithm = new BoxPlotSeries() { };
            model_glob_boxplot_algorithm_sp.Series.Add(boxplotSeries_sp_algorithm);
            parameters = allParameters[3].Distinct().Select(t => Double.Parse(t)).ToList();
            parameters.Sort();
            for (int i = 0; i < parameters.Count; i++)
            {
                model_glob_boxplot_algorithm_sp_cataxis.ActualLabels.Add($"Precision {parameters[i]}");
                boxplotSeries_sp_algorithm.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[3] == parameters[i].ToString()).Select(t => t.PrecisionSP).ToList(), i));
            }
            for (int i = 0; i < parameters.Count; i++)
            {
                model_glob_boxplot_algorithm_sp_cataxis.ActualLabels.Add($"Recall {parameters[i]}");
                boxplotSeries_sp_algorithm.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[3] == parameters[i].ToString()).Select(t => t.RecallSP).ToList(), i + (parameters.Count * 1)));
            }
            for (int i = 0; i < parameters.Count; i++)
            {
                model_glob_boxplot_algorithm_sp_cataxis.ActualLabels.Add($"Speceficity {parameters[i]}");
                boxplotSeries_sp_algorithm.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[3] == parameters[i].ToString()).Select(t => t.SpecificitySP).ToList(), i + (parameters.Count * 2)));
            }
            for (int i = 0; i < parameters.Count; i++)
            {
                model_glob_boxplot_algorithm_sp_cataxis.ActualLabels.Add($"FalsePositive {parameters[i]}");
                boxplotSeries_sp_algorithm.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[3] == parameters[i].ToString()).Select(t => t.FalsePositiveRateSP).ToList(), i + (parameters.Count * 3)));
            }
            for (int i = 0; i < parameters.Count; i++)
            {
                model_glob_boxplot_algorithm_sp_cataxis.ActualLabels.Add($"NegativePredicted {parameters[i]}");
                boxplotSeries_sp_algorithm.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[3] == parameters[i].ToString()).Select(t => t.NegativePredictedValueSP).ToList(), i + (parameters.Count * 4)));
            }
            for (int i = 0; i < parameters.Count; i++)
            {
                model_glob_boxplot_algorithm_sp_cataxis.ActualLabels.Add($"Accuracy {parameters[i]}");
                boxplotSeries_sp_algorithm.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[3] == parameters[i].ToString()).Select(t => t.AccuracySP).ToList(), i + (parameters.Count * 5)));
            }
            using (var filestream = new FileStream($"{ResultsFolder.FullName}\\global_boxplot_algorithm.pdf", FileMode.OpenOrCreate))
            {
                OxyPlot.PdfExporter.Export(model_glob_boxplot_algorithm_sp, filestream, PlotModelWidth * 3, PlotModelHeight);
                filestream.Close();
            }
            #endregion
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

        public static List<String> ExtractParams(String pParameterString)
        {
            var paras = pParameterString.Split(' ');

            //we only want 1,2,3; neurons, dropout, patience, algorithm
            return new List<string>() {paras[1], paras[2] , paras[3], paras[4] };
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
            public bool IsValidPrediction => IsValidSequence(PredictedActivities, PrefixActivities);

        }

        class Bucket
        {
            public int BucketLevel { get; set; }
            public List<String> Parameters { get; set; }

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

        static bool IsValidSequence(String pPredictionSequence, String pPrefixSequence)
        {
            //split sequence by whitespace
            List<int> sequence = pPredictionSequence.Split(' ').Select(int.Parse).ToList();

            //iterate
            for (int i = 0; i < sequence.Count; i++)
            {
                var leg = GetTransportLeg(sequence[i]);

                //get next item from same transport leg
                for (int j = i + 1; j < sequence.Count; j++)
                {
                    if (GetTransportLeg(sequence[j]) == leg)
                    {
                        //check if next item in leg is valid
                        var valid = GetValidFollowing(sequence[i]);
                        if (valid.Any() && !valid.Contains(sequence[j]))
                        {
                            return false;
                        }else if (!valid.Any())
                            return false;

                        break;
                    }
                }
            }

            //special case 13
            List<int> prefixsequence = pPrefixSequence.Split(' ').Select(int.Parse).ToList();
            if (sequence.Count(t => t == 13) + prefixsequence.Count(t => t == 13) != 1)
                return false;

            return true;
        }

        static int GetTransportLeg(int pNumber)
        {
            switch (pNumber)
            {
                case 1:
                    return 1;
                case 2:
                    return 1;
                case 3:
                    return 1;
                case 4:
                    return 1;
                case 5:
                    return 2;
                case 6:
                    return 2;
                case 7:
                    return 2;
                case 8:
                    return 2;
                case 9:
                    return 3;
                case 10:
                    return 3;
                case 11:
                    return 3;
                case 12:
                    return 3;
                case 13:
                    return 4;
                case 14:
                    return 4;
                case 15:
                    return 4;
                case 16:
                    return 4;
                default:
                    throw new Exception("unknown transport leg");
            }
        }

        static List<int> GetValidFollowing(int pNumber)
        {
            switch (pNumber)
            {
                case 1:
                    return new List<int>() { 2 };
                case 2:
                    return new List<int>() { 3 };
                case 3:
                    return new List<int>() { 2,4 };
                case 4:
                    return new List<int>() { };
                case 5:
                    return new List<int>() { 6 };
                case 6:
                    return new List<int>() { 7 };
                case 7:
                    return new List<int>() { 6,8 };
                case 8:
                    return new List<int>() {  };
                case 9:
                    return new List<int>() { 10 };
                case 10:
                    return new List<int>() { 11 };
                case 11:
                    return new List<int>() { 10,12 };
                case 12:
                    return new List<int>() { };
                case 13:
                    return new List<int>() { 14 };
                case 14:
                    return new List<int>() { 15 };
                case 15:
                    return new List<int>() { 14,16 };
                case 16:
                    return new List<int>() { };
                default:
                    throw new Exception("wront sequence token");
            }
        }

        static BoxPlotItem CreateBoxplot(List<double> pValues, double pX)
        {
            //https://searchcode.com/codesearch/view/28446375/
            var values = pValues.Where(t => !Double.IsNaN(t)).ToList();
            values.Sort();

            var median = Median(values.ToArray());
            int r = values.Count % 2;
            double firstQuartil = Median(values.Take((values.Count + r) / 2).ToArray());
            double thirdQuartil = Median(values.Skip((values.Count - r) / 2).ToArray());
            var iqr = thirdQuartil - firstQuartil;
            var step = iqr * 1.5;
            var upperWhisker = thirdQuartil + step;
            upperWhisker = values.Where(v => v <= upperWhisker).Max();
            var lowerWhisker = firstQuartil - step;
            lowerWhisker = values.Where(v => v >= lowerWhisker).Min();
            var outliers = values.Where(v => v > upperWhisker || v < lowerWhisker).ToList();

            return new BoxPlotItem(
                pX,
                lowerWhisker,
                firstQuartil,
                median,
                thirdQuartil,
                upperWhisker)
            {
                Outliers = outliers,
                
            };
        }
    }
}
