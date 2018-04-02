using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.ServiceModel.Channels;
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
            //enforce decimal encoding
            System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            CultureInfo.DefaultThreadCurrentCulture = customCulture;

            //target folders
            DirectoryInfo InFolder = new DirectoryInfo(@"Y:\Sicherung\Adrian\Sync\Sciebo\MA RNN-LSTM Results\Durchlauf 4\raw");
            DirectoryInfo ResultsFolder = new DirectoryInfo(@"Y:\Sicherung\Adrian\Sync\Sciebo\MA RNN-LSTM Results\Durchlauf 4\");
            List<FileInfo> InFiles = InFolder.EnumerateFiles("*", SearchOption.AllDirectories).Where(t => t.Name.Contains(".csv") && !t.Name.Contains(".edited.csv")).ToList();

            //globals
            int maxSequences = 0;
            List<Bucket> allBuckets = new List<Bucket>();
            List<String>[] allParameters = new List<String>[5];
            for (int i = 0; i < allParameters.Length; i++)
                allParameters[i] = new List<string>();

            #region init global models
            OxyPlot.PlotModel model_glob_precision_target = new PlotModel() { Title = "Results: Precision target" };
            model_glob_precision_target.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0, Maximum = 0.95, Title = "Process completion" });
            model_glob_precision_target.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1, Title = "Value" });
            model_glob_precision_target.IsLegendVisible = true;

            OxyPlot.PlotModel model_glob_recall_target = new PlotModel() { Title = "Results: Recall target" };
            model_glob_recall_target.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0, Maximum = 0.95, Title = "Process completion" });
            model_glob_recall_target.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1, Title = "Value" });
            model_glob_recall_target.IsLegendVisible = true;

            OxyPlot.PlotModel model_glob_speceficity_target = new PlotModel() { Title = "Results: Speceficity target" };
            model_glob_speceficity_target.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0, Maximum = 0.95, Title = "Process completion" });
            model_glob_speceficity_target.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1, Title = "Value" });
            model_glob_speceficity_target.IsLegendVisible = true;

            OxyPlot.PlotModel model_glob_falsepositives_target = new PlotModel() { Title = "Results: False Positives target" };
            model_glob_falsepositives_target.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0, Maximum = 0.95, Title = "Process completion" });
            model_glob_falsepositives_target.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1, Title = "Value" });
            model_glob_falsepositives_target.IsLegendVisible = true;

            OxyPlot.PlotModel model_glob_negativepredictions_target = new PlotModel() { Title = "Results: Negative Predictions target" };
            model_glob_negativepredictions_target.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0, Maximum = 0.95, Title = "Process completion" });
            model_glob_negativepredictions_target.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1, Title = "Value" });
            model_glob_negativepredictions_target.IsLegendVisible = true;

            OxyPlot.PlotModel model_glob_accuracy_target = new PlotModel() { Title = "Results: Accuracy target" };
            model_glob_accuracy_target.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0, Maximum = 0.95, Title = "Process completion" });
            model_glob_accuracy_target.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1, Title = "Value" });
            model_glob_accuracy_target.IsLegendVisible = true;

            OxyPlot.PlotModel model_glob_fmetric_target = new PlotModel() { Title = "Results: F-Metric target" };
            model_glob_fmetric_target.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0, Maximum = 0.95, Title = "Process completion" });
            model_glob_fmetric_target.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1, Title = "Value" });
            model_glob_fmetric_target.IsLegendVisible = true;

            OxyPlot.PlotModel model_glob_mcc_target = new PlotModel() { Title = "Results: MCC target" };
            model_glob_mcc_target.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0, Maximum = 0.95, Title = "Process completion" });
            model_glob_mcc_target.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1, Title = "Value" });
            model_glob_mcc_target.IsLegendVisible = true;           

            OxyPlot.PlotModel model_glob_predictedsequences = new PlotModel() { Title = "Predicted sequences" };
            var model_glob_predictedsequences_cataxis = new CategoryAxis() { Position = AxisPosition.Bottom, Title = "Parameters", Angle = 45, FontSize = 8 };
            model_glob_predictedsequences.Axes.Add(model_glob_predictedsequences_cataxis);
            model_glob_predictedsequences.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1, Title = "Percentage" });
            Dictionary<String, int> predictedSequences = new Dictionary<string, int>();

            OxyPlot.PlotModel model_glob_validsequences = new PlotModel() { Title = "Valid prediction sequences" };
            var model_glob_validsequences_cataxis = new CategoryAxis { Position = AxisPosition.Bottom, Title = "Parameters", Angle = 45, FontSize = 8 };
            model_glob_validsequences.Axes.Add(model_glob_validsequences_cataxis);
            model_glob_validsequences.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1, Title = "Percentage" });
            Dictionary<String, int> validSequences = new Dictionary<string, int>();

            //boxplots
            OxyPlot.PlotModel model_glob_boxplot_neurons_target = new PlotModel() { Title = "Number of Neurons comparison (t >= 0.5)" };
            var model_glob_boxplot_neurons_target_cataxis = new CategoryAxis { Position = AxisPosition.Bottom, Title = "Parameters", Angle = 45, FontSize = 8 };
            model_glob_boxplot_neurons_target.Axes.Add(model_glob_boxplot_neurons_target_cataxis);
            model_glob_boxplot_neurons_target.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1, Title = "Value (Percentage)" });

            OxyPlot.PlotModel model_glob_boxplot_dropout_target = new PlotModel() { Title = "Dropout comparison (t >= 0.5)" };
            var model_glob_boxplot_dropout_target_cataxis = new CategoryAxis { Position = AxisPosition.Bottom, Title = "Parameters", Angle = 45, FontSize = 8 };
            model_glob_boxplot_dropout_target.Axes.Add(model_glob_boxplot_dropout_target_cataxis);
            model_glob_boxplot_dropout_target.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1, Title = "Value (Percentage)" });

            OxyPlot.PlotModel model_glob_boxplot_patience_target = new PlotModel() { Title = "Patience comparison (t >= 0.5)" };
            var model_glob_boxplot_patience_target_cataxis = new CategoryAxis { Position = AxisPosition.Bottom, Title = "Parameters", Angle = 45, FontSize = 8 };
            model_glob_boxplot_patience_target.Axes.Add(model_glob_boxplot_patience_target_cataxis);
            model_glob_boxplot_patience_target.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1, Title = "Value (Percentage)" });

            OxyPlot.PlotModel model_glob_boxplot_algorithm_target = new PlotModel() { Title = "Algorithm comparison (t >= 0.5)" };
            var model_glob_boxplot_algorithm_target_cataxis = new CategoryAxis() { Position = AxisPosition.Bottom, Title = "Parameters", Angle = 45, FontSize = 8 };
            model_glob_boxplot_algorithm_target.Axes.Add(model_glob_boxplot_algorithm_target_cataxis);
            model_glob_boxplot_algorithm_target.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1, Title = "Value (Percentage)" });

            //boxplots
            OxyPlot.PlotModel model_glob_boxplot_neurons_ts = new PlotModel() { Title = "Number of Neurons comparison (t >= 0.5)" };
            var model_glob_boxplot_neurons_ts_cataxis = new CategoryAxis { Position = AxisPosition.Bottom, Title = "Parameters", Angle = 45, FontSize = 8 };
            model_glob_boxplot_neurons_ts.Axes.Add(model_glob_boxplot_neurons_ts_cataxis);
            model_glob_boxplot_neurons_ts.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1, Title = "Value (Percentage)" });

            OxyPlot.PlotModel model_glob_boxplot_dropout_ts = new PlotModel() { Title = "Dropout comparison (t >= 0.5)" };
            var model_glob_boxplot_dropout_ts_cataxis = new CategoryAxis { Position = AxisPosition.Bottom, Title = "Parameters", Angle = 45, FontSize = 8 };
            model_glob_boxplot_dropout_ts.Axes.Add(model_glob_boxplot_dropout_ts_cataxis);
            model_glob_boxplot_dropout_ts.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1, Title = "Value (Percentage)" });

            OxyPlot.PlotModel model_glob_boxplot_patience_ts = new PlotModel() { Title = "Patience comparison (t >= 0.5)" };
            var model_glob_boxplot_patience_ts_cataxis = new CategoryAxis { Position = AxisPosition.Bottom, Title = "Parameters", Angle = 45, FontSize = 8 };
            model_glob_boxplot_patience_ts.Axes.Add(model_glob_boxplot_patience_ts_cataxis);
            model_glob_boxplot_patience_ts.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1, Title = "Value (Percentage)" });

            OxyPlot.PlotModel model_glob_boxplot_algorithm_ts = new PlotModel() { Title = "Algorithm comparison (t >= 0.5)" };
            var model_glob_boxplot_algorithm_ts_cataxis = new CategoryAxis() { Position = AxisPosition.Bottom, Title = "Parameters", Angle = 45, FontSize = 8 };
            model_glob_boxplot_algorithm_ts.Axes.Add(model_glob_boxplot_algorithm_ts_cataxis);
            model_glob_boxplot_algorithm_ts.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1, Title = "Value (Percentage)" });

            #endregion

            int counter = 0;
            Parallel.ForEach(InFiles, file =>
            {
                using (TextFieldParser parser = new TextFieldParser(file.FullName))
                {
                    List<Line> output = new List<Line>();

                    parser.TextFieldType = FieldType.Delimited;
                    parser.SetDelimiters(",");
                    bool firstline = true;
                    int rows = 0;

                    //numeric or binary
                    bool IsBinaryPrediction = false;
                    //rgb encoding
                    bool IsRGBencoding = false;
                    //no path encoding
                    bool IsNopathEncoding = false;

                    TargetData TargetData = TargetData.SP;


                    //List<String> Parameters = ExtractParams(file.Name.Replace("results-", String.Empty).Replace(".csv", String.Empty));
                    List<String> Parameters = ExtractParams(file.Directory.Name);
                    if (Parameters.Any(t => t.Contains("binary")))
                    {
                        IsBinaryPrediction = true;
                        TargetData = TargetData.SP;
                    }
                    else if (Parameters.Any(t => t.Contains("numeric")))
                    {
                        IsBinaryPrediction = false;
                        if (Parameters.Any(t => t.Contains("s2s")))
                            TargetData = TargetData.SP;
                        else if (Parameters.Any(t => t.Contains("s2e")))
                            TargetData = TargetData.TS;
                        else
                            Console.WriteLine("sequencer not found");
                    }
                    else
                        Console.WriteLine("classifier not found");

                    if (Parameters.Any(t => t.Contains("rgb")))
                        IsRGBencoding = true;
                    else
                        IsRGBencoding = false;

                    if (Parameters.Any(t => t.Contains("nopath")))
                        IsNopathEncoding = true;
                    else
                        IsNopathEncoding = false;

                    //add to global
                    for (int i = 0; i < allParameters.Length; i++)
                        allParameters[i].Add(Parameters[i]);

                    //check parameters for 

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
                            IsBinaryPrediction = IsBinaryPrediction,
                            IsRGBEncoding = IsRGBencoding,

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
                            PredictedActivities = fields[11],
                            SuffixActivities = fields[12]
                        };
                        if (IsBinaryPrediction)
                            line.Predicted_Violations = fields[13] == "true";

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
                        BucketList.Add(new Bucket()
                        {
                            BucketLevel = i,
                            Parameters = Parameters,
                            Target = TargetData,
                            Prediction_SP = new List<double>(),
                            Prediction_TS = new List<double>(),
                            ViolationStringsTS = new List<string>(),
                            ViolationStringsSP = new List<string>(),
                            ViolationStringsTarget = new List<string>(),
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
                                    if (TargetData == TargetData.SP)
                                        BucketList[i].ViolationStringsTarget.Add(line.Violation_StringSP);
                                    else
                                        BucketList[i].ViolationStringsTarget.Add(line.Violation_StringTS);
                                    BucketList[i].PredictionAccuraciesSP.Add(line.AccuracySumprevious);
                                    BucketList[i].PredictionAccuraciesTS.Add(line.AccuracyTimestamp);
                                    BucketList[i].DeviationsAbsoluteSP.Add(line.DeviationAbsoluteSumprevious);
                                    BucketList[i].DeviationsAbsoluteTS.Add(line.DeviationAbsoluteTimestamp);
                                    break;
                                }
                            }
                        }
                    }
                    else if (BucketingType == 2)
                    {
                        //fill buckets (three ranged)
                        var midbucket =
                            BucketList.First(t => Math.Abs(t.BucketLevel * BucketGranularity - 0.5) < 0.001);
                        foreach (var line in output)
                        {
                            //get index of 50% point
                            var listout = (line.PrefixActivities + ' ' + line.SuffixActivities).Split(' ').ToList();
                            var indexout = -1;
                            if (IsRGBencoding)
                                indexout = listout.IndexOf("M");
                            else if (IsNopathEncoding)
                                indexout = listout.LastIndexOf("1");
                            else
                                indexout = listout.IndexOf("13");

                            if (indexout == -1)
                                throw new Exception("indexing failed");

                            //case prediction = 13 (50% marker)
                            if (indexout == line.Prefix)
                            {
                                line.Completion = 0.5d;
                                midbucket.Prediction_SP.Add(line.SumPrevious);
                                midbucket.Prediction_TS.Add(line.Timestamp);
                                midbucket.ViolationStringsSP.Add(line.Violation_StringSP);
                                midbucket.ViolationStringsTS.Add(line.Violation_StringTS);
                                if (TargetData == TargetData.SP)
                                    midbucket.ViolationStringsTarget.Add(line.Violation_StringSP);
                                else
                                    midbucket.ViolationStringsTarget.Add(line.Violation_StringTS);
                                midbucket.PredictionAccuraciesSP.Add(line.AccuracySumprevious);
                                midbucket.PredictionAccuraciesTS.Add(line.AccuracyTimestamp);
                                midbucket.DeviationsAbsoluteSP.Add(line.DeviationAbsoluteSumprevious);
                                midbucket.DeviationsAbsoluteTS.Add(line.DeviationAbsoluteTimestamp);
                            }
                            //case prediction (suffix) contains 13 and prefix does not (<50%)
                            else if (indexout > line.Prefix)
                            {
                                var completion = ((double)((line.Prefix) / (double)(indexout)) / 2);
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
                                        if (TargetData == TargetData.SP)
                                            BucketList[i].ViolationStringsTarget.Add(line.Violation_StringSP);
                                        else
                                            BucketList[i].ViolationStringsTarget.Add(line.Violation_StringTS);
                                        BucketList[i].PredictionAccuraciesSP.Add(line.AccuracySumprevious);
                                        BucketList[i].PredictionAccuraciesTS.Add(line.AccuracyTimestamp);
                                        BucketList[i].DeviationsAbsoluteSP.Add(line.DeviationAbsoluteSumprevious);
                                        BucketList[i].DeviationsAbsoluteTS.Add(line.DeviationAbsoluteTimestamp);
                                        break;
                                    }
                                }
                            }
                            //case prediction (suffix) does not contain 13 and prefix does (>50%)
                            else if (indexout < line.Prefix)
                            {
                                var completion = (((double)(line.Prefix - indexout) /
                                                   (double)(line.SequenceLength - indexout)) / 2) + 0.5d;
                                line.Completion = completion; //overwrite old values
                                //iterate until proper bucket found
                                for (int i = 0; i * BucketGranularity <= 1; i++)
                                {
                                    if (completion >= i * BucketGranularity && completion < (i + 1) * BucketGranularity)
                                    {
                                        if (BucketList[i] == midbucket)
                                            i++;

                                        BucketList[i].Prediction_SP.Add(line.SumPrevious);
                                        BucketList[i].Prediction_TS.Add(line.Timestamp);
                                        BucketList[i].ViolationStringsSP.Add(line.Violation_StringSP);
                                        BucketList[i].ViolationStringsTS.Add(line.Violation_StringTS);
                                        if (TargetData == TargetData.SP)
                                            BucketList[i].ViolationStringsTarget.Add(line.Violation_StringSP);
                                        else
                                            BucketList[i].ViolationStringsTarget.Add(line.Violation_StringTS);
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
                                   "suffix_activities," +
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
                                       $"{line.SuffixActivities}," +
                                       $"{line.AccuracySumprevious}," +
                                       $"{line.AccuracyTimestamp}," +
                                       $"{line.Violation_Effective}," +
                                       $"{line.Violation_PredictedSP}," +
                                       $"{line.Violation_PredictedTS}," +
                                       $"{line.Violation_StringSP}," +
                                       $"{line.Violation_StringTS}," +
                                       $"{line.DeviationAbsoluteSumprevious}," +
                                       $"{line.DeviationAbsoluteTimestamp}," +
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
                                   "MCC_SP," +
                                   "MSE_TS," +
                                   "RMSE_TS," +
                                   "MAE_TS," +
                                   "RSE_TS," +
                                   "RRSE_TS," +
                                   "RAE_TS," +
                                   "Precision_TS," +
                                   "Recall_TS," +
                                   "Specificity_TS," +
                                   "FalsePositiveRate_TS," +
                                   "NegativePredictedValue_TS," +
                                   "Accuracy_TS," +
                                   "F-Measure_TS," +
                                   "MCC_TS"
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
                                           $"{bucket.MCC_SP}," +
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
                                           $"{bucket.FMeasureTS}," +
                                           $"{bucket.MCC_TS}"
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
                                   $"{BucketList.Where(t => t.ViolationStringsSP.Count > 0).Average(t => t.MCC_SP)}," +
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
                                   $"{BucketList.Where(t => t.ViolationStringsTS.Count > 0).Average(t => t.FMeasureTS)}," +
                                   $"{BucketList.Where(t => t.ViolationStringsTS.Count > 0).Average(t => t.MCC_TS)}"
                    );

                    ////export as csv to match LSTM input examples
                    Task.Run(() => File.WriteAllLines($"{file.FullName.Replace(".csv", "")}.edited.csv", exportrows));

                    //plot and export
                    //SP series
                    OxyPlot.PlotModel model_target = new PlotModel() { Title = "Results" };
                    model_target.Axes.Add(new LinearAxis
                    {
                        Position = AxisPosition.Bottom,
                        Minimum = 0,
                        Maximum = 0.95,
                        Title = "Process completion"
                    });
                    model_target.Axes.Add(new LinearAxis
                    {
                        Position = AxisPosition.Left,
                        Minimum = 0,
                        Maximum = 1,
                        Title = "Value"
                    });
                    model_target.IsLegendVisible = true;

                    var precisionSeries_target = new LineSeries() { Title = "Precision_target" };
                    var recallSeries_target = new LineSeries() { Title = "Recall_target" };
                    var speceficitySeries_target = new LineSeries() { Title = "Speceficity_target" };
                    var falsepositivesSeries_target = new LineSeries() { Title = "False Positives_target" };
                    var negativepredictedSeries_target = new LineSeries() { Title = "Negative Predictions_target" };
                    var accuracySeries_target = new LineSeries() { Title = "Accuracy_target" };
                    var fmetricSeries_target = new LineSeries() { Title = "Fmetric_target" };
                    var MCCSeries_target = new LineSeries() { Title = "MCC_target" };

                    var precisionSeries_target_global = new LineSeries() { Title = String.Join(" ", Parameters) };
                    var recallSeries_target_global = new LineSeries() { Title = String.Join(" ", Parameters) };
                    var speceficitySeries_target_global = new LineSeries() { Title = String.Join(" ", Parameters) };
                    var falsepositivesSeries_target_global = new LineSeries() { Title = String.Join(" ", Parameters) };
                    var negativepredictedSeries_target_global = new LineSeries() { Title = String.Join(" ", Parameters) };
                    var accuracySeries_target_global = new LineSeries() { Title = String.Join(" ", Parameters) };
                    var fmetricSeries_target_global = new LineSeries() { Title = String.Join(" ", Parameters) };
                    var MCCSeries_target_global = new LineSeries() { Title = String.Join(" ", Parameters) };

                    foreach (var bucket in BucketList)
                    {
                        //add to full list
                        allBuckets.Add(bucket);

                        precisionSeries_target.Points.Add(new DataPoint(bucket.BucketLevel * BucketGranularity,
                            bucket.PrecisionTarget));
                        recallSeries_target.Points.Add(new DataPoint(bucket.BucketLevel * BucketGranularity,
                            bucket.RecallTarget));
                        speceficitySeries_target.Points.Add(new DataPoint(bucket.BucketLevel * BucketGranularity,
                            bucket.SpecificityTarget));
                        falsepositivesSeries_target.Points.Add(new DataPoint(bucket.BucketLevel * BucketGranularity,
                            bucket.FalsePositiveRateTarget));
                        negativepredictedSeries_target.Points.Add(new DataPoint(bucket.BucketLevel * BucketGranularity,
                            bucket.NegativePredictedValueTarget));
                        accuracySeries_target.Points.Add(new DataPoint(bucket.BucketLevel * BucketGranularity,
                            bucket.AccuracyTarget));
                        fmetricSeries_target.Points.Add(new DataPoint(bucket.BucketLevel * BucketGranularity,
                            bucket.FMeasureTarget));
                        MCCSeries_target.Points.Add(new DataPoint(bucket.BucketLevel * BucketGranularity, bucket.MCC_Target));

                        precisionSeries_target_global.Points.Add(new DataPoint(bucket.BucketLevel * BucketGranularity,
                            bucket.PrecisionTarget));
                        recallSeries_target_global.Points.Add(new DataPoint(bucket.BucketLevel * BucketGranularity,
                            bucket.RecallTarget));
                        speceficitySeries_target_global.Points.Add(new DataPoint(bucket.BucketLevel * BucketGranularity,
                            bucket.SpecificityTarget));
                        falsepositivesSeries_target_global.Points.Add(new DataPoint(bucket.BucketLevel * BucketGranularity,
                            bucket.FalsePositiveRateTarget));
                        negativepredictedSeries_target_global.Points.Add(
                            new DataPoint(bucket.BucketLevel * BucketGranularity, bucket.NegativePredictedValueTarget));
                        accuracySeries_target_global.Points.Add(new DataPoint(bucket.BucketLevel * BucketGranularity,
                            bucket.AccuracyTarget));
                        fmetricSeries_target_global.Points.Add(new DataPoint(bucket.BucketLevel * BucketGranularity,
                            bucket.FMeasureTarget));
                        MCCSeries_target_global.Points.Add(new DataPoint(bucket.BucketLevel * BucketGranularity,
                            bucket.MCC_Target));
                    }

                    model_target.Series.Add(precisionSeries_target);
                    model_target.Series.Add(recallSeries_target);
                    model_target.Series.Add(speceficitySeries_target);
                    model_target.Series.Add(falsepositivesSeries_target);
                    model_target.Series.Add(negativepredictedSeries_target);
                    model_target.Series.Add(accuracySeries_target);
                    model_target.Series.Add(fmetricSeries_target);
                    model_target.Series.Add(MCCSeries_target);
                    Task.Run(() =>
                    {
                        using (var filestream = new FileStream($"{file.FullName.Replace(".csv", "")}.plot_target.pdf",
                            FileMode.OpenOrCreate))
                        {
                            OxyPlot.PdfExporter.Export(model_target, filestream, PlotModelWidth, PlotModelHeight);
                            filestream.Close();
                        }
                    });
                    //add to global
                    model_glob_precision_target.Series.Add(precisionSeries_target_global);
                    model_glob_recall_target.Series.Add(recallSeries_target_global);
                    model_glob_speceficity_target.Series.Add(speceficitySeries_target_global);
                    model_glob_falsepositives_target.Series.Add(falsepositivesSeries_target_global);
                    model_glob_negativepredictions_target.Series.Add(negativepredictedSeries_target_global);
                    model_glob_accuracy_target.Series.Add(accuracySeries_target_global);
                    model_glob_fmetric_target.Series.Add(fmetricSeries_target_global);
                    model_glob_mcc_target.Series.Add(MCCSeries_target_global);
                    
                    //get valid/predicted sequences
                    validSequences.Add(String.Join(" ", Parameters), output.Count(t => t.IsValidPrediction));
                    predictedSequences.Add(String.Join(" ", Parameters), output.Count);
                }

                counter++;
                Console.WriteLine($"finished file {counter}");
            });

            //create aggregated file
            Dictionary<String, List<String>> sortedbucketsPrecision = new Dictionary<String, List<String>>();
            Dictionary<String, List<String>> sortedbucketsRecall = new Dictionary<String, List<String>>();
            Dictionary<String, List<String>> sortedbucketsSpeceficity = new Dictionary<String, List<String>>();
            Dictionary<String, List<String>> sortedbucketsFalsepositives = new Dictionary<String, List<String>>();
            Dictionary<String, List<String>> sortedbucketsNegativepredictions = new Dictionary<String, List<String>>();
            Dictionary<String, List<String>> sortedbucketsAccuracy = new Dictionary<String, List<String>>();
            Dictionary<String, List<String>> sortedbucketsFmetric = new Dictionary<String, List<String>>();
            Dictionary<String, List<String>> sortedbucketsMCC = new Dictionary<String, List<String>>();
            Dictionary<String, List<String>> sortedbucketsMSE = new Dictionary<String, List<String>>();
            Dictionary<String, List<String>> sortedbucketsRMSE = new Dictionary<String, List<String>>();
            Dictionary<String, List<String>> sortedbucketsMAE = new Dictionary<String, List<String>>();
            Dictionary<String, List<String>> sortedbucketsRSE = new Dictionary<String, List<String>>();
            Dictionary<String, List<String>> sortedbucketsRRSE = new Dictionary<String, List<String>>();
            Dictionary<String, List<String>> sortedbucketsRAE = new Dictionary<String, List<String>>();
            List<Dictionary<String, List<String>>> sortedbucketsList = new List<Dictionary<string, List<string>>>()
            {
                sortedbucketsPrecision, sortedbucketsRecall, sortedbucketsSpeceficity, sortedbucketsFalsepositives,
                sortedbucketsNegativepredictions, sortedbucketsAccuracy, sortedbucketsFmetric, sortedbucketsMCC,
                sortedbucketsMSE,sortedbucketsRMSE,sortedbucketsMAE,sortedbucketsRSE,sortedbucketsRRSE,sortedbucketsRAE
            };
            List<String> sortedbucketsListParameters = new List<string>()
            {
                "Precision","Recall","Speceficity","Falsepositives","Negativepredictions","Accuracy","Fmetric","MCC",
                "MSE","RMSE","MAE","RSE","RRSE","RAE"
            };

            foreach (var bucket in allBuckets)
            {
                if (!sortedbucketsPrecision.ContainsKey(String.Join(" ", bucket.Parameters)))
                    sortedbucketsPrecision.Add(String.Join(" ", bucket.Parameters), new List<String>());
                if (!sortedbucketsRecall.ContainsKey(String.Join(" ", bucket.Parameters)))
                    sortedbucketsRecall.Add(String.Join(" ", bucket.Parameters), new List<String>());
                if (!sortedbucketsSpeceficity.ContainsKey(String.Join(" ", bucket.Parameters)))
                    sortedbucketsSpeceficity.Add(String.Join(" ", bucket.Parameters), new List<String>());
                if (!sortedbucketsFalsepositives.ContainsKey(String.Join(" ", bucket.Parameters)))
                    sortedbucketsFalsepositives.Add(String.Join(" ", bucket.Parameters), new List<String>());
                if (!sortedbucketsNegativepredictions.ContainsKey(String.Join(" ", bucket.Parameters)))
                    sortedbucketsNegativepredictions.Add(String.Join(" ", bucket.Parameters), new List<String>());
                if (!sortedbucketsAccuracy.ContainsKey(String.Join(" ", bucket.Parameters)))
                    sortedbucketsAccuracy.Add(String.Join(" ", bucket.Parameters), new List<String>());
                if (!sortedbucketsFmetric.ContainsKey(String.Join(" ", bucket.Parameters)))
                    sortedbucketsFmetric.Add(String.Join(" ", bucket.Parameters), new List<String>());
                if (!sortedbucketsMCC.ContainsKey(String.Join(" ", bucket.Parameters)))
                    sortedbucketsMCC.Add(String.Join(" ", bucket.Parameters), new List<String>());
                if (!sortedbucketsMSE.ContainsKey(String.Join(" ", bucket.Parameters)))
                    sortedbucketsMSE.Add(String.Join(" ", bucket.Parameters), new List<String>());
                if (!sortedbucketsRMSE.ContainsKey(String.Join(" ", bucket.Parameters)))
                    sortedbucketsRMSE.Add(String.Join(" ", bucket.Parameters), new List<String>());
                if (!sortedbucketsMAE.ContainsKey(String.Join(" ", bucket.Parameters)))
                    sortedbucketsMAE.Add(String.Join(" ", bucket.Parameters), new List<String>());
                if (!sortedbucketsRSE.ContainsKey(String.Join(" ", bucket.Parameters)))
                    sortedbucketsRSE.Add(String.Join(" ", bucket.Parameters), new List<String>());
                if (!sortedbucketsRRSE.ContainsKey(String.Join(" ", bucket.Parameters)))
                    sortedbucketsRRSE.Add(String.Join(" ", bucket.Parameters), new List<String>());
                if (!sortedbucketsRAE.ContainsKey(String.Join(" ", bucket.Parameters)))
                    sortedbucketsRAE.Add(String.Join(" ", bucket.Parameters), new List<String>());
            }

            //generate values
            for(int i = 0; i * BucketGranularity < 1; i++)
            {
                var buckets = allBuckets.Where(t => t.BucketLevel == i);
                foreach(var bucket in buckets)
                {
                    sortedbucketsPrecision[String.Join(" ", bucket.Parameters)].Add(double.IsNaN(bucket.PrecisionTarget) ? "0" : bucket.PrecisionTarget.ToString());
                    sortedbucketsRecall[String.Join(" ", bucket.Parameters)].Add(double.IsNaN(bucket.RecallTarget) ? "0" : bucket.RecallTarget.ToString());
                    sortedbucketsSpeceficity[String.Join(" ", bucket.Parameters)].Add(double.IsNaN(bucket.SpecificityTarget) ? "0" : bucket.SpecificityTarget.ToString());
                    sortedbucketsFalsepositives[String.Join(" ", bucket.Parameters)].Add(double.IsNaN(bucket.FalsePositiveRateTarget) ? "0" : bucket.FalsePositiveRateTarget.ToString());
                    sortedbucketsNegativepredictions[String.Join(" ", bucket.Parameters)].Add(double.IsNaN(bucket.NegativePredictedValueTarget) ? "0" : bucket.NegativePredictedValueTarget.ToString());
                    sortedbucketsAccuracy[String.Join(" ", bucket.Parameters)].Add(double.IsNaN(bucket.AccuracyTarget) ? "0" : bucket.AccuracyTarget.ToString());
                    sortedbucketsFmetric[String.Join(" ", bucket.Parameters)].Add(double.IsNaN(bucket.FMeasureTarget) ? "0" : bucket.FMeasureTarget.ToString());
                    sortedbucketsMCC[String.Join(" ", bucket.Parameters)].Add(double.IsNaN(bucket.MCC_Target) ? "0" : bucket.MCC_Target.ToString());
                    sortedbucketsMSE[String.Join(" ", bucket.Parameters)].Add(double.IsNaN(bucket.MSE_Target) ? "0" : bucket.MSE_Target.ToString());
                    sortedbucketsRMSE[String.Join(" ", bucket.Parameters)].Add(double.IsNaN(bucket.RMSE_Target) ? "0" : bucket.RMSE_Target.ToString());
                    sortedbucketsMAE[String.Join(" ", bucket.Parameters)].Add(double.IsNaN(bucket.MAE_Target) ? "0" : bucket.MAE_Target.ToString());
                    sortedbucketsRSE[String.Join(" ", bucket.Parameters)].Add(double.IsNaN(bucket.RSE_Target) ? "0" : bucket.RSE_Target.ToString());
                    sortedbucketsRRSE[String.Join(" ", bucket.Parameters)].Add(double.IsNaN(bucket.RRSE_Target) ? "0" : bucket.RRSE_Target.ToString());
                    sortedbucketsRAE[String.Join(" ", bucket.Parameters)].Add(double.IsNaN(bucket.RAE_Target) ? "0" : bucket.RAE_Target.ToString());
                }
            }

            for(int i = 0; i < sortedbucketsList.Count; i++)
            {
                //print out values
                List<String> outLines = new List<string>();
                Dictionary<String, List<String>> dictOutlinesMCC = new Dictionary<string, List<string>>(); //R-format
                outLines.Add("p0,p1,p2,p3,p4,p5,0.0,0.1,0.2,0.3,0.4,0.5,0.6,0.7,0.8,0.9"); //header
                foreach (var result in sortedbucketsList[i])
                {
                    var line = String.Empty;
                    line += result.Key.Replace(' ', ',');
                    line += ',';
                    line += String.Join(",", result.Value);
                    outLines.Add(line.ToString());

                    //R format
                    if (!dictOutlinesMCC.ContainsKey(result.Key.Split(' ')[0]))
                        dictOutlinesMCC.Add(result.Key.Split(' ')[0], new List<string>());
                    result.Value.ForEach(t => dictOutlinesMCC[result.Key.Split(' ')[0]].Add(t));
                }
                //R
                var outLines2 = new List<String>();
                var headline = String.Empty;
                headline += "param";
                for (int j = 0; j < dictOutlinesMCC.Max(t => t.Value.Count); j += 10)
                    headline += ",0.0,0.1,0.2,0.3,0.4,0.5,0.6,0.7,0.8,0.9";
                outLines2.Add(headline);
                foreach (var dict in dictOutlinesMCC)
                    outLines2.Add($"{dict.Key},{String.Join(",", dict.Value)}");

                File.WriteAllLines($"{ResultsFolder.FullName}\\raw_{sortedbucketsListParameters[i]}.csv", outLines);
                File.WriteAllLines($"{ResultsFolder.FullName}\\raw_{sortedbucketsListParameters[i]}2.csv", outLines2);
            }

            #region print global
            using (var filestream = new FileStream($"{ResultsFolder.FullName}\\global_precision_target.pdf", FileMode.OpenOrCreate))
            {
                OxyPlot.PdfExporter.Export(model_glob_precision_target, filestream, PlotModelWidth, PlotModelHeight);
                filestream.Close();
            }
            using (var filestream = new FileStream($"{ResultsFolder.FullName}\\global_recall_target.pdf", FileMode.OpenOrCreate))
            {
                OxyPlot.PdfExporter.Export(model_glob_recall_target, filestream, PlotModelWidth, PlotModelHeight);
                filestream.Close();
            }
            using (var filestream = new FileStream($"{ResultsFolder.FullName}\\global_speceficity_target.pdf", FileMode.OpenOrCreate))
            {
                OxyPlot.PdfExporter.Export(model_glob_speceficity_target, filestream, PlotModelWidth, PlotModelHeight);
                filestream.Close();
            }
            using (var filestream = new FileStream($"{ResultsFolder.FullName}\\global_falsepositives_target.pdf", FileMode.OpenOrCreate))
            {
                OxyPlot.PdfExporter.Export(model_glob_falsepositives_target, filestream, PlotModelWidth, PlotModelHeight);
                filestream.Close();
            }
            using (var filestream = new FileStream($"{ResultsFolder.FullName}\\global_negativepredictions_target.pdf", FileMode.OpenOrCreate))
            {
                OxyPlot.PdfExporter.Export(model_glob_negativepredictions_target, filestream, PlotModelWidth, PlotModelHeight);
                filestream.Close();
            }
            using (var filestream = new FileStream($"{ResultsFolder.FullName}\\global_accuracy_target.pdf", FileMode.OpenOrCreate))
            {
                OxyPlot.PdfExporter.Export(model_glob_accuracy_target, filestream, PlotModelWidth, PlotModelHeight);
                filestream.Close();
            }
            using (var filestream = new FileStream($"{ResultsFolder.FullName}\\global_fmetric_target.pdf", FileMode.OpenOrCreate))
            {
                OxyPlot.PdfExporter.Export(model_glob_fmetric_target, filestream, PlotModelWidth, PlotModelHeight);
                filestream.Close();
            }
            using (var filestream = new FileStream($"{ResultsFolder.FullName}\\global_mcc_target.pdf", FileMode.OpenOrCreate))
            {
                OxyPlot.PdfExporter.Export(model_glob_mcc_target, filestream, PlotModelWidth, PlotModelHeight);
                filestream.Close();
            }            

            //prediction validity
            ColumnSeries validpredictionSeries = new ColumnSeries();
            model_glob_validsequences.Series.Add(validpredictionSeries);
            foreach (var entry in validSequences)
            {
                model_glob_validsequences_cataxis.ActualLabels.Add(entry.Key);
                validpredictionSeries.Items.Add(new ColumnItem() { Value = (double)entry.Value / (double)maxSequences });
            }
            using (var filestream = new FileStream($"{ResultsFolder.FullName}\\global_valid_sequences.pdf", FileMode.OpenOrCreate))
            {
                OxyPlot.PdfExporter.Export(model_glob_validsequences, filestream, PlotModelWidth * 6, PlotModelHeight);
                filestream.Close();
            }

            ColumnSeries predictionsSeries = new ColumnSeries();
            model_glob_predictedsequences.Series.Add(predictionsSeries);
            foreach (var entry in predictedSequences)
            {
                model_glob_predictedsequences_cataxis.ActualLabels.Add(entry.Key);
                predictionsSeries.Items.Add(new ColumnItem() { Value = (double)entry.Value / (double)maxSequences });
            }
            using (var filestream = new FileStream($"{ResultsFolder.FullName}\\global_predicted_sequences.pdf", FileMode.OpenOrCreate))
            {
                OxyPlot.PdfExporter.Export(model_glob_predictedsequences, filestream, PlotModelWidth * 6, PlotModelHeight);
                filestream.Close();
            }
            #endregion

            #region boxplots
            var boxplotSeries_target_neurons = new BoxPlotSeries() { };
            model_glob_boxplot_neurons_target.Series.Add(boxplotSeries_target_neurons);
            var parameters = allParameters[0].Distinct().ToList();
            parameters.Sort();
            for (int i = 0; i < parameters.Count; i++)
            {
                model_glob_boxplot_neurons_target_cataxis.ActualLabels.Add($"Precision {parameters[i]}");
                boxplotSeries_target_neurons.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[0] == parameters[i].ToString()).Select(t => t.PrecisionTarget).ToList(), i));
            }
            for (int i = 0; i < parameters.Count; i++)
            {
                model_glob_boxplot_neurons_target_cataxis.ActualLabels.Add($"Recall {parameters[i]}");
                boxplotSeries_target_neurons.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[0] == parameters[i].ToString()).Select(t => t.RecallTarget).ToList(), i + (parameters.Count * 1)));
            }
            for (int i = 0; i < parameters.Count; i++)
            {
                model_glob_boxplot_neurons_target_cataxis.ActualLabels.Add($"Speceficity {parameters[i]}");
                boxplotSeries_target_neurons.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[0] == parameters[i].ToString()).Select(t => t.SpecificityTarget).ToList(), i + (parameters.Count * 2)));
            }
            for (int i = 0; i < parameters.Count; i++)
            {
                model_glob_boxplot_neurons_target_cataxis.ActualLabels.Add($"FalsePositive {parameters[i]}");
                boxplotSeries_target_neurons.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[0] == parameters[i].ToString()).Select(t => t.FalsePositiveRateTarget).ToList(), i + (parameters.Count * 3)));
            }
            for (int i = 0; i < parameters.Count; i++)
            {
                model_glob_boxplot_neurons_target_cataxis.ActualLabels.Add($"NegativePredicted {parameters[i]}");
                boxplotSeries_target_neurons.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[0] == parameters[i].ToString()).Select(t => t.NegativePredictedValueTarget).ToList(), i + (parameters.Count * 4)));
            }
            for (int i = 0; i < parameters.Count; i++)
            {
                model_glob_boxplot_neurons_target_cataxis.ActualLabels.Add($"Accuracy {parameters[i]}");
                boxplotSeries_target_neurons.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[0] == parameters[i].ToString()).Select(t => t.AccuracyTarget).ToList(), i + (parameters.Count * 5)));
            }
            for (int i = 0; i < parameters.Count; i++)
            {
                model_glob_boxplot_neurons_target_cataxis.ActualLabels.Add($"Fmetric {parameters[i]}");
                boxplotSeries_target_neurons.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[0] == parameters[i].ToString()).Select(t => t.FMeasureTarget).ToList(), i + (parameters.Count * 6)));
            }
            for (int i = 0; i < parameters.Count; i++)
            {
                model_glob_boxplot_neurons_target_cataxis.ActualLabels.Add($"MCC {parameters[i]}");
                boxplotSeries_target_neurons.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[0] == parameters[i].ToString()).Select(t => t.MCC_Target).ToList(), i + (parameters.Count * 7)));
            }
            using (var filestream = new FileStream($"{ResultsFolder.FullName}\\global_boxplot_neurons_target.pdf", FileMode.OpenOrCreate))
            {
                OxyPlot.PdfExporter.Export(model_glob_boxplot_neurons_target, filestream, PlotModelWidth * 3, PlotModelHeight);
                filestream.Close();
            }

            var boxplotSeries_target_dropout = new BoxPlotSeries() { };
            model_glob_boxplot_dropout_target.Series.Add(boxplotSeries_target_dropout);
            parameters = allParameters[1].Distinct().ToList();
            parameters.Sort();
            for (int i = 0; i < parameters.Count; i++)
            {
                model_glob_boxplot_dropout_target_cataxis.ActualLabels.Add($"Precision {parameters[i]}");
                boxplotSeries_target_dropout.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[1] == parameters[i].ToString()).Select(t => t.PrecisionTarget).ToList(), i));
            }
            for (int i = 0; i < parameters.Count; i++)
            {
                model_glob_boxplot_dropout_target_cataxis.ActualLabels.Add($"Recall {parameters[i]}");
                boxplotSeries_target_dropout.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[1] == parameters[i].ToString()).Select(t => t.RecallTarget).ToList(), i + (parameters.Count * 1)));
            }
            for (int i = 0; i < parameters.Count; i++)
            {
                model_glob_boxplot_dropout_target_cataxis.ActualLabels.Add($"Speceficity {parameters[i]}");
                boxplotSeries_target_dropout.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[1] == parameters[i].ToString()).Select(t => t.SpecificityTarget).ToList(), i + (parameters.Count * 2)));
            }
            for (int i = 0; i < parameters.Count; i++)
            {
                model_glob_boxplot_dropout_target_cataxis.ActualLabels.Add($"FalsePositive {parameters[i]}");
                boxplotSeries_target_dropout.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[1] == parameters[i].ToString()).Select(t => t.FalsePositiveRateTarget).ToList(), i + (parameters.Count * 3)));
            }
            for (int i = 0; i < parameters.Count; i++)
            {
                model_glob_boxplot_dropout_target_cataxis.ActualLabels.Add($"NegativePredicted {parameters[i]}");
                boxplotSeries_target_dropout.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[1] == parameters[i].ToString()).Select(t => t.NegativePredictedValueTarget).ToList(), i + (parameters.Count * 4)));
            }
            for (int i = 0; i < parameters.Count; i++)
            {
                model_glob_boxplot_dropout_target_cataxis.ActualLabels.Add($"Accuracy {parameters[i]}");
                boxplotSeries_target_dropout.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[1] == parameters[i].ToString()).Select(t => t.AccuracyTarget).ToList(), i + (parameters.Count * 5)));
            }
            for (int i = 0; i < parameters.Count; i++)
            {
                model_glob_boxplot_dropout_target_cataxis.ActualLabels.Add($"Fmetric {parameters[i]}");
                boxplotSeries_target_dropout.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[1] == parameters[i].ToString()).Select(t => t.FMeasureTarget).ToList(), i + (parameters.Count * 6)));
            }
            for (int i = 0; i < parameters.Count; i++)
            {
                model_glob_boxplot_dropout_target_cataxis.ActualLabels.Add($"MCC {parameters[i]}");
                boxplotSeries_target_dropout.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[1] == parameters[i].ToString()).Select(t => t.MCC_Target).ToList(), i + (parameters.Count * 7)));
            }
            using (var filestream = new FileStream($"{ResultsFolder.FullName}\\global_boxplot_dropout_target.pdf", FileMode.OpenOrCreate))
            {
                OxyPlot.PdfExporter.Export(model_glob_boxplot_dropout_target, filestream, PlotModelWidth * 3, PlotModelHeight);
                filestream.Close();
            }

            var boxplotSeries_target_patience = new BoxPlotSeries() { };
            model_glob_boxplot_patience_target.Series.Add(boxplotSeries_target_patience);
            parameters = allParameters[2].Distinct().ToList();
            parameters.Sort();
            for (int i = 0; i < parameters.Count; i++)
            {
                model_glob_boxplot_patience_target_cataxis.ActualLabels.Add($"Precision {parameters[i]}");
                boxplotSeries_target_patience.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[2] == parameters[i].ToString()).Select(t => t.PrecisionTarget).ToList(), i));
            }
            for (int i = 0; i < parameters.Count; i++)
            {
                model_glob_boxplot_patience_target_cataxis.ActualLabels.Add($"Recall {parameters[i]}");
                boxplotSeries_target_patience.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[2] == parameters[i].ToString()).Select(t => t.RecallTarget).ToList(), i + (parameters.Count * 1)));
            }
            for (int i = 0; i < parameters.Count; i++)
            {
                model_glob_boxplot_patience_target_cataxis.ActualLabels.Add($"Speceficity {parameters[i]}");
                boxplotSeries_target_patience.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[2] == parameters[i].ToString()).Select(t => t.SpecificityTarget).ToList(), i + (parameters.Count * 2)));
            }
            for (int i = 0; i < parameters.Count; i++)
            {
                model_glob_boxplot_patience_target_cataxis.ActualLabels.Add($"FalsePositive {parameters[i]}");
                boxplotSeries_target_patience.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[2] == parameters[i].ToString()).Select(t => t.FalsePositiveRateTarget).ToList(), i + (parameters.Count * 3)));
            }
            for (int i = 0; i < parameters.Count; i++)
            {
                model_glob_boxplot_patience_target_cataxis.ActualLabels.Add($"NegativePredicted {parameters[i]}");
                boxplotSeries_target_patience.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[2] == parameters[i].ToString()).Select(t => t.NegativePredictedValueTarget).ToList(), i + (parameters.Count * 4)));
            }
            for (int i = 0; i < parameters.Count; i++)
            {
                model_glob_boxplot_patience_target_cataxis.ActualLabels.Add($"Accuracy {parameters[i]}");
                boxplotSeries_target_patience.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[2] == parameters[i].ToString()).Select(t => t.AccuracyTarget).ToList(), i + (parameters.Count * 5)));
            }
            for (int i = 0; i < parameters.Count; i++)
            {
                model_glob_boxplot_patience_target_cataxis.ActualLabels.Add($"Fmetric {parameters[i]}");
                boxplotSeries_target_patience.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[2] == parameters[i].ToString()).Select(t => t.FMeasureTarget).ToList(), i + (parameters.Count * 6)));
            }
            for (int i = 0; i < parameters.Count; i++)
            {
                model_glob_boxplot_patience_target_cataxis.ActualLabels.Add($"MCC {parameters[i]}");
                boxplotSeries_target_patience.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[2] == parameters[i].ToString()).Select(t => t.MCC_Target).ToList(), i + (parameters.Count * 7)));
            }
            using (var filestream = new FileStream($"{ResultsFolder.FullName}\\global_boxplot_patience_target.pdf", FileMode.OpenOrCreate))
            {
                OxyPlot.PdfExporter.Export(model_glob_boxplot_patience_target, filestream, PlotModelWidth * 3, PlotModelHeight);
                filestream.Close();
            }

            var boxplotSeries_target_algorithm = new BoxPlotSeries() { };
            model_glob_boxplot_algorithm_target.Series.Add(boxplotSeries_target_algorithm);
            parameters = allParameters[3].Distinct().ToList();
            parameters.Sort();
            for (int i = 0; i < parameters.Count; i++)
            {
                model_glob_boxplot_algorithm_target_cataxis.ActualLabels.Add($"Precision {parameters[i]}");
                boxplotSeries_target_algorithm.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[3] == parameters[i].ToString()).Select(t => t.PrecisionTarget).ToList(), i));
            }
            for (int i = 0; i < parameters.Count; i++)
            {
                model_glob_boxplot_algorithm_target_cataxis.ActualLabels.Add($"Recall {parameters[i]}");
                boxplotSeries_target_algorithm.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[3] == parameters[i].ToString()).Select(t => t.RecallTarget).ToList(), i + (parameters.Count * 1)));
            }
            for (int i = 0; i < parameters.Count; i++)
            {
                model_glob_boxplot_algorithm_target_cataxis.ActualLabels.Add($"Speceficity {parameters[i]}");
                boxplotSeries_target_algorithm.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[3] == parameters[i].ToString()).Select(t => t.SpecificityTarget).ToList(), i + (parameters.Count * 2)));
            }
            for (int i = 0; i < parameters.Count; i++)
            {
                model_glob_boxplot_algorithm_target_cataxis.ActualLabels.Add($"FalsePositive {parameters[i]}");
                boxplotSeries_target_algorithm.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[3] == parameters[i].ToString()).Select(t => t.FalsePositiveRateTarget).ToList(), i + (parameters.Count * 3)));
            }
            for (int i = 0; i < parameters.Count; i++)
            {
                model_glob_boxplot_algorithm_target_cataxis.ActualLabels.Add($"NegativePredicted {parameters[i]}");
                boxplotSeries_target_algorithm.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[3] == parameters[i].ToString()).Select(t => t.NegativePredictedValueTarget).ToList(), i + (parameters.Count * 4)));
            }
            for (int i = 0; i < parameters.Count; i++)
            {
                model_glob_boxplot_algorithm_target_cataxis.ActualLabels.Add($"Accuracy {parameters[i]}");
                boxplotSeries_target_algorithm.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[3] == parameters[i].ToString()).Select(t => t.AccuracyTarget).ToList(), i + (parameters.Count * 5)));
            }
            for (int i = 0; i < parameters.Count; i++)
            {
                model_glob_boxplot_algorithm_target_cataxis.ActualLabels.Add($"Fmetric {parameters[i]}");
                boxplotSeries_target_algorithm.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[3] == parameters[i].ToString()).Select(t => t.FMeasureTarget).ToList(), i + (parameters.Count * 6)));
            }
            for (int i = 0; i < parameters.Count; i++)
            {
                model_glob_boxplot_algorithm_target_cataxis.ActualLabels.Add($"MCC {parameters[i]}");
                boxplotSeries_target_algorithm.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[3] == parameters[i].ToString()).Select(t => t.MCC_Target).ToList(), i + (parameters.Count * 7)));
            }
            using (var filestream = new FileStream($"{ResultsFolder.FullName}\\global_boxplot_algorithm_target.pdf", FileMode.OpenOrCreate))
            {
                OxyPlot.PdfExporter.Export(model_glob_boxplot_algorithm_target, filestream, PlotModelWidth * 3, PlotModelHeight);
                filestream.Close();
            }
            #endregion

            #region groupings
            //get and sort parameters
            for (int group = 0; group < allParameters.Length; group++)
            {
                //create grouping model
                OxyPlot.PlotModel model_groupings0 = new PlotModel() { Title = $"Results: Avg MCC grouped by parameter {group}" };
                model_groupings0.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0, Maximum = 0.95, Title = "Process completion" });
                model_groupings0.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1, Title = "Value" });
                model_groupings0.IsLegendVisible = true;

                parameters = allParameters[group].Distinct().ToList();
                parameters.Sort();

                //set up output lines
                var grouping0out = new List<String>();
                List<String> grouping0header = new List<String>() { "groupingname" };
                for (int j = 0; j * BucketGranularity < 1; j++)
                    grouping0header.Add((j * BucketGranularity).ToString());
                grouping0out.Add(String.Join(",", grouping0header));

                //iterate through nth level grouping
                for (int i = 0; i < parameters.Count; i++)
                {
                    var grouping0line = new List<String>();
                    grouping0line.Add(parameters[i]);
                    var groupingSeries = new LineSeries() { Title = parameters[i] };

                    //create boxplot model for each nth level grouping
                    OxyPlot.PlotModel groupingboxplotmodel = new PlotModel() { Title = $"Grouping Boxplots for {parameters[i]}" };
                    groupingboxplotmodel.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0, Maximum = 0.95, Title = "Process completion" });
                    groupingboxplotmodel.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1, Title = "Value (MCC)" });
                    var groupingBoxPlotSeries = new BoxPlotSeries() { BoxWidth = 0.025 };
                    groupingboxplotmodel.Series.Add(groupingBoxPlotSeries);

                    //iterate through each bucket
                    for (int j = 0; j * BucketGranularity < 1; j++)
                    {
                        var tbuckets = allBuckets.Where(t => t.BucketLevel == j && t.Parameters[group] == parameters[i].ToString());
                        var val = 0d;
                        if (tbuckets.Where(t => !double.IsNaN(t.MCC_Target)).Any())
                        {
                            val = tbuckets.Where(t => !double.IsNaN(t.MCC_Target)).Average(t => t.MCC_Target);
                            groupingBoxPlotSeries.Items.Add(CreateBoxplot(tbuckets.Where(t => !double.IsNaN(t.MCC_Target)).Select(t => t.MCC_Target).ToList(), j * BucketGranularity));
                        }
                        groupingSeries.Points.Add(new DataPoint(j * BucketGranularity, val));
                        grouping0line.Add(val.ToString());

                    }
                    model_groupings0.Series.Add(groupingSeries);
                    grouping0out.Add(String.Join(",", grouping0line));
                    using (var filestream = new FileStream($"{ResultsFolder.FullName}\\grouping{group}_boxplot_{parameters[i]}.pdf", FileMode.OpenOrCreate))
                    {
                        OxyPlot.PdfExporter.Export(groupingboxplotmodel, filestream, PlotModelWidth, PlotModelHeight);
                        filestream.Close();
                    }

                }
                using (var filestream = new FileStream($"{ResultsFolder.FullName}\\grouping{group}.pdf", FileMode.OpenOrCreate))
                {
                    OxyPlot.PdfExporter.Export(model_groupings0, filestream, PlotModelWidth, PlotModelHeight);
                    filestream.Close();
                }
                File.WriteAllLines($"{ResultsFolder.FullName}\\grouping{group}.csv", grouping0out);

            }
            #endregion

            #region statistics
            //(anova + wilcox paired)
            List<String> anovaBlockOutlines = new List<String>();
            List<String> wilcoxBlockOutlines = new List<String>();
            List<String> ttestBlockOutlines = new List<String>();
            List<String> anovaRowOutlines = new List<String>();
            List<String> wilcoxRowOutlines = new List<String>();
            List<String> ttestRowOutlines = new List<String>();
            parameters = allParameters[0].Distinct().ToList();
            parameters.Sort();

            List<Task<Tuple<List<String>, List<String>, List<String>>>> taskList = new List<Task<Tuple<List<String>, List<String>, List<String>>>>();
            for (int i = 0; i * BucketGranularity < 1; i++)
            {
                var buckets = allBuckets.Where(t => t.BucketLevel == i).ToList();
                var level = ((double)i * BucketGranularity).ToString();
                taskList.Add(Task.Run(() => FillStatisticData(level, parameters, buckets)));
            }

            //all, <50%, >=50%
            taskList.Add(Task.Run(() => FillStatisticData("all",parameters, allBuckets)));
            taskList.Add(Task.Run(() => FillStatisticData("<50%",parameters, allBuckets.Where(t => t.BucketLevel * BucketGranularity < 0.5d).ToList())));
            taskList.Add(Task.Run(() => FillStatisticData(">=50%",parameters, allBuckets.Where(t => t.BucketLevel * BucketGranularity >= 0.5d).ToList())));

            //x vs y blocks
            for(int i = 0; i < taskList.Count; i++)
            {
                taskList[i].Wait();
                var result = taskList[i].Result;
                anovaBlockOutlines.AddRange(result.Item1);
                wilcoxBlockOutlines.AddRange(result.Item2);
                ttestBlockOutlines.AddRange(result.Item3);
            }

            //rows
            foreach(var parameter in new List<String>() { "s2s", "nopath", "noplanned", "rgb" })//test parameter vs opposite
            {
                //header 
                var line = $"{parameter},null";
                for (int j = 0; j * BucketGranularity < 1; j++)
                    line += $",{(j * BucketGranularity).ToString()}";
                anovaRowOutlines.Add(line);
                wilcoxRowOutlines.Add(line);
                ttestRowOutlines.Add(line);

                //get unique p0s
                var p01 = parameters.Where(t => t.Contains(parameter)).Distinct();
                var p02 = parameters.Where(t => !t.Contains(parameter)).Distinct();

                //iterate through p0s (generating combinations for parameter vs !parameter)
                foreach (var p01param in p01)
                {
                    foreach (var p02param in p02)
                    {
                        var anovaoutline = new List<String>(); anovaoutline.Add(p01param); anovaoutline.Add(p02param);
                        var wilcoxoutline = new List<String>(); wilcoxoutline.Add(p01param); wilcoxoutline.Add(p02param);
                        var ttestoutline = new List<String>(); ttestoutline.Add(p01param); ttestoutline.Add(p02param);

                        //go through each bucket
                        for (int i = 0; i * BucketGranularity < 1; i++)
                        {
                            //get data
                            var data1 = allBuckets.Where(t => t.Parameters.First() == p01param && t.BucketLevel == i);
                            var data2 = allBuckets.Where(t => t.Parameters.First() == p02param && t.BucketLevel == i);

                            //write data
                            //get data values
                            List<double> res1 = data1.Where(t => !double.IsNaN(t.MCC_Target)).Select(t => t.MCC_Target).ToList();
                            List<double> res2 = data2.Where(t => !double.IsNaN(t.MCC_Target)).Select(t => t.MCC_Target).ToList();

                            //get p-value
                            anovaoutline.Add(Statistics.CalculateP(res1, res2).ToString());
                            wilcoxoutline.Add(new Accord.Statistics.Testing.MannWhitneyWilcoxonTest(res1.ToArray(), res2.ToArray(), exact: false).PValue.ToString());
                            ttestoutline.Add(new Accord.Statistics.Testing.TwoSampleTTest(res1.ToArray(), res2.ToArray(), false).PValue.ToString());                            
                        }
                        //edge cases //all
                        var dataa1 = allBuckets.Where(t => t.Parameters.First() == p01param).Where(t => !double.IsNaN(t.MCC_Target)).Select(t => t.MCC_Target).ToList();
                        var dataa2 = allBuckets.Where(t => t.Parameters.First() == p02param).Where(t => !double.IsNaN(t.MCC_Target)).Select(t => t.MCC_Target).ToList();
                        anovaoutline.Add(Statistics.CalculateP(dataa1, dataa2).ToString());
                        wilcoxoutline.Add(new Accord.Statistics.Testing.MannWhitneyWilcoxonTest(dataa1.ToArray(), dataa2.ToArray(), exact: false).PValue.ToString());
                        ttestoutline.Add(new Accord.Statistics.Testing.TwoSampleTTest(dataa1.ToArray(), dataa2.ToArray(), false).PValue.ToString());

                        anovaRowOutlines.Add(String.Join(",",anovaoutline));
                        wilcoxRowOutlines.Add(String.Join(",", wilcoxoutline));
                        ttestRowOutlines.Add(String.Join(",", ttestoutline));
                    }
                }
            }

            File.WriteAllLines($"{ResultsFolder.FullName}\\pvalues_anova_block.csv", anovaBlockOutlines);
            File.WriteAllLines($"{ResultsFolder.FullName}\\pvalues_wilcox_block.csv", wilcoxBlockOutlines);
            File.WriteAllLines($"{ResultsFolder.FullName}\\pvalues_ttest_block.csv", ttestBlockOutlines);
            File.WriteAllLines($"{ResultsFolder.FullName}\\pvalues_anova_rows.csv", anovaRowOutlines);
            File.WriteAllLines($"{ResultsFolder.FullName}\\pvalues_wilcox_rows.csv", wilcoxRowOutlines);
            File.WriteAllLines($"{ResultsFolder.FullName}\\pvalues_ttest_rows.csv", ttestRowOutlines);
            #endregion statistics
        }

        public static double CalculateAccuracy(double pInput, double pReference)
        {
            return pInput > pReference
                ? (pInput - (Math.Abs(pInput - pReference) * 2)) / pReference
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
            return pParameterString.Split(' ').ToList();
        }

        class Line
        {
            public bool IsBinaryPrediction { get; set; }
            public bool IsRGBEncoding { get; set; }

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
            public String SuffixActivities { get; set; }
            public bool Predicted_Violations { get; set; }

            //output
            public double AccuracySumprevious { get; set; }
            public double AccuracyTimestamp { get; set; }
            public double DeviationAbsoluteSumprevious => SumPrevious - GT_SumPrevious;
            public double DeviationAbsoluteTimestamp => Timestamp - GT_Timestamp;
            public bool Violation_Effective => GT_Timestamp > GT_Planned;
            public bool Violation_PredictedTS => Timestamp > GT_Planned;
            public bool Violation_PredictedSP => SumPrevious > GT_Planned;
            public String Violation_StringTS => CalculateViolationString(Violation_Effective, Violation_PredictedTS);
            public String Violation_StringSP => CalculateViolationString(Violation_Effective, IsBinaryPrediction ? Predicted_Violations : Violation_PredictedSP);
            public bool IsValidPrediction => IsValidSequence(PredictedActivities, PrefixActivities, IsRGBEncoding);

        }

        class Bucket
        {
            public int BucketLevel { get; set; }
            public List<String> Parameters { get; set; }
            public TargetData Target { get; set; }

            //counts
            public int TPcountSP => ViolationStringsSP.Count(t => t == "TP");
            public int FPcountSP => ViolationStringsSP.Count(t => t == "FP");
            public int TNcountSP => ViolationStringsSP.Count(t => t == "TN");
            public int FNcountSP => ViolationStringsSP.Count(t => t == "FN");
            public int TPcountTS => ViolationStringsTS.Count(t => t == "TP");
            public int FPcountTS => ViolationStringsTS.Count(t => t == "FP");
            public int TNcountTS => ViolationStringsTS.Count(t => t == "TN");
            public int FNcountTS => ViolationStringsTS.Count(t => t == "FN");
            public int TPcountTarget => ViolationStringsTarget.Count(t => t == "TP");
            public int FPcountTarget => ViolationStringsTarget.Count(t => t == "FP");
            public int TNcountTarget => ViolationStringsTarget.Count(t => t == "TN");
            public int FNcountTarget => ViolationStringsTarget.Count(t => t == "FN");


            public List<Double> Prediction_SP { get; set; }
            public List<Double> Prediction_TS { get; set; }
            public List<String> ViolationStringsTS { get; set; }
            public List<String> ViolationStringsSP { get; set; }
            public List<String> ViolationStringsTarget { get; set; }
            public List<Double> PredictionAccuraciesSP { get; set; }
            public List<Double> PredictionAccuraciesTS { get; set; }
            public List<Double> DeviationsAbsoluteSP { get; set; }
            public List<Double> DeviationsAbsoluteTS { get; set; }
            //binary prediction
            public double PrecisionSP => (double)ViolationStringsSP.Count(t => t == "TP") / (double)ViolationStringsSP.Count(t => t == "TP" || t == "FP");
            public double RecallSP => (double)ViolationStringsSP.Count(t => t == "TP") / (double)ViolationStringsSP.Count(t => t == "TP" || t == "FN");
            public double SpecificitySP => (double)ViolationStringsSP.Count(t => t == "TN") / (double)ViolationStringsSP.Count(t => t == "TN" || t == "FP");
            public double FalsePositiveRateSP => (double)ViolationStringsSP.Count(t => t == "FP") / (double)ViolationStringsSP.Count(t => t == "FP" || t == "TN");
            public double NegativePredictedValueSP => (double)ViolationStringsSP.Count(t => t == "TN") / (double)ViolationStringsSP.Count(t => t == "TN" || t == "TP");
            public double AccuracySP => (double)ViolationStringsSP.Count(t => t == "TN" || t == "TP") / (double)ViolationStringsSP.Count;
            public double FMeasureSP => ((1 + Math.Pow(FmetricBeta, 2)) * PrecisionSP * RecallSP) / ((Math.Pow(FmetricBeta, 2) * PrecisionSP) + RecallSP);
            public double MCC_SP => (double)((TPcountSP * TNcountSP) - (FPcountSP * FNcountSP)) / Math.Sqrt((double)(TPcountSP + FPcountSP) * (TPcountSP + FNcountSP) * (TNcountSP + FPcountSP) * (TNcountSP + FNcountSP));

            public double PrecisionTS => (double)ViolationStringsTS.Count(t => t == "TP") / (double)ViolationStringsTS.Count(t => t == "TP" || t == "FP");
            public double RecallTS => (double)ViolationStringsTS.Count(t => t == "TP") / (double)ViolationStringsTS.Count(t => t == "TP" || t == "FN");
            public double SpecificityTS => (double)ViolationStringsTS.Count(t => t == "TN") / (double)ViolationStringsTS.Count(t => t == "TN" || t == "FP");
            public double FalsePositiveRateTS => (double)ViolationStringsTS.Count(t => t == "FP") / (double)ViolationStringsTS.Count(t => t == "FP" || t == "TN");
            public double NegativePredictedValueTS => (double)ViolationStringsTS.Count(t => t == "TN") / (double)ViolationStringsTS.Count(t => t == "TN" || t == "TP");
            public double AccuracyTS => (double)ViolationStringsTS.Count(t => t == "TN" || t == "TP") / (double)ViolationStringsTS.Count;
            public double FMeasureTS => ((1 + Math.Pow(FmetricBeta, 2)) * PrecisionTS * RecallTS) / ((Math.Pow(FmetricBeta, 2) * PrecisionTS) + RecallTS);
            public double MCC_TS => (double)((TPcountTS * TNcountTS) - (FPcountTS * FNcountTS)) / Math.Sqrt((double)(TPcountTS + FPcountTS) * (TPcountTS + FNcountTS) * (TNcountTS + FPcountTS) * (TNcountTS + FNcountTS));

            //target metric
            public double PrecisionTarget => (double)ViolationStringsTarget.Count(t => t == "TP") / (double)ViolationStringsTarget.Count(t => t == "TP" || t == "FP");
            public double RecallTarget => (double)ViolationStringsTarget.Count(t => t == "TP") / (double)ViolationStringsTarget.Count(t => t == "TP" || t == "FN");
            public double SpecificityTarget => (double)ViolationStringsTarget.Count(t => t == "TN") / (double)ViolationStringsTarget.Count(t => t == "TN" || t == "FP");
            public double FalsePositiveRateTarget => (double)ViolationStringsTarget.Count(t => t == "FP") / (double)ViolationStringsTarget.Count(t => t == "FP" || t == "TN");
            public double NegativePredictedValueTarget => (double)ViolationStringsTarget.Count(t => t == "TN") / (double)ViolationStringsTarget.Count(t => t == "TN" || t == "TP");
            public double AccuracyTarget => (double)ViolationStringsTarget.Count(t => t == "TN" || t == "TP") / (double)ViolationStringsTarget.Count;
            public double FMeasureTarget => ((1 + Math.Pow(FmetricBeta, 2)) * PrecisionTarget * RecallTarget) / ((Math.Pow(FmetricBeta, 2) * PrecisionTarget) + RecallTarget);
            public double MCC_Target => (double)((TPcountTarget * TNcountTarget) - (FPcountTarget * FNcountTarget)) / Math.Sqrt((double)(TPcountTarget + FPcountTarget) * (TPcountTarget + FNcountTarget) * (TNcountTarget + FPcountTarget) * (TNcountTarget + FNcountTarget));

            //regression prediction
            public double PredictionMedianSP => Median(PredictionAccuraciesSP.ToArray());
            public double PredictionMedianTS => Median(PredictionAccuraciesTS.ToArray());

            //numeric metrics
            public double MSE_SP => DeviationsAbsoluteSP.Sum(t => Math.Pow(t, 2)) / DeviationsAbsoluteSP.Count;
            public double RMSE_SP => Math.Sqrt(DeviationsAbsoluteSP.Sum(t => Math.Pow(t, 2)) / DeviationsAbsoluteSP.Count);
            public double MAE_SP => DeviationsAbsoluteSP.Sum(t => Math.Abs(t)) / DeviationsAbsoluteSP.Count;
            public double RSE_SP => DeviationsAbsoluteSP.Sum(t => Math.Pow(t, 2)) / (Prediction_SP.Sum(t => Math.Pow(t - Prediction_SP.Average(), 2)));
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

            //numeric target metrics
            public double MSE_Target => Target == TargetData.SP ? MSE_SP : MSE_TS;
            public double RMSE_Target => Target == TargetData.SP ? RMSE_SP : RMSE_TS;
            public double MAE_Target => Target == TargetData.SP ? MAE_SP : MAE_TS;
            public double RSE_Target => Target == TargetData.SP ? RSE_SP : RSE_TS;
            public double RRSE_Target => Target == TargetData.SP ? RRSE_SP : RRSE_TS;
            public double RAE_Target => Target == TargetData.SP ? RAE_SP : RAE_TS;

        }

        static double Median(double[] xs)
        {
            if (xs.Length == 0)
                return 0;

            //https://stackoverflow.com/questions/4140719/calculate-median-in-c-sharp
            var ys = xs.OrderBy(x => x).ToList();
            double mid = (ys.Count - 1) / 2.0;
            return (ys[(int)(mid)] + ys[(int)(mid + 0.5)]) / 2;
        }

        static bool IsValidSequence(String pPredictionSequence, String pPrefixSequence, bool pIsRGBencoding)
        {
            //in case of rgb encoding, automatically assume valid sequence
            if (pIsRGBencoding)
                return true;

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
                        }
                        else if (!valid.Any())
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
                    return new List<int>() { 2, 4 };
                case 4:
                    return new List<int>() { };
                case 5:
                    return new List<int>() { 6 };
                case 6:
                    return new List<int>() { 7 };
                case 7:
                    return new List<int>() { 6, 8 };
                case 8:
                    return new List<int>() { };
                case 9:
                    return new List<int>() { 10 };
                case 10:
                    return new List<int>() { 11 };
                case 11:
                    return new List<int>() { 10, 12 };
                case 12:
                    return new List<int>() { };
                case 13:
                    return new List<int>() { 14 };
                case 14:
                    return new List<int>() { 15 };
                case 15:
                    return new List<int>() { 14, 16 };
                case 16:
                    return new List<int>() { };
                default:
                    throw new Exception("wront sequence token");
            }
        }

        static BoxPlotItem CreateBoxplot(List<double> pValues, double pX)
        {
            if (!pValues.Any() || pValues.Count(t => !double.IsNaN(t)) == 0)
                return null;

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

        static Tuple<List<String>, List<String>, List<String>> FillStatisticData(String level, List<String> parameters,List<Bucket> buckets)
        {
            List<String> anovaOutlines = new List<string>();
            List<String> wilcoxOutlines = new List<string>();
            List<String> ttestOutlines = new List<string>();

            anovaOutlines.Add(level + "," + String.Join(",", parameters)); //header
            wilcoxOutlines.Add(level + "," + String.Join(",", parameters));
            ttestOutlines.Add(level + "," + String.Join(",", parameters));
            foreach (var parameter1 in parameters) //rows
            {
                //get all combinations of parameter vs parameter and belonging buckets
                var parameterbuckets1 = buckets.Where(t => t.Parameters[0] == parameter1);

                List<double> anovapvalues = new List<double>();
                List<double> wilcoxpvalues = new List<double>();
                List<double> ttestpvalues = new List<double>();
                foreach (var parameter2 in parameters) //columns
                {
                    var parameterbuckets2 = buckets.Where(t => t.Parameters[0] == parameter2);

                    //get data values
                    List<double> data1 = parameterbuckets1.Where(t => !double.IsNaN(t.MCC_Target)).Select(t => t.MCC_Target).ToList();
                    List<double> data2 = parameterbuckets2.Where(t => !double.IsNaN(t.MCC_Target)).Select(t => t.MCC_Target).ToList();

                    //get p-value
                    if (parameter1 == parameter2)
                    {
                        anovapvalues.Add(1d);
                        wilcoxpvalues.Add(1d);
                        ttestpvalues.Add(1d);
                    }
                    else if (data1.Count <= 2 || data2.Count <= 2)
                    {
                        anovapvalues.Add(double.NaN);
                        wilcoxpvalues.Add(double.NaN);
                        ttestpvalues.Add(double.NaN);
                    }
                    else
                    {
                        anovapvalues.Add(Statistics.CalculateP(data1, data2));
                        wilcoxpvalues.Add(new Accord.Statistics.Testing.MannWhitneyWilcoxonTest(data1.ToArray(), data2.ToArray(), exact:false).PValue);
                        ttestpvalues.Add(new Accord.Statistics.Testing.TwoSampleTTest(data1.ToArray(), data2.ToArray(), false).PValue);
                    }

                }
                anovaOutlines.Add(parameter1 + "," + String.Join(",", anovapvalues));
                wilcoxOutlines.Add(parameter1 + "," + String.Join(",", wilcoxpvalues));
                ttestOutlines.Add(parameter1 + "," + String.Join(",", ttestpvalues));
            }

            return Tuple.Create(anovaOutlines, wilcoxOutlines, ttestOutlines);
        }
        
        public enum TargetData { SP, TS }
        
    }
    
}
