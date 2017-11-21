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
        const double BucketGranularity = 0.05; //creates a bucket every 0.05 of completion
        const double FmetricBeta = 1;
        private const int PlotModelHeight = 512;
        private const int PlotModelWidth = 512;

        static void Main(string[] args)
        {
            System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;

            DirectoryInfo InFolder = new DirectoryInfo(@"Y:\Sicherung\Adrian\Sync\Sciebo\MA RNN-LSTM Results\raw");
            DirectoryInfo ResultsFolder = new DirectoryInfo(@"Y:\Sicherung\Adrian\Sync\Sciebo\MA RNN-LSTM Results");
            List<FileInfo> InFiles = InFolder.EnumerateFiles("*",SearchOption.AllDirectories).Where(t => t.Name.Contains(".csv") && !t.Name.Contains(".edited.csv")).ToList();

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
            #endregion

            foreach (var file in InFiles)
            {
                using (TextFieldParser parser = new TextFieldParser(file.FullName))
                {
                    List<Line> output = new List<Line>();

                    parser.TextFieldType = FieldType.Delimited;
                    parser.SetDelimiters(",");
                    bool firstline = true;
                    List<String> Parameters = ExtractParams(file.Name.Replace("results-", String.Empty).Replace(".csv", String.Empty));
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
                        OxyPlot.PdfExporter.Export(model_sp, filestream, PlotModelHeight, PlotModelWidth);
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
                        OxyPlot.PdfExporter.Export(model_ts, filestream, PlotModelHeight, PlotModelWidth);
                        filestream.Close();
                    }
                    //add to global
                    model_glob_precision_ts.Series.Add(precisionSeries_ts_global);
                    model_glob_recall_ts.Series.Add(recallSeries_ts_global);
                    model_glob_speceficity_ts.Series.Add(speceficitySeries_ts_global);
                    model_glob_falsepositives_ts.Series.Add(falsepositivesSeries_ts_global);
                    model_glob_negativepredictions_ts.Series.Add(negativepredictedSeries_ts_global);
                    model_glob_accuracy_ts.Series.Add(accuracySeries_ts_global);
                }
            }

            #region print global
            using (var filestream = new FileStream($"{ResultsFolder.FullName}\\global_precision_sp.pdf", FileMode.OpenOrCreate))
            {
                OxyPlot.PdfExporter.Export(model_glob_precision_sp, filestream, PlotModelHeight, PlotModelWidth);
                filestream.Close();
            }
            using (var filestream = new FileStream($"{ResultsFolder.FullName}\\global_recall_sp.pdf", FileMode.OpenOrCreate))
            {
                OxyPlot.PdfExporter.Export(model_glob_recall_sp, filestream, PlotModelHeight, PlotModelWidth);
                filestream.Close();
            }
            using (var filestream = new FileStream($"{ResultsFolder.FullName}\\global_speceficity_sp.pdf", FileMode.OpenOrCreate))
            {
                OxyPlot.PdfExporter.Export(model_glob_speceficity_sp, filestream, PlotModelHeight, PlotModelWidth);
                filestream.Close();
            }
            using (var filestream = new FileStream($"{ResultsFolder.FullName}\\global_falsepositives_sp.pdf", FileMode.OpenOrCreate))
            {
                OxyPlot.PdfExporter.Export(model_glob_falsepositives_sp, filestream, PlotModelHeight, PlotModelWidth);
                filestream.Close();
            }
            using (var filestream = new FileStream($"{ResultsFolder.FullName}\\global_negativepredictions_sp.pdf", FileMode.OpenOrCreate))
            {
                OxyPlot.PdfExporter.Export(model_glob_negativepredictions_sp, filestream, PlotModelHeight, PlotModelWidth);
                filestream.Close();
            }
            using (var filestream = new FileStream($"{ResultsFolder.FullName}\\global_accuracy_sp.pdf", FileMode.OpenOrCreate))
            {
                OxyPlot.PdfExporter.Export(model_glob_accuracy_sp, filestream, PlotModelHeight, PlotModelWidth);
                filestream.Close();
            }

            //ts
            using (var filestream = new FileStream($"{ResultsFolder.FullName}\\global_precision_ts.pdf", FileMode.OpenOrCreate))
            {
                OxyPlot.PdfExporter.Export(model_glob_precision_ts, filestream, PlotModelHeight, PlotModelWidth);
                filestream.Close();
            }
            using (var filestream = new FileStream($"{ResultsFolder.FullName}\\global_recall_ts.pdf", FileMode.OpenOrCreate))
            {
                OxyPlot.PdfExporter.Export(model_glob_recall_ts, filestream, PlotModelHeight, PlotModelWidth);
                filestream.Close();
            }
            using (var filestream = new FileStream($"{ResultsFolder.FullName}\\global_speceficity_ts.pdf", FileMode.OpenOrCreate))
            {
                OxyPlot.PdfExporter.Export(model_glob_speceficity_ts, filestream, PlotModelHeight, PlotModelWidth);
                filestream.Close();
            }
            using (var filestream = new FileStream($"{ResultsFolder.FullName}\\global_falsepositives_ts.pdf", FileMode.OpenOrCreate))
            {
                OxyPlot.PdfExporter.Export(model_glob_falsepositives_ts, filestream, PlotModelHeight, PlotModelWidth);
                filestream.Close();
            }
            using (var filestream = new FileStream($"{ResultsFolder.FullName}\\global_negativepredictions_ts.pdf", FileMode.OpenOrCreate))
            {
                OxyPlot.PdfExporter.Export(model_glob_negativepredictions_ts, filestream, PlotModelHeight, PlotModelWidth);
                filestream.Close();
            }
            using (var filestream = new FileStream($"{ResultsFolder.FullName}\\global_accuracy_ts.pdf", FileMode.OpenOrCreate))
            {
                OxyPlot.PdfExporter.Export(model_glob_accuracy_ts, filestream, PlotModelHeight, PlotModelWidth);
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

            //we only want 1,2,3; neurons, dropout, patience
            return new List<string>() {paras[1], paras[2] , paras[3] };
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
