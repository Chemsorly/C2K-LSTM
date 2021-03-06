﻿using Microsoft.VisualBasic.FileIO;
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
using System.Collections.Concurrent;

namespace Analyser
{
    public class Program
    {
        private static readonly int MaxTasks = 12; //maximum amount of parallel tasks
        private static readonly double BucketGranularity = 0.1; //creates a bucket every 0.1 of completion
        private static readonly double FmetricBeta = 1;
        private static readonly double[] ReliabilityThresholds = {
            0.5, //0.52, 0.54, 0.56, 0.58,
            //0.6, //0.62, 0.64, 0.66, 0.68,
            //0.7, //0.72, 0.74, 0.76, 0.78,
            //0.8, //0.82, 0.84, 0.86, 0.88,
            //0.9, //0.92, 0.94, 0.96, 0.98,
            //1.0
        }; //turns true predictions to false if below threshold (values between 0.5 and 1; 0.5 = no prediction changes)

        //bucketing type: defines how results are bucketet
        //1 = normal bucketing over all results
        //2 = triple ranged: 0% - 50%, 50%, 50% - 100% (c2k only)
        private static readonly int BucketingType = 1;

        //violation type: predictions bigger than planned equal violation
        //TT dataset: false
        //C2K dataset: true
        private static readonly bool PositiveIsViolation = false;

        /// <summary>
        /// number of test cases to check against. ignores the entry if number is not equal and notifies the user after the procedure; -1 for ignore
        /// </summary>
        public static readonly int TestCasesCount = -1; //49002;//17690;

        //removes test instances from ensembles if they are below the threshold
        //true: test instances do not appear in the output (i.e. only instances with r >= R are considered)
        //false: test instances with r < R have their true predictions turned into false predictions (i.e. no adaptation considered)
        public static readonly bool RemoveBelowReliabilityThreshold = false;

        private static readonly int PlotModelWidth = 512;
        private static readonly int PlotModelHeight = 512;

        static List<String> folders = Folderlist.GetFolderlist();

        private static readonly bool clearFolder = true;
        private static readonly bool clearBadData = false;
        private static readonly bool verbose = true;

        static void Main(string[] args)
        {
            //enforce decimal encoding
            System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            CultureInfo.DefaultThreadCurrentCulture = customCulture;

            ParallelOptions options = new ParallelOptions()
            {
                MaxDegreeOfParallelism = MaxTasks
            };

            foreach (var folder in folders)
            {
                //target folders
                DirectoryInfo ResultsFolder = new DirectoryInfo(folder);
                DirectoryInfo InFolder = new DirectoryInfo(ResultsFolder.FullName + @"\models");
                DirectoryInfo OutFolder = new DirectoryInfo(ResultsFolder.FullName + @"\evaluation");

                //create ensemble folder (if not exist)
                Directory.CreateDirectory(OutFolder.FullName + @"\ensembles");
                DirectoryInfo EnsembleFolder = new DirectoryInfo(OutFolder.FullName + @"\ensembles");
                List<FileInfo> InFiles = InFolder.EnumerateFiles("*", SearchOption.AllDirectories).Where(t => t.Name.Contains("results.csv") && !t.Name.Contains(".edited.csv")).ToList();

                if (!InFiles.Any())
                {
                    Logger.AddErrorMessage($"ERROR: no files found in {ResultsFolder.FullName}");
                    continue;
                }
                   

                //clear folder if defined //WARNING: WILL DELETE ALL FILES IN SAID FOLDER
                if (clearFolder)
                {
                    //clear root
                    OutFolder.GetFiles().ToList().ForEach(t => t.Delete());
                    //clear ensemble folders
                    EnsembleFolder.GetFiles().ToList().ForEach(t => t.Delete());
                    EnsembleFolder.GetDirectories().ToList().ForEach(t => t.Delete(true));
                }


                //globals
                int maxSequences = 0;
                List<Bucket> allBuckets = new List<Bucket>();
                List<List<Bucket>> ensembleBuckets = new List<List<Bucket>>();

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

                #endregion

                int counter = 0;
                ConcurrentBag<List<Line>> bagLines = new ConcurrentBag<List<Line>>();
                Parallel.ForEach(InFiles, options, file =>
                {
                    int rows = 0;
                    List<Line> output = new List<Line>();
                    List<String> Parameters = ExtractParams(file.Directory.Name);

                    using (TextFieldParser parser = new TextFieldParser(file.FullName))
                    {
                        parser.TextFieldType = FieldType.Delimited;
                        parser.SetDelimiters(",");

                        //generate line objects
                        output = GetLinesFromData(file.FullName, parser, ref rows, PositiveIsViolation);
                    }

                    //check for inconsistencies
                    if (TestCasesCount != -1 && output.Count != TestCasesCount)
                    {
                        Logger.AddErrorMessage($"{file.FullName} line count does not equal {TestCasesCount}");
                        if (clearBadData)
                        {
                            Logger.AddErrorMessage($"deleting {file.FullName}");
                            file.Directory.Delete(true);                            
                        }
                        return; //abort
                    }                        

                    //save longest sequence
                    if (rows > maxSequences)
                        maxSequences = rows;

                    //get buckets
                    var BucketList = Bucketing.CreateBuckets(BucketGranularity, Parameters, BucketingType, output);

                    //run workload
                    RunPerFileWorkload(output, ref bagLines, BucketList, ref allBuckets, ref ensembleBuckets, Parameters, file.FullName,
                    model_glob_precision_target,
                    model_glob_recall_target,
                    model_glob_speceficity_target,
                    model_glob_falsepositives_target,
                    model_glob_negativepredictions_target,
                    model_glob_accuracy_target,
                    model_glob_mcc_target, model_glob_fmetric_target,
                    ref counter,
                    folder,
                    "raw");
                });

                //add ensembles on top
                #region ensemble                

                OxyPlot.PlotModel ensemblePlot_bysizeUnsorted = new PlotModel() { Title = "Results: unsorted ensemble majority vote by ensemble size" };
                ensemblePlot_bysizeUnsorted.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0, Maximum = ensembleBuckets.Count, Title = "Ensemble Size" });
                ensemblePlot_bysizeUnsorted.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = -1, Maximum = 1, Title = "MCC Value" });
                LineSeries mccensemblesizeUnsortedSeries = new LineSeries() { Title = "mcc" };
                LineSeries reliabilityensemblesizeUnsortedSeries = new LineSeries() { Title = "reliability" };
                ensemblePlot_bysizeUnsorted.Series.Add(mccensemblesizeUnsortedSeries);
                ensemblePlot_bysizeUnsorted.Series.Add(reliabilityensemblesizeUnsortedSeries);
                ensemblePlot_bysizeUnsorted.IsLegendVisible = true;

                OxyPlot.PlotModel ensemblePlot_bysizeBoosted = new PlotModel() { Title = "Results: boosted ensemble majority vote by ensemble size" };
                ensemblePlot_bysizeBoosted.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0, Maximum = ensembleBuckets.Count, Title = "Ensemble Size" });
                ensemblePlot_bysizeBoosted.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = -1, Maximum = 1, Title = "MCC Value" });
                LineSeries mccensemblesizeBoostedSeries = new LineSeries() { Title = "mcc" };
                LineSeries reliabilityensemblesizeBoostedSeries = new LineSeries() { Title = "reliability" };
                ensemblePlot_bysizeBoosted.Series.Add(mccensemblesizeBoostedSeries);
                ensemblePlot_bysizeBoosted.Series.Add(reliabilityensemblesizeBoostedSeries);
                ensemblePlot_bysizeBoosted.IsLegendVisible = true;

                OxyPlot.PlotModel ensemblePlot_byprogressUnsorted = new PlotModel() { Title = "Results: unsorted ensemble majority vote by process progress" };
                ensemblePlot_byprogressUnsorted.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0, Maximum = 1, Title = "Progress" });
                ensemblePlot_byprogressUnsorted.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1, Title = "MCC / Reliability" });
                ensemblePlot_byprogressUnsorted.IsLegendVisible = true;

                OxyPlot.PlotModel ensemblePlot_byprogressBoosted = new PlotModel() { Title = "Results: boosted ensemble majority vote by process progress" };
                ensemblePlot_byprogressBoosted.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0, Maximum = 1, Title = "Progress" });
                ensemblePlot_byprogressBoosted.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1, Title = "MCC / Reliability" });
                ensemblePlot_byprogressBoosted.IsLegendVisible = true;

                var allLines = bagLines.ToList();
                var mccsortedBuckets = ensembleBuckets.Select(t => t.Where(u => !double.IsNaN(u.MCC))).OrderByDescending(t => t.Sum(u => u.MCC)); //.OrderBy(t => t.Where(u => !double.IsNaN(u.MCC_Target)).Sum(u => u.MCC_Target));
                var mccsortedAllLines = mccsortedBuckets.Select(t => t.SelectMany(u => u.Lines).ToList()).ToList();

                ReliabilityThresholds.ToList().ForEach(reliabilityThreshold =>
                {
                    //run for unsorted
                    Parallel.For(0, allLines.Count, options, i =>  //(int i = 0; i < allLines.Count; i++)
                    {
                        String targetFilename = $"raw_ensemble_size_unsorted{i}_mcc.csv";

                        List<List<Line>> items = new List<List<Line>>();
                        List<String> filePaths = new List<String>();
                        for (int j = 0; j <= i; j++)
                        {
                            items.Add(allLines[j]);
                        }                            

                        Ensemble ensemble = new Ensemble(items, reliabilityThreshold);
                        mccensemblesizeUnsortedSeries.Points.Add(new DataPoint(ensemble.EnsembleSize, ensemble.MCC));
                        reliabilityensemblesizeUnsortedSeries.Points.Add(new DataPoint(ensemble.EnsembleSize, ensemble.Reliability));
                        //export to csv
                        Directory.CreateDirectory($"{EnsembleFolder.FullName}\\{reliabilityThreshold}\\{i}");
                        File.WriteAllLines($"{EnsembleFolder.FullName}\\{reliabilityThreshold}\\{i}\\{targetFilename}", ensemble.ExportToCsv());

                        var ensembleLines = GetLinesFromEnsemble(ensemble, false, false, reliabilityThreshold, RemoveBelowReliabilityThreshold);
                        //get buckets
                        var BucketList = Bucketing.CreateBuckets(BucketGranularity, new List<string>() { $"ensemble_unsorted_{reliabilityThreshold}", i.ToString(), "100", "0.1", "20", "1" }, BucketingType, ensembleLines);

                        //create models
                        OxyPlot.PlotModel ensembleBoxplot = new PlotModel() { Title = "Reliability distribution by progress" };
                        ensembleBoxplot.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0, Maximum = 1, Title = "Process completion" });
                        ensembleBoxplot.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0.5, Maximum = 1, Title = "Reliability" });
                        var ensembleBoxplotSeries = new BoxPlotSeries() { BoxWidth = 0.025 };
                        ensembleBoxplot.Series.Add(ensembleBoxplotSeries);

                        LineSeries mccensembleprogressSeries = new LineSeries() { Title = $"mcc ensemble {i}" };
                        LineSeries reliabilityensembleprogressseries = new LineSeries() { Title = $"reliability ensemble {i}" };
                        ensemblePlot_byprogressUnsorted.Series.Add(mccensembleprogressSeries);
                        ensemblePlot_byprogressUnsorted.Series.Add(reliabilityensembleprogressseries);
                        foreach (var bucket in BucketList)
                        {
                            //add buckets to chart
                            mccensembleprogressSeries.Points.Add(new DataPoint(bucket.BucketLevel * BucketGranularity, bucket.MCC));
                            reliabilityensembleprogressseries.Points.Add(new DataPoint(bucket.BucketLevel * BucketGranularity, ensemble.GetReliabilityForBucket(bucket.BucketLevel, BucketGranularity)));
                            var item = CreateBoxplot(ensemble.GetReliabilitiesForBucket(bucket.BucketLevel, BucketGranularity), bucket.BucketLevel * BucketGranularity);
                            if (item != null)
                                ensembleBoxplotSeries.Items.Add(item);
                        }
                        RunPerFileWorkload(ensembleLines, ref bagLines, BucketList, ref allBuckets, ref ensembleBuckets, new List<string>() { "ensemble unsorted" },
                            $"{EnsembleFolder.FullName}\\{reliabilityThreshold}\\{i}\\{targetFilename}",
                            model_glob_precision_target,
                            model_glob_recall_target,
                            model_glob_speceficity_target,
                            model_glob_falsepositives_target,
                            model_glob_negativepredictions_target,
                            model_glob_accuracy_target,
                            model_glob_mcc_target, model_glob_fmetric_target,
                            ref counter,
                            folder,
                            "ensemble");

                        using (var filestream = new FileStream($"{EnsembleFolder.FullName}\\{reliabilityThreshold}\\{i}\\ensemble_boxplot_unsorted_{i} .pdf", FileMode.OpenOrCreate))
                        {
                            OxyPlot.PdfExporter.Export(ensembleBoxplot, filestream, PlotModelWidth, PlotModelHeight);
                            filestream.Close();
                        }
                    });
                    mccensemblesizeUnsortedSeries.Points.Sort((x, y) => x.X.CompareTo(y.X));
                    reliabilityensemblesizeUnsortedSeries.Points.Sort((x, y) => x.X.CompareTo(y.X));

                    //run for mcc sorted (aka boosted)
                    Parallel.For(0, mccsortedAllLines.Count, options, i => //for (int i = 0; i < mccsortedAllLines.Count; i++)
                    {
                        String targetFilename = $"raw_ensemble_size_boosted{i}_mcc.csv";

                        List<List<Line>> items = new List<List<Line>>();
                        for (int j = 0; j <= i; j++)
                            items.Add(mccsortedAllLines[j]);

                        Ensemble ensemble = new Ensemble(items, reliabilityThreshold);
                        mccensemblesizeBoostedSeries.Points.Add(new DataPoint(ensemble.EnsembleSize, ensemble.MCC));
                        reliabilityensemblesizeBoostedSeries.Points.Add(new DataPoint(ensemble.EnsembleSize, ensemble.Reliability));
                        //export to csv
                        File.WriteAllLines($"{EnsembleFolder.FullName}\\{reliabilityThreshold}\\{i}\\{targetFilename}", ensemble.ExportToCsv());

                        var ensembleLines = GetLinesFromEnsemble(ensemble, false, false, reliabilityThreshold, RemoveBelowReliabilityThreshold);
                        //get buckets
                        var BucketList = Bucketing.CreateBuckets(BucketGranularity, new List<string>() { $"ensemble_boosted_{reliabilityThreshold}", i.ToString(), "100", "0.1", "20", "1" }, BucketingType, ensembleLines);

                        LineSeries mccensembleprogressSeries = new LineSeries() { Title = $"mcc ensemble {i}" };
                        LineSeries reliabilityensembleprogressseries = new LineSeries() { Title = $"reliability ensemble {i}" };
                        ensemblePlot_byprogressBoosted.Series.Add(mccensembleprogressSeries);
                        ensemblePlot_byprogressBoosted.Series.Add(reliabilityensembleprogressseries);
                        foreach (var bucket in BucketList)
                        {
                            //add buckets to chart
                            mccensembleprogressSeries.Points.Add(new DataPoint(bucket.BucketLevel * BucketGranularity, bucket.MCC));
                            reliabilityensembleprogressseries.Points.Add(new DataPoint(bucket.BucketLevel * BucketGranularity, ensemble.GetReliabilityForBucket(bucket.BucketLevel, BucketGranularity)));
                        }
                        RunPerFileWorkload(ensembleLines, ref bagLines, BucketList, ref allBuckets, ref ensembleBuckets, new List<string>() { "ensemble boosted" },
                            $"{EnsembleFolder.FullName}\\{reliabilityThreshold}\\{i}\\{targetFilename}",
                            model_glob_precision_target,
                            model_glob_recall_target,
                            model_glob_speceficity_target,
                            model_glob_falsepositives_target,
                            model_glob_negativepredictions_target,
                            model_glob_accuracy_target,
                            model_glob_mcc_target, model_glob_fmetric_target,
                            ref counter, 
                            folder,
                            "ensemble boosted");
                    });
                    mccensemblesizeBoostedSeries.Points.Sort((x, y) => x.X.CompareTo(y.X));
                    reliabilityensemblesizeBoostedSeries.Points.Sort((x, y) => x.X.CompareTo(y.X));

                    using (var filestream = new FileStream($"{EnsembleFolder.FullName}\\ensemble_mcc_bysize_unsorted.pdf", FileMode.OpenOrCreate))
                    {
                        OxyPlot.PdfExporter.Export(ensemblePlot_bysizeUnsorted, filestream, PlotModelWidth * 2, PlotModelHeight);
                        filestream.Close();
                    }
                    using (var filestream = new FileStream($"{EnsembleFolder.FullName}\\ensemble_mcc_byprogress_unsorted.pdf", FileMode.OpenOrCreate))
                    {
                        OxyPlot.PdfExporter.Export(ensemblePlot_byprogressUnsorted, filestream, PlotModelWidth * 2, PlotModelHeight);
                        filestream.Close();
                    }
                    using (var filestream = new FileStream($"{EnsembleFolder.FullName}\\ensemble_mcc_bysize_boosted.pdf", FileMode.OpenOrCreate))
                    {
                        OxyPlot.PdfExporter.Export(ensemblePlot_bysizeBoosted, filestream, PlotModelWidth * 2, PlotModelHeight);
                        filestream.Close();
                    }
                    using (var filestream = new FileStream($"{EnsembleFolder.FullName}\\ensemble_mcc_byprogress_boosted.pdf", FileMode.OpenOrCreate))
                    {
                        OxyPlot.PdfExporter.Export(ensemblePlot_byprogressBoosted, filestream, PlotModelWidth * 2, PlotModelHeight);
                        filestream.Close();
                    }
                });

                #endregion
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
                    if (bucket != null)
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
                }

                //generate values
                for (int i = 0; i * BucketGranularity < 1; i++)
                {
                    var buckets = allBuckets.Where(t => t != null && t.BucketLevel == i);
                    foreach (var bucket in buckets)
                    {
                        sortedbucketsPrecision[String.Join(" ", bucket.Parameters)].Add(double.IsNaN(bucket.Precision) ? "0" : bucket.Precision.ToString());
                        sortedbucketsRecall[String.Join(" ", bucket.Parameters)].Add(double.IsNaN(bucket.Recall) ? "0" : bucket.Recall.ToString());
                        sortedbucketsSpeceficity[String.Join(" ", bucket.Parameters)].Add(double.IsNaN(bucket.Specificity) ? "0" : bucket.Specificity.ToString());
                        sortedbucketsFalsepositives[String.Join(" ", bucket.Parameters)].Add(double.IsNaN(bucket.FalsePositiveRate) ? "0" : bucket.FalsePositiveRate.ToString());
                        sortedbucketsNegativepredictions[String.Join(" ", bucket.Parameters)].Add(double.IsNaN(bucket.NegativePredictedValue) ? "0" : bucket.NegativePredictedValue.ToString());
                        sortedbucketsAccuracy[String.Join(" ", bucket.Parameters)].Add(double.IsNaN(bucket.Accuracy) ? "0" : bucket.Accuracy.ToString());
                        sortedbucketsFmetric[String.Join(" ", bucket.Parameters)].Add(double.IsNaN(bucket.FMeasure) ? "0" : bucket.FMeasure.ToString());
                        sortedbucketsMCC[String.Join(" ", bucket.Parameters)].Add(double.IsNaN(bucket.MCC) ? "0" : bucket.MCC.ToString());
                        sortedbucketsMSE[String.Join(" ", bucket.Parameters)].Add(double.IsNaN(bucket.MSE) ? "0" : bucket.MSE.ToString());
                        sortedbucketsRMSE[String.Join(" ", bucket.Parameters)].Add(double.IsNaN(bucket.RMSE) ? "0" : bucket.RMSE.ToString());
                        sortedbucketsMAE[String.Join(" ", bucket.Parameters)].Add(double.IsNaN(bucket.MAE) ? "0" : bucket.MAE.ToString());
                        sortedbucketsRSE[String.Join(" ", bucket.Parameters)].Add(double.IsNaN(bucket.RSE) ? "0" : bucket.RSE.ToString());
                        sortedbucketsRRSE[String.Join(" ", bucket.Parameters)].Add(double.IsNaN(bucket.RRSE) ? "0" : bucket.RRSE.ToString());
                        sortedbucketsRAE[String.Join(" ", bucket.Parameters)].Add(double.IsNaN(bucket.RAE) ? "0" : bucket.RAE.ToString());
                    }
                }

                for (int i = 0; i < sortedbucketsList.Count; i++)
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

                    File.WriteAllLines($"{OutFolder.FullName}\\raw_{sortedbucketsListParameters[i]}.csv", outLines);
                    File.WriteAllLines($"{OutFolder.FullName}\\raw_{sortedbucketsListParameters[i]}2.csv", outLines2);
                }

                #region print global
                using (var filestream = new FileStream($"{OutFolder.FullName}\\global_precision_target.pdf", FileMode.OpenOrCreate))
                {
                    OxyPlot.PdfExporter.Export(model_glob_precision_target, filestream, PlotModelWidth, PlotModelHeight);
                    filestream.Close();
                }
                using (var filestream = new FileStream($"{OutFolder.FullName}\\global_recall_target.pdf", FileMode.OpenOrCreate))
                {
                    OxyPlot.PdfExporter.Export(model_glob_recall_target, filestream, PlotModelWidth, PlotModelHeight);
                    filestream.Close();
                }
                using (var filestream = new FileStream($"{OutFolder.FullName}\\global_speceficity_target.pdf", FileMode.OpenOrCreate))
                {
                    OxyPlot.PdfExporter.Export(model_glob_speceficity_target, filestream, PlotModelWidth, PlotModelHeight);
                    filestream.Close();
                }
                using (var filestream = new FileStream($"{OutFolder.FullName}\\global_falsepositives_target.pdf", FileMode.OpenOrCreate))
                {
                    OxyPlot.PdfExporter.Export(model_glob_falsepositives_target, filestream, PlotModelWidth, PlotModelHeight);
                    filestream.Close();
                }
                using (var filestream = new FileStream($"{OutFolder.FullName}\\global_negativepredictions_target.pdf", FileMode.OpenOrCreate))
                {
                    OxyPlot.PdfExporter.Export(model_glob_negativepredictions_target, filestream, PlotModelWidth, PlotModelHeight);
                    filestream.Close();
                }
                using (var filestream = new FileStream($"{OutFolder.FullName}\\global_accuracy_target.pdf", FileMode.OpenOrCreate))
                {
                    OxyPlot.PdfExporter.Export(model_glob_accuracy_target, filestream, PlotModelWidth, PlotModelHeight);
                    filestream.Close();
                }
                using (var filestream = new FileStream($"{OutFolder.FullName}\\global_fmetric_target.pdf", FileMode.OpenOrCreate))
                {
                    OxyPlot.PdfExporter.Export(model_glob_fmetric_target, filestream, PlotModelWidth, PlotModelHeight);
                    filestream.Close();
                }
                using (var filestream = new FileStream($"{OutFolder.FullName}\\global_mcc_target.pdf", FileMode.OpenOrCreate))
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
                using (var filestream = new FileStream($"{OutFolder.FullName}\\global_valid_sequences.pdf", FileMode.OpenOrCreate))
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
                using (var filestream = new FileStream($"{OutFolder.FullName}\\global_predicted_sequences.pdf", FileMode.OpenOrCreate))
                {
                    OxyPlot.PdfExporter.Export(model_glob_predictedsequences, filestream, PlotModelWidth * 6, PlotModelHeight);
                    filestream.Close();
                }
                #endregion

                #region boxplots
                //var boxplotSeries_target_neurons = new BoxPlotSeries() { };
                //model_glob_boxplot_neurons_target.Series.Add(boxplotSeries_target_neurons);
                //var parameters = allParameters[0].Distinct().ToList();
                //parameters.Sort();
                //for (int i = 0; i < parameters.Count; i++)
                //{
                //    model_glob_boxplot_neurons_target_cataxis.ActualLabels.Add($"Precision {parameters[i]}");
                //    boxplotSeries_target_neurons.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[0] == parameters[i].ToString()).Select(t => t.PrecisionTarget).ToList(), i));
                //}
                //for (int i = 0; i < parameters.Count; i++)
                //{
                //    model_glob_boxplot_neurons_target_cataxis.ActualLabels.Add($"Recall {parameters[i]}");
                //    boxplotSeries_target_neurons.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[0] == parameters[i].ToString()).Select(t => t.RecallTarget).ToList(), i + (parameters.Count * 1)));
                //}
                //for (int i = 0; i < parameters.Count; i++)
                //{
                //    model_glob_boxplot_neurons_target_cataxis.ActualLabels.Add($"Speceficity {parameters[i]}");
                //    boxplotSeries_target_neurons.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[0] == parameters[i].ToString()).Select(t => t.SpecificityTarget).ToList(), i + (parameters.Count * 2)));
                //}
                //for (int i = 0; i < parameters.Count; i++)
                //{
                //    model_glob_boxplot_neurons_target_cataxis.ActualLabels.Add($"FalsePositive {parameters[i]}");
                //    boxplotSeries_target_neurons.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[0] == parameters[i].ToString()).Select(t => t.FalsePositiveRateTarget).ToList(), i + (parameters.Count * 3)));
                //}
                //for (int i = 0; i < parameters.Count; i++)
                //{
                //    model_glob_boxplot_neurons_target_cataxis.ActualLabels.Add($"NegativePredicted {parameters[i]}");
                //    boxplotSeries_target_neurons.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[0] == parameters[i].ToString()).Select(t => t.NegativePredictedValueTarget).ToList(), i + (parameters.Count * 4)));
                //}
                //for (int i = 0; i < parameters.Count; i++)
                //{
                //    model_glob_boxplot_neurons_target_cataxis.ActualLabels.Add($"Accuracy {parameters[i]}");
                //    boxplotSeries_target_neurons.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[0] == parameters[i].ToString()).Select(t => t.AccuracyTarget).ToList(), i + (parameters.Count * 5)));
                //}
                //for (int i = 0; i < parameters.Count; i++)
                //{
                //    model_glob_boxplot_neurons_target_cataxis.ActualLabels.Add($"Fmetric {parameters[i]}");
                //    boxplotSeries_target_neurons.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[0] == parameters[i].ToString()).Select(t => t.FMeasureTarget).ToList(), i + (parameters.Count * 6)));
                //}
                //for (int i = 0; i < parameters.Count; i++)
                //{
                //    model_glob_boxplot_neurons_target_cataxis.ActualLabels.Add($"MCC {parameters[i]}");
                //    boxplotSeries_target_neurons.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[0] == parameters[i].ToString()).Select(t => t.MCC_Target).ToList(), i + (parameters.Count * 7)));
                //}
                //using (var filestream = new FileStream($"{OutFolder.FullName}\\global_boxplot_neurons_target.pdf", FileMode.OpenOrCreate))
                //{
                //    OxyPlot.PdfExporter.Export(model_glob_boxplot_neurons_target, filestream, PlotModelWidth * 3, PlotModelHeight);
                //    filestream.Close();
                //}

                //var boxplotSeries_target_dropout = new BoxPlotSeries() { };
                //model_glob_boxplot_dropout_target.Series.Add(boxplotSeries_target_dropout);
                //parameters = allParameters[1].Distinct().ToList();
                //parameters.Sort();
                //for (int i = 0; i < parameters.Count; i++)
                //{
                //    model_glob_boxplot_dropout_target_cataxis.ActualLabels.Add($"Precision {parameters[i]}");
                //    boxplotSeries_target_dropout.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[1] == parameters[i].ToString()).Select(t => t.PrecisionTarget).ToList(), i));
                //}
                //for (int i = 0; i < parameters.Count; i++)
                //{
                //    model_glob_boxplot_dropout_target_cataxis.ActualLabels.Add($"Recall {parameters[i]}");
                //    boxplotSeries_target_dropout.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[1] == parameters[i].ToString()).Select(t => t.RecallTarget).ToList(), i + (parameters.Count * 1)));
                //}
                //for (int i = 0; i < parameters.Count; i++)
                //{
                //    model_glob_boxplot_dropout_target_cataxis.ActualLabels.Add($"Speceficity {parameters[i]}");
                //    boxplotSeries_target_dropout.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[1] == parameters[i].ToString()).Select(t => t.SpecificityTarget).ToList(), i + (parameters.Count * 2)));
                //}
                //for (int i = 0; i < parameters.Count; i++)
                //{
                //    model_glob_boxplot_dropout_target_cataxis.ActualLabels.Add($"FalsePositive {parameters[i]}");
                //    boxplotSeries_target_dropout.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[1] == parameters[i].ToString()).Select(t => t.FalsePositiveRateTarget).ToList(), i + (parameters.Count * 3)));
                //}
                //for (int i = 0; i < parameters.Count; i++)
                //{
                //    model_glob_boxplot_dropout_target_cataxis.ActualLabels.Add($"NegativePredicted {parameters[i]}");
                //    boxplotSeries_target_dropout.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[1] == parameters[i].ToString()).Select(t => t.NegativePredictedValueTarget).ToList(), i + (parameters.Count * 4)));
                //}
                //for (int i = 0; i < parameters.Count; i++)
                //{
                //    model_glob_boxplot_dropout_target_cataxis.ActualLabels.Add($"Accuracy {parameters[i]}");
                //    boxplotSeries_target_dropout.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[1] == parameters[i].ToString()).Select(t => t.AccuracyTarget).ToList(), i + (parameters.Count * 5)));
                //}
                //for (int i = 0; i < parameters.Count; i++)
                //{
                //    model_glob_boxplot_dropout_target_cataxis.ActualLabels.Add($"Fmetric {parameters[i]}");
                //    boxplotSeries_target_dropout.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[1] == parameters[i].ToString()).Select(t => t.FMeasureTarget).ToList(), i + (parameters.Count * 6)));
                //}
                //for (int i = 0; i < parameters.Count; i++)
                //{
                //    model_glob_boxplot_dropout_target_cataxis.ActualLabels.Add($"MCC {parameters[i]}");
                //    boxplotSeries_target_dropout.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[1] == parameters[i].ToString()).Select(t => t.MCC_Target).ToList(), i + (parameters.Count * 7)));
                //}
                //using (var filestream = new FileStream($"{OutFolder.FullName}\\global_boxplot_dropout_target.pdf", FileMode.OpenOrCreate))
                //{
                //    OxyPlot.PdfExporter.Export(model_glob_boxplot_dropout_target, filestream, PlotModelWidth * 3, PlotModelHeight);
                //    filestream.Close();
                //}

                //var boxplotSeries_target_patience = new BoxPlotSeries() { };
                //model_glob_boxplot_patience_target.Series.Add(boxplotSeries_target_patience);
                //parameters = allParameters[2].Distinct().ToList();
                //parameters.Sort();
                //for (int i = 0; i < parameters.Count; i++)
                //{
                //    model_glob_boxplot_patience_target_cataxis.ActualLabels.Add($"Precision {parameters[i]}");
                //    boxplotSeries_target_patience.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[2] == parameters[i].ToString()).Select(t => t.PrecisionTarget).ToList(), i));
                //}
                //for (int i = 0; i < parameters.Count; i++)
                //{
                //    model_glob_boxplot_patience_target_cataxis.ActualLabels.Add($"Recall {parameters[i]}");
                //    boxplotSeries_target_patience.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[2] == parameters[i].ToString()).Select(t => t.RecallTarget).ToList(), i + (parameters.Count * 1)));
                //}
                //for (int i = 0; i < parameters.Count; i++)
                //{
                //    model_glob_boxplot_patience_target_cataxis.ActualLabels.Add($"Speceficity {parameters[i]}");
                //    boxplotSeries_target_patience.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[2] == parameters[i].ToString()).Select(t => t.SpecificityTarget).ToList(), i + (parameters.Count * 2)));
                //}
                //for (int i = 0; i < parameters.Count; i++)
                //{
                //    model_glob_boxplot_patience_target_cataxis.ActualLabels.Add($"FalsePositive {parameters[i]}");
                //    boxplotSeries_target_patience.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[2] == parameters[i].ToString()).Select(t => t.FalsePositiveRateTarget).ToList(), i + (parameters.Count * 3)));
                //}
                //for (int i = 0; i < parameters.Count; i++)
                //{
                //    model_glob_boxplot_patience_target_cataxis.ActualLabels.Add($"NegativePredicted {parameters[i]}");
                //    boxplotSeries_target_patience.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[2] == parameters[i].ToString()).Select(t => t.NegativePredictedValueTarget).ToList(), i + (parameters.Count * 4)));
                //}
                //for (int i = 0; i < parameters.Count; i++)
                //{
                //    model_glob_boxplot_patience_target_cataxis.ActualLabels.Add($"Accuracy {parameters[i]}");
                //    boxplotSeries_target_patience.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[2] == parameters[i].ToString()).Select(t => t.AccuracyTarget).ToList(), i + (parameters.Count * 5)));
                //}
                //for (int i = 0; i < parameters.Count; i++)
                //{
                //    model_glob_boxplot_patience_target_cataxis.ActualLabels.Add($"Fmetric {parameters[i]}");
                //    boxplotSeries_target_patience.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[2] == parameters[i].ToString()).Select(t => t.FMeasureTarget).ToList(), i + (parameters.Count * 6)));
                //}
                //for (int i = 0; i < parameters.Count; i++)
                //{
                //    model_glob_boxplot_patience_target_cataxis.ActualLabels.Add($"MCC {parameters[i]}");
                //    boxplotSeries_target_patience.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[2] == parameters[i].ToString()).Select(t => t.MCC_Target).ToList(), i + (parameters.Count * 7)));
                //}
                //using (var filestream = new FileStream($"{OutFolder.FullName}\\global_boxplot_patience_target.pdf", FileMode.OpenOrCreate))
                //{
                //    OxyPlot.PdfExporter.Export(model_glob_boxplot_patience_target, filestream, PlotModelWidth * 3, PlotModelHeight);
                //    filestream.Close();
                //}

                //var boxplotSeries_target_algorithm = new BoxPlotSeries() { };
                //model_glob_boxplot_algorithm_target.Series.Add(boxplotSeries_target_algorithm);
                //parameters = allParameters[3].Distinct().ToList();
                //parameters.Sort();
                //for (int i = 0; i < parameters.Count; i++)
                //{
                //    model_glob_boxplot_algorithm_target_cataxis.ActualLabels.Add($"Precision {parameters[i]}");
                //    boxplotSeries_target_algorithm.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[3] == parameters[i].ToString()).Select(t => t.PrecisionTarget).ToList(), i));
                //}
                //for (int i = 0; i < parameters.Count; i++)
                //{
                //    model_glob_boxplot_algorithm_target_cataxis.ActualLabels.Add($"Recall {parameters[i]}");
                //    boxplotSeries_target_algorithm.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[3] == parameters[i].ToString()).Select(t => t.RecallTarget).ToList(), i + (parameters.Count * 1)));
                //}
                //for (int i = 0; i < parameters.Count; i++)
                //{
                //    model_glob_boxplot_algorithm_target_cataxis.ActualLabels.Add($"Speceficity {parameters[i]}");
                //    boxplotSeries_target_algorithm.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[3] == parameters[i].ToString()).Select(t => t.SpecificityTarget).ToList(), i + (parameters.Count * 2)));
                //}
                //for (int i = 0; i < parameters.Count; i++)
                //{
                //    model_glob_boxplot_algorithm_target_cataxis.ActualLabels.Add($"FalsePositive {parameters[i]}");
                //    boxplotSeries_target_algorithm.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[3] == parameters[i].ToString()).Select(t => t.FalsePositiveRateTarget).ToList(), i + (parameters.Count * 3)));
                //}
                //for (int i = 0; i < parameters.Count; i++)
                //{
                //    model_glob_boxplot_algorithm_target_cataxis.ActualLabels.Add($"NegativePredicted {parameters[i]}");
                //    boxplotSeries_target_algorithm.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[3] == parameters[i].ToString()).Select(t => t.NegativePredictedValueTarget).ToList(), i + (parameters.Count * 4)));
                //}
                //for (int i = 0; i < parameters.Count; i++)
                //{
                //    model_glob_boxplot_algorithm_target_cataxis.ActualLabels.Add($"Accuracy {parameters[i]}");
                //    boxplotSeries_target_algorithm.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[3] == parameters[i].ToString()).Select(t => t.AccuracyTarget).ToList(), i + (parameters.Count * 5)));
                //}
                //for (int i = 0; i < parameters.Count; i++)
                //{
                //    model_glob_boxplot_algorithm_target_cataxis.ActualLabels.Add($"Fmetric {parameters[i]}");
                //    boxplotSeries_target_algorithm.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[3] == parameters[i].ToString()).Select(t => t.FMeasureTarget).ToList(), i + (parameters.Count * 6)));
                //}
                //for (int i = 0; i < parameters.Count; i++)
                //{
                //    model_glob_boxplot_algorithm_target_cataxis.ActualLabels.Add($"MCC {parameters[i]}");
                //    boxplotSeries_target_algorithm.Items.Add(CreateBoxplot(allBuckets.Where(t => (t.BucketLevel * BucketGranularity >= 0.5) && t.Parameters[3] == parameters[i].ToString()).Select(t => t.MCC_Target).ToList(), i + (parameters.Count * 7)));
                //}
                //using (var filestream = new FileStream($"{OutFolder.FullName}\\global_boxplot_algorithm_target.pdf", FileMode.OpenOrCreate))
                //{
                //    OxyPlot.PdfExporter.Export(model_glob_boxplot_algorithm_target, filestream, PlotModelWidth * 3, PlotModelHeight);
                //    filestream.Close();
                //}
                #endregion

                #region groupings
                //get and sort parameters
                //for (int group = 0; group < allParameters.Length; group++)
                //{
                //    //create grouping model
                //    OxyPlot.PlotModel model_groupings0accuracy = new PlotModel() { Title = $"Results: Avg accuracy grouped by parameter {group}" };
                //    model_groupings0accuracy.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0, Maximum = 0.95, Title = "Process completion" });
                //    model_groupings0accuracy.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1, Title = "Value" });
                //    model_groupings0accuracy.IsLegendVisible = true;
                //    OxyPlot.PlotModel model_groupings0falsepositives = new PlotModel() { Title = $"Results: Avg false positives grouped by parameter {group}" };
                //    model_groupings0falsepositives.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0, Maximum = 0.95, Title = "Process completion" });
                //    model_groupings0falsepositives.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1, Title = "Value" });
                //    model_groupings0falsepositives.IsLegendVisible = true;
                //    OxyPlot.PlotModel model_groupings0fmetric = new PlotModel() { Title = $"Results: Avg fmetric grouped by parameter {group}" };
                //    model_groupings0fmetric.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0, Maximum = 0.95, Title = "Process completion" });
                //    model_groupings0fmetric.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1, Title = "Value" });
                //    model_groupings0fmetric.IsLegendVisible = true;
                //    OxyPlot.PlotModel model_groupings0negativepredictions = new PlotModel() { Title = $"Results: Avg negative predictions grouped by parameter {group}" };
                //    model_groupings0negativepredictions.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0, Maximum = 0.95, Title = "Process completion" });
                //    model_groupings0negativepredictions.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1, Title = "Value" });
                //    model_groupings0negativepredictions.IsLegendVisible = true;
                //    OxyPlot.PlotModel model_groupings0precision = new PlotModel() { Title = $"Results: Avg precision grouped by parameter {group}" };
                //    model_groupings0precision.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0, Maximum = 0.95, Title = "Process completion" });
                //    model_groupings0precision.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1, Title = "Value" });
                //    model_groupings0precision.IsLegendVisible = true;
                //    OxyPlot.PlotModel model_groupings0recall = new PlotModel() { Title = $"Results: Avg recall grouped by parameter {group}" };
                //    model_groupings0recall.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0, Maximum = 0.95, Title = "Process completion" });
                //    model_groupings0recall.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1, Title = "Value" });
                //    model_groupings0recall.IsLegendVisible = true;
                //    OxyPlot.PlotModel model_groupings0speceficity = new PlotModel() { Title = $"Results: Avg speceficity grouped by parameter {group}" };
                //    model_groupings0speceficity.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0, Maximum = 0.95, Title = "Process completion" });
                //    model_groupings0speceficity.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1, Title = "Value" });
                //    model_groupings0speceficity.IsLegendVisible = true;
                //    OxyPlot.PlotModel model_groupings0mcc = new PlotModel() { Title = $"Results: Avg MCC grouped by parameter {group}" };
                //    model_groupings0mcc.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0, Maximum = 0.95, Title = "Process completion" });
                //    model_groupings0mcc.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1, Title = "Value" });
                //    model_groupings0mcc.IsLegendVisible = true;

                //    parameters = allParameters[group].Distinct().ToList();
                //    parameters.Sort();

                //    //set up output lines
                //    var grouping0acc_out = new List<String>();
                //    var grouping0fpr_out = new List<String>();
                //    var grouping0fmetric_out = new List<String>();
                //    var grouping0npr_out = new List<String>();
                //    var grouping0prec_out = new List<String>();
                //    var grouping0rec_out = new List<String>();
                //    var grouping0spec_out = new List<String>();
                //    var grouping0mcc_out = new List<String>();
                //    List<String> grouping0header = new List<String>() { "groupingname" };
                //    for (int j = 0; j * BucketGranularity < 1; j++)
                //        grouping0header.Add((j * BucketGranularity).ToString());
                //    grouping0acc_out.Add(String.Join(",", grouping0header));
                //    grouping0fpr_out.Add(String.Join(",", grouping0header));
                //    grouping0fmetric_out.Add(String.Join(",", grouping0header));
                //    grouping0npr_out.Add(String.Join(",", grouping0header));
                //    grouping0prec_out.Add(String.Join(",", grouping0header));
                //    grouping0rec_out.Add(String.Join(",", grouping0header));
                //    grouping0spec_out.Add(String.Join(",", grouping0header));
                //    grouping0mcc_out.Add(String.Join(",", grouping0header));

                //    //iterate through nth level grouping
                //    for (int i = 0; i < parameters.Count; i++)
                //    {
                //        var grouping0line_acc = new List<String>();
                //        var grouping0line_fpr = new List<String>();
                //        var grouping0line_fmetric = new List<String>();
                //        var grouping0line_npr = new List<String>();
                //        var grouping0line_prec = new List<String>();
                //        var grouping0line_rec = new List<String>();
                //        var grouping0line_spec = new List<String>();
                //        var grouping0line_mcc = new List<String>();
                //        grouping0line_acc.Add(parameters[i]);
                //        grouping0line_fpr.Add(parameters[i]);
                //        grouping0line_fmetric.Add(parameters[i]);
                //        grouping0line_npr.Add(parameters[i]);
                //        grouping0line_prec.Add(parameters[i]);
                //        grouping0line_rec.Add(parameters[i]);
                //        grouping0line_spec.Add(parameters[i]);
                //        grouping0line_mcc.Add(parameters[i]);
                //        var groupingSeries_acc = new LineSeries() { Title = parameters[i] };
                //        var groupingSeries_fpr = new LineSeries() { Title = parameters[i] };
                //        var groupingSeries_fmetric = new LineSeries() { Title = parameters[i] };
                //        var groupingSeries_npr = new LineSeries() { Title = parameters[i] };
                //        var groupingSeries_prec = new LineSeries() { Title = parameters[i] };
                //        var groupingSeries_rec = new LineSeries() { Title = parameters[i] };
                //        var groupingSeries_spec = new LineSeries() { Title = parameters[i] };
                //        var groupingSeries_mcc = new LineSeries() { Title = parameters[i] };

                //        //create boxplot model for each nth level grouping
                //        OxyPlot.PlotModel groupingboxplotmodel_acc = new PlotModel() { Title = $"Grouping Boxplots for {parameters[i]}" };
                //        groupingboxplotmodel_acc.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0, Maximum = 0.95, Title = "Process completion" });
                //        groupingboxplotmodel_acc.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1, Title = "Value (accuracy)" });
                //        var groupingBoxPlotSeries_acc = new BoxPlotSeries() { BoxWidth = 0.025 };
                //        groupingboxplotmodel_acc.Series.Add(groupingBoxPlotSeries_acc);

                //        OxyPlot.PlotModel groupingboxplotmodel_fpr = new PlotModel() { Title = $"Grouping Boxplots for {parameters[i]}" };
                //        groupingboxplotmodel_fpr.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0, Maximum = 0.95, Title = "Process completion" });
                //        groupingboxplotmodel_fpr.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1, Title = "Value (false positves)" });
                //        var groupingBoxPlotSeries_fpr = new BoxPlotSeries() { BoxWidth = 0.025 };
                //        groupingboxplotmodel_fpr.Series.Add(groupingBoxPlotSeries_fpr);

                //        OxyPlot.PlotModel groupingboxplotmodel_fmetric = new PlotModel() { Title = $"Grouping Boxplots for {parameters[i]}" };
                //        groupingboxplotmodel_fmetric.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0, Maximum = 0.95, Title = "Process completion" });
                //        groupingboxplotmodel_fmetric.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1, Title = "Value (fmetric)" });
                //        var groupingBoxPlotSeries_fmetric = new BoxPlotSeries() { BoxWidth = 0.025 };
                //        groupingboxplotmodel_fmetric.Series.Add(groupingBoxPlotSeries_fmetric);

                //        OxyPlot.PlotModel groupingboxplotmodel_npr = new PlotModel() { Title = $"Grouping Boxplots for {parameters[i]}" };
                //        groupingboxplotmodel_npr.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0, Maximum = 0.95, Title = "Process completion" });
                //        groupingboxplotmodel_npr.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1, Title = "Value (negative predictions)" });
                //        var groupingBoxPlotSeries_npr = new BoxPlotSeries() { BoxWidth = 0.025 };
                //        groupingboxplotmodel_npr.Series.Add(groupingBoxPlotSeries_npr);

                //        OxyPlot.PlotModel groupingboxplotmodel_prec = new PlotModel() { Title = $"Grouping Boxplots for {parameters[i]}" };
                //        groupingboxplotmodel_prec.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0, Maximum = 0.95, Title = "Process completion" });
                //        groupingboxplotmodel_prec.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1, Title = "Value (precision)" });
                //        var groupingBoxPlotSeries_prec = new BoxPlotSeries() { BoxWidth = 0.025 };
                //        groupingboxplotmodel_prec.Series.Add(groupingBoxPlotSeries_prec);

                //        OxyPlot.PlotModel groupingboxplotmodel_rec = new PlotModel() { Title = $"Grouping Boxplots for {parameters[i]}" };
                //        groupingboxplotmodel_rec.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0, Maximum = 0.95, Title = "Process completion" });
                //        groupingboxplotmodel_rec.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1, Title = "Value (recall)" });
                //        var groupingBoxPlotSeries_rec = new BoxPlotSeries() { BoxWidth = 0.025 };
                //        groupingboxplotmodel_rec.Series.Add(groupingBoxPlotSeries_rec);

                //        OxyPlot.PlotModel groupingboxplotmodel_spec = new PlotModel() { Title = $"Grouping Boxplots for {parameters[i]}" };
                //        groupingboxplotmodel_spec.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0, Maximum = 0.95, Title = "Process completion" });
                //        groupingboxplotmodel_spec.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1, Title = "Value (speceficity)" });
                //        var groupingBoxPlotSeries_spec = new BoxPlotSeries() { BoxWidth = 0.025 };
                //        groupingboxplotmodel_spec.Series.Add(groupingBoxPlotSeries_spec);

                //        OxyPlot.PlotModel groupingboxplotmodel_mcc = new PlotModel() { Title = $"Grouping Boxplots for {parameters[i]}" };
                //        groupingboxplotmodel_mcc.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0, Maximum = 0.95, Title = "Process completion" });
                //        groupingboxplotmodel_mcc.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1, Title = "Value (MCC)" });
                //        var groupingBoxPlotSeries_mcc = new BoxPlotSeries() { BoxWidth = 0.025 };
                //        groupingboxplotmodel_mcc.Series.Add(groupingBoxPlotSeries_mcc);

                //        //iterate through each bucket
                //        for (int j = 0; j * BucketGranularity < 1; j++)
                //        {
                //            var tbuckets = allBuckets.Where(t => t.BucketLevel == j && t.Parameters[group] == parameters[i].ToString());
                //            var val_acc = 0d;
                //            var val_fpr = 0d;
                //            var val_fmetric = 0d;
                //            var val_npr = 0d;
                //            var val_prec = 0d;
                //            var val_rec = 0d;
                //            var val_spec = 0d;
                //            var val_mcc = 0d;
                //            if (tbuckets.Where(t => !double.IsNaN(t.AccuracyTarget)).Any())
                //            {
                //                val_acc = tbuckets.Where(t => !double.IsNaN(t.AccuracyTarget)).Average(t => t.AccuracyTarget);
                //                groupingBoxPlotSeries_acc.Items.Add(CreateBoxplot(tbuckets.Where(t => !double.IsNaN(t.AccuracyTarget)).Select(t => t.AccuracyTarget).ToList(), j * BucketGranularity));
                //            }
                //            if (tbuckets.Where(t => !double.IsNaN(t.FalsePositiveRateTarget)).Any())
                //            {
                //                val_fpr = tbuckets.Where(t => !double.IsNaN(t.FalsePositiveRateTarget)).Average(t => t.FalsePositiveRateTarget);
                //                groupingBoxPlotSeries_fpr.Items.Add(CreateBoxplot(tbuckets.Where(t => !double.IsNaN(t.FalsePositiveRateTarget)).Select(t => t.FalsePositiveRateTarget).ToList(), j * BucketGranularity));
                //            }
                //            if (tbuckets.Where(t => !double.IsNaN(t.FMeasureTarget)).Any())
                //            {
                //                val_fmetric = tbuckets.Where(t => !double.IsNaN(t.FMeasureTarget)).Average(t => t.FMeasureTarget);
                //                groupingBoxPlotSeries_fmetric.Items.Add(CreateBoxplot(tbuckets.Where(t => !double.IsNaN(t.FMeasureTarget)).Select(t => t.FMeasureTarget).ToList(), j * BucketGranularity));
                //            }
                //            if (tbuckets.Where(t => !double.IsNaN(t.NegativePredictedValueTarget)).Any())
                //            {
                //                val_npr = tbuckets.Where(t => !double.IsNaN(t.NegativePredictedValueTarget)).Average(t => t.NegativePredictedValueTarget);
                //                groupingBoxPlotSeries_npr.Items.Add(CreateBoxplot(tbuckets.Where(t => !double.IsNaN(t.NegativePredictedValueTarget)).Select(t => t.NegativePredictedValueTarget).ToList(), j * BucketGranularity));
                //            }
                //            if (tbuckets.Where(t => !double.IsNaN(t.PrecisionTarget)).Any())
                //            {
                //                val_prec = tbuckets.Where(t => !double.IsNaN(t.PrecisionTarget)).Average(t => t.PrecisionTarget);
                //                groupingBoxPlotSeries_prec.Items.Add(CreateBoxplot(tbuckets.Where(t => !double.IsNaN(t.PrecisionTarget)).Select(t => t.PrecisionTarget).ToList(), j * BucketGranularity));
                //            }
                //            if (tbuckets.Where(t => !double.IsNaN(t.RecallTarget)).Any())
                //            {
                //                val_rec = tbuckets.Where(t => !double.IsNaN(t.RecallTarget)).Average(t => t.RecallTarget);
                //                groupingBoxPlotSeries_rec.Items.Add(CreateBoxplot(tbuckets.Where(t => !double.IsNaN(t.RecallTarget)).Select(t => t.RecallTarget).ToList(), j * BucketGranularity));
                //            }
                //            if (tbuckets.Where(t => !double.IsNaN(t.SpecificityTarget)).Any())
                //            {
                //                val_spec = tbuckets.Where(t => !double.IsNaN(t.SpecificityTarget)).Average(t => t.SpecificityTarget);
                //                groupingBoxPlotSeries_spec.Items.Add(CreateBoxplot(tbuckets.Where(t => !double.IsNaN(t.SpecificityTarget)).Select(t => t.SpecificityTarget).ToList(), j * BucketGranularity));
                //            }
                //            if (tbuckets.Where(t => !double.IsNaN(t.MCC_Target)).Any())
                //            {
                //                val_mcc = tbuckets.Where(t => !double.IsNaN(t.MCC_Target)).Average(t => t.MCC_Target);
                //                groupingBoxPlotSeries_mcc.Items.Add(CreateBoxplot(tbuckets.Where(t => !double.IsNaN(t.MCC_Target)).Select(t => t.MCC_Target).ToList(), j * BucketGranularity));
                //            }
                //            groupingSeries_acc.Points.Add(new DataPoint(j * BucketGranularity, val_acc));
                //            groupingSeries_fpr.Points.Add(new DataPoint(j * BucketGranularity, val_fpr));
                //            groupingSeries_fmetric.Points.Add(new DataPoint(j * BucketGranularity, val_fmetric));
                //            groupingSeries_npr.Points.Add(new DataPoint(j * BucketGranularity, val_npr));
                //            groupingSeries_prec.Points.Add(new DataPoint(j * BucketGranularity, val_prec));
                //            groupingSeries_rec.Points.Add(new DataPoint(j * BucketGranularity, val_rec));
                //            groupingSeries_spec.Points.Add(new DataPoint(j * BucketGranularity, val_spec));
                //            groupingSeries_mcc.Points.Add(new DataPoint(j * BucketGranularity, val_mcc));
                //            grouping0line_acc.Add(val_acc.ToString());
                //            grouping0line_fpr.Add(val_fpr.ToString());
                //            grouping0line_fmetric.Add(val_fmetric.ToString());
                //            grouping0line_npr.Add(val_npr.ToString());
                //            grouping0line_prec.Add(val_prec.ToString());
                //            grouping0line_rec.Add(val_rec.ToString());
                //            grouping0line_spec.Add(val_spec.ToString());
                //            grouping0line_mcc.Add(val_mcc.ToString());
                //        }
                //        model_groupings0accuracy.Series.Add(groupingSeries_acc);
                //        model_groupings0falsepositives.Series.Add(groupingSeries_fpr);
                //        model_groupings0fmetric.Series.Add(groupingSeries_fmetric);
                //        model_groupings0negativepredictions.Series.Add(groupingSeries_npr);
                //        model_groupings0precision.Series.Add(groupingSeries_prec);
                //        model_groupings0recall.Series.Add(groupingSeries_rec);
                //        model_groupings0speceficity.Series.Add(groupingSeries_spec);
                //        model_groupings0mcc.Series.Add(groupingSeries_mcc);
                //        grouping0acc_out.Add(String.Join(",", grouping0line_acc));
                //        grouping0fpr_out.Add(String.Join(",", grouping0line_fpr));
                //        grouping0fmetric_out.Add(String.Join(",", grouping0line_fmetric));
                //        grouping0npr_out.Add(String.Join(",", grouping0line_npr));
                //        grouping0prec_out.Add(String.Join(",", grouping0line_prec));
                //        grouping0rec_out.Add(String.Join(",", grouping0line_rec));
                //        grouping0spec_out.Add(String.Join(",", grouping0line_spec));
                //        grouping0mcc_out.Add(String.Join(",", grouping0line_mcc));
                //        using (var filestream = new FileStream($"{OutFolder.FullName}\\grouping{group}_boxplot_{parameters[i]}_acc.pdf", FileMode.OpenOrCreate))
                //        {
                //            OxyPlot.PdfExporter.Export(groupingboxplotmodel_acc, filestream, PlotModelWidth, PlotModelHeight);
                //            filestream.Close();
                //        }
                //        using (var filestream = new FileStream($"{OutFolder.FullName}\\grouping{group}_boxplot_{parameters[i]}_fpr.pdf", FileMode.OpenOrCreate))
                //        {
                //            OxyPlot.PdfExporter.Export(groupingboxplotmodel_fpr, filestream, PlotModelWidth, PlotModelHeight);
                //            filestream.Close();
                //        }
                //        using (var filestream = new FileStream($"{OutFolder.FullName}\\grouping{group}_boxplot_{parameters[i]}_fmetric.pdf", FileMode.OpenOrCreate))
                //        {
                //            OxyPlot.PdfExporter.Export(groupingboxplotmodel_fmetric, filestream, PlotModelWidth, PlotModelHeight);
                //            filestream.Close();
                //        }
                //        using (var filestream = new FileStream($"{OutFolder.FullName}\\grouping{group}_boxplot_{parameters[i]}_npr.pdf", FileMode.OpenOrCreate))
                //        {
                //            OxyPlot.PdfExporter.Export(groupingboxplotmodel_npr, filestream, PlotModelWidth, PlotModelHeight);
                //            filestream.Close();
                //        }
                //        using (var filestream = new FileStream($"{OutFolder.FullName}\\grouping{group}_boxplot_{parameters[i]}_prec.pdf", FileMode.OpenOrCreate))
                //        {
                //            OxyPlot.PdfExporter.Export(groupingboxplotmodel_prec, filestream, PlotModelWidth, PlotModelHeight);
                //            filestream.Close();
                //        }
                //        using (var filestream = new FileStream($"{OutFolder.FullName}\\grouping{group}_boxplot_{parameters[i]}_rec.pdf", FileMode.OpenOrCreate))
                //        {
                //            OxyPlot.PdfExporter.Export(groupingboxplotmodel_rec, filestream, PlotModelWidth, PlotModelHeight);
                //            filestream.Close();
                //        }
                //        using (var filestream = new FileStream($"{OutFolder.FullName}\\grouping{group}_boxplot_{parameters[i]}_spec.pdf", FileMode.OpenOrCreate))
                //        {
                //            OxyPlot.PdfExporter.Export(groupingboxplotmodel_spec, filestream, PlotModelWidth, PlotModelHeight);
                //            filestream.Close();
                //        }
                //        using (var filestream = new FileStream($"{OutFolder.FullName}\\grouping{group}_boxplot_{parameters[i]}_mcc.pdf", FileMode.OpenOrCreate))
                //        {
                //            OxyPlot.PdfExporter.Export(groupingboxplotmodel_mcc, filestream, PlotModelWidth, PlotModelHeight);
                //            filestream.Close();
                //        }

                //    }
                //    using (var filestream = new FileStream($"{OutFolder.FullName}\\grouping{group}_acc.pdf", FileMode.OpenOrCreate))
                //    {
                //        OxyPlot.PdfExporter.Export(model_groupings0accuracy, filestream, PlotModelWidth, PlotModelHeight);
                //        filestream.Close();
                //    }
                //    File.WriteAllLines($"{OutFolder.FullName}\\grouping{group}_acc.csv", grouping0acc_out);

                //    using (var filestream = new FileStream($"{OutFolder.FullName}\\grouping{group}_fpr.pdf", FileMode.OpenOrCreate))
                //    {
                //        OxyPlot.PdfExporter.Export(model_groupings0falsepositives, filestream, PlotModelWidth, PlotModelHeight);
                //        filestream.Close();
                //    }
                //    File.WriteAllLines($"{OutFolder.FullName}\\grouping{group}_fpr.csv", grouping0fpr_out);

                //    using (var filestream = new FileStream($"{OutFolder.FullName}\\grouping{group}_fmetric.pdf", FileMode.OpenOrCreate))
                //    {
                //        OxyPlot.PdfExporter.Export(model_groupings0fmetric, filestream, PlotModelWidth, PlotModelHeight);
                //        filestream.Close();
                //    }
                //    File.WriteAllLines($"{OutFolder.FullName}\\grouping{group}_fmetric.csv", grouping0fmetric_out);

                //    using (var filestream = new FileStream($"{OutFolder.FullName}\\grouping{group}_npr.pdf", FileMode.OpenOrCreate))
                //    {
                //        OxyPlot.PdfExporter.Export(model_groupings0negativepredictions, filestream, PlotModelWidth, PlotModelHeight);
                //        filestream.Close();
                //    }
                //    File.WriteAllLines($"{OutFolder.FullName}\\grouping{group}_npr.csv", grouping0npr_out);

                //    using (var filestream = new FileStream($"{OutFolder.FullName}\\grouping{group}_prec.pdf", FileMode.OpenOrCreate))
                //    {
                //        OxyPlot.PdfExporter.Export(model_groupings0precision, filestream, PlotModelWidth, PlotModelHeight);
                //        filestream.Close();
                //    }
                //    File.WriteAllLines($"{OutFolder.FullName}\\grouping{group}_prec.csv", grouping0prec_out);

                //    using (var filestream = new FileStream($"{OutFolder.FullName}\\grouping{group}_rec.pdf", FileMode.OpenOrCreate))
                //    {
                //        OxyPlot.PdfExporter.Export(model_groupings0recall, filestream, PlotModelWidth, PlotModelHeight);
                //        filestream.Close();
                //    }
                //    File.WriteAllLines($"{OutFolder.FullName}\\grouping{group}_rec.csv", grouping0rec_out);

                //    using (var filestream = new FileStream($"{OutFolder.FullName}\\grouping{group}_spec.pdf", FileMode.OpenOrCreate))
                //    {
                //        OxyPlot.PdfExporter.Export(model_groupings0speceficity, filestream, PlotModelWidth, PlotModelHeight);
                //        filestream.Close();
                //    }
                //    File.WriteAllLines($"{OutFolder.FullName}\\grouping{group}_spec.csv", grouping0spec_out);

                //    using (var filestream = new FileStream($"{OutFolder.FullName}\\grouping{group}_mcc.pdf", FileMode.OpenOrCreate))
                //    {
                //        OxyPlot.PdfExporter.Export(model_groupings0mcc, filestream, PlotModelWidth, PlotModelHeight);
                //        filestream.Close();
                //    }
                //    File.WriteAllLines($"{OutFolder.FullName}\\grouping{group}_mcc.csv", grouping0mcc_out);

                //}
                #endregion

                #region statistics
                ////(anova + wilcox paired)
                //List<String> anovaBlockOutlines = new List<String>();
                //List<String> wilcoxBlockOutlines = new List<String>();
                //List<String> ttestBlockOutlines = new List<String>();
                //List<String> anovaRowOutlines = new List<String>();
                //List<String> wilcoxRowOutlines = new List<String>();
                //List<String> ttestRowOutlines = new List<String>();
                //parameters = allParameters[0].Distinct().ToList();
                //parameters.Sort();

                //List<Task<Tuple<List<String>, List<String>, List<String>>>> taskList = new List<Task<Tuple<List<String>, List<String>, List<String>>>>();
                //for (int i = 0; i * BucketGranularity < 1; i++)
                //{
                //    var buckets = allBuckets.Where(t => t.BucketLevel == i).ToList();
                //    var level = ((double)i * BucketGranularity).ToString();
                //    taskList.Add(Task.Run(() => FillStatisticData(level, parameters, buckets)));
                //}

                ////all, <50%, >=50%
                //taskList.Add(Task.Run(() => FillStatisticData("all", parameters, allBuckets)));
                //taskList.Add(Task.Run(() => FillStatisticData("<50%", parameters, allBuckets.Where(t => t.BucketLevel * BucketGranularity < 0.5d).ToList())));
                //taskList.Add(Task.Run(() => FillStatisticData(">=50%", parameters, allBuckets.Where(t => t.BucketLevel * BucketGranularity >= 0.5d).ToList())));

                ////x vs y blocks
                //for (int i = 0; i < taskList.Count; i++)
                //{
                //    taskList[i].Wait();
                //    var result = taskList[i].Result;
                //    anovaBlockOutlines.AddRange(result.Item1);
                //    wilcoxBlockOutlines.AddRange(result.Item2);
                //    ttestBlockOutlines.AddRange(result.Item3);
                //}

                ////rows
                //foreach (var parameter in new List<String>() { "s2s", "nopath", "noplanned", "rgb" })//test parameter vs opposite
                //{
                //    //header 
                //    var line = $"{parameter},null";
                //    for (int j = 0; j * BucketGranularity < 1; j++)
                //        line += $",{(j * BucketGranularity).ToString()}";
                //    anovaRowOutlines.Add(line);
                //    wilcoxRowOutlines.Add(line);
                //    ttestRowOutlines.Add(line);

                //    //get unique p0s
                //    var p01 = parameters.Where(t => t.Contains(parameter)).Distinct();
                //    var p02 = parameters.Where(t => !t.Contains(parameter)).Distinct();

                //    //iterate through p0s (generating combinations for parameter vs !parameter)
                //    foreach (var p01param in p01)
                //    {
                //        foreach (var p02param in p02)
                //        {
                //            var anovaoutline = new List<String>(); anovaoutline.Add(p01param); anovaoutline.Add(p02param);
                //            var wilcoxoutline = new List<String>(); wilcoxoutline.Add(p01param); wilcoxoutline.Add(p02param);
                //            var ttestoutline = new List<String>(); ttestoutline.Add(p01param); ttestoutline.Add(p02param);

                //            //go through each bucket
                //            for (int i = 0; i * BucketGranularity < 1; i++)
                //            {
                //                //get data
                //                var data1 = allBuckets.Where(t => t.Parameters.First() == p01param && t.BucketLevel == i);
                //                var data2 = allBuckets.Where(t => t.Parameters.First() == p02param && t.BucketLevel == i);

                //                //write data
                //                //get data values
                //                List<double> res1 = data1.Where(t => !double.IsNaN(t.MCC_Target)).Select(t => t.MCC_Target).ToList();
                //                List<double> res2 = data2.Where(t => !double.IsNaN(t.MCC_Target)).Select(t => t.MCC_Target).ToList();

                //                //get p-value
                //                anovaoutline.Add(Statistics.CalculateP(res1, res2).ToString());
                //                wilcoxoutline.Add(new Accord.Statistics.Testing.MannWhitneyWilcoxonTest(res1.ToArray(), res2.ToArray(), exact: false).PValue.ToString());
                //                ttestoutline.Add(new Accord.Statistics.Testing.TwoSampleTTest(res1.ToArray(), res2.ToArray(), false).PValue.ToString());
                //            }
                //            //edge cases //all
                //            var dataa1 = allBuckets.Where(t => t.Parameters.First() == p01param).Where(t => !double.IsNaN(t.MCC_Target)).Select(t => t.MCC_Target).ToList();
                //            var dataa2 = allBuckets.Where(t => t.Parameters.First() == p02param).Where(t => !double.IsNaN(t.MCC_Target)).Select(t => t.MCC_Target).ToList();
                //            anovaoutline.Add(Statistics.CalculateP(dataa1, dataa2).ToString());
                //            wilcoxoutline.Add(new Accord.Statistics.Testing.MannWhitneyWilcoxonTest(dataa1.ToArray(), dataa2.ToArray(), exact: false).PValue.ToString());
                //            ttestoutline.Add(new Accord.Statistics.Testing.TwoSampleTTest(dataa1.ToArray(), dataa2.ToArray(), false).PValue.ToString());

                //            anovaRowOutlines.Add(String.Join(",", anovaoutline));
                //            wilcoxRowOutlines.Add(String.Join(",", wilcoxoutline));
                //            ttestRowOutlines.Add(String.Join(",", ttestoutline));
                //        }
                //    }
                //}

                //File.WriteAllLines($"{OutFolder.FullName}\\pvalues_anova_block.csv", anovaBlockOutlines);
                //File.WriteAllLines($"{OutFolder.FullName}\\pvalues_wilcox_block.csv", wilcoxBlockOutlines);
                //File.WriteAllLines($"{OutFolder.FullName}\\pvalues_ttest_block.csv", ttestBlockOutlines);
                //File.WriteAllLines($"{OutFolder.FullName}\\pvalues_anova_rows.csv", anovaRowOutlines);
                //File.WriteAllLines($"{OutFolder.FullName}\\pvalues_wilcox_rows.csv", wilcoxRowOutlines);
                //File.WriteAllLines($"{OutFolder.FullName}\\pvalues_ttest_rows.csv", ttestRowOutlines);
                #endregion statistics                

                Logger.AddLogMessage($"finished folder {folder}", verbose);
            }

            //out the stuff in the logger
            Logger.WriteErrorLogToFilesystem("errorlog.txt");
            Logger.WriteLogToFilesystem("log.txt");
            Console.WriteLine("Done");
            Console.ReadKey();
        }

        public static void RunPerFileWorkload(List<Line> output, ref ConcurrentBag<List<Line>> bagLines, List<Bucket> BucketList, ref List<Bucket> allBuckets, ref List<List<Bucket>> ensembleBuckets, List<String> Parameters, String file,
            OxyPlot.PlotModel model_glob_precision_target,
            OxyPlot.PlotModel model_glob_recall_target,
            OxyPlot.PlotModel model_glob_speceficity_target,
            OxyPlot.PlotModel model_glob_falsepositives_target,
            OxyPlot.PlotModel model_glob_negativepredictions_target,
            OxyPlot.PlotModel model_glob_accuracy_target,
            OxyPlot.PlotModel model_glob_mcc_target,
            OxyPlot.PlotModel model_glob_fmetric_target,
            ref int counter,
            String folder, 
            String type
            )
        {
            //writelines
            List<String> exportrows = new List<string>();
            exportrows.Add("sequenceid," +
                           "sequencelength," +
                           "prefix," +
                           "completion," +
                           "prediction," +
                           "gt_prediction," +
                           "gt_planned," +
                           "gt_instance," +
                           "prefix_activities," +
                           "suffix_activities," +
                           "accuracy_sumprevious," +
                           "violation_effective," +
                           "violation_predicted," +
                           "violation_string," +
                           "deviation_abs," +
                           "reliability," +
                           "bucket_level");
            foreach (var line in output)
            {
                exportrows.Add($"{line.SequenceID}," +                               
                               $"{line.SequenceLength}," +
                               $"{line.Prefix}," +
                               $"{line.Completion}," +
                               $"{line.TargetValue}," +
                               $"{line.GT_TargetValue}," +
                               $"{line.GT_Planned}," +
                               $"{line.GT_InstanceID}," +
                               $"{line.PrefixActivities}," +
                               $"{line.SuffixActivities}," +
                               $"{line.Accuracy}," +
                               $"{line.Violation_Effective}," +
                               $"{line.Violation_Predicted}," +
                               $"{line.Violation_String}," +
                               $"{line.DeviationFromTarget}," +
                               $"{line.Reliability},"+
                               $"{line.Bucket.BucketLevel}");
            }

            //add buckets
            exportrows.Add("bucket_level," +
                           "Count," +
                           "Count(TP)," +
                           "Count(FP)," +
                           "Count(TN)," +
                           "Count(FN)," +
                           "Median," +
                           "Deviations," +
                           "MSE," +
                           "RMSE," +
                           "MAE," +
                           "RSE," +
                           "RRSE," +
                           "RAE," +
                           "Precision," +
                           "Recall," +
                           "Specificity," +
                           "FalsePositiveRate," +
                           "NegativePredictedValue," +
                           "Accuracy," +
                           "F-Measure," +
                           "MCC," +
                           "AvgReliability"
            );
            foreach (var bucket in BucketList)
                if (bucket.ViolationStrings.Any())
                    exportrows.Add($"{bucket.BucketLevel * BucketGranularity}," +
                                   $"{bucket.ViolationStrings.Count}," +
                                   $"{bucket.TPcount}," +
                                   $"{bucket.FPcount}," +
                                   $"{bucket.TNcount}," +
                                   $"{bucket.FNcount}," +
                                   $"{bucket.PredictionMedian}," +
                                   $"{bucket.DeviationsAbsolute.Average()}," +
                                   $"{bucket.MSE}," +
                                   $"{bucket.RMSE}," +
                                   $"{bucket.MAE}," +
                                   $"{bucket.RSE}," +
                                   $"{bucket.RRSE}," +
                                   $"{bucket.RAE}," +
                                   $"{bucket.Precision}," +
                                   $"{bucket.Recall}," +
                                   $"{bucket.Specificity}," +
                                   $"{bucket.FalsePositiveRate}," +
                                   $"{bucket.NegativePredictedValue}," +
                                   $"{bucket.Accuracy}," +
                                   $"{bucket.FMeasure}," +
                                   $"{bucket.MCC}," +
                                   $"{bucket.AvgReliability}"
                    );
            exportrows.Add($"Total," +
                           $"{BucketList.Sum(t => t.ViolationStrings.Count)}," +
                           $"{BucketList.Sum(t => t.TPcount)}," +
                           $"{BucketList.Sum(t => t.FPcount)}," +
                           $"{BucketList.Sum(t => t.TNcount)}," +
                           $"{BucketList.Sum(t => t.FNcount)}," +
                           $"{BucketList.Where(t => t.ViolationStrings.Count > 0).Sum(t => t.PredictionMedian) / (double)BucketList.Count(t => t.ViolationStrings.Any())}," +
                           $"{BucketList.Where(t => t.ViolationStrings.Count > 0).Sum(t => t.DeviationsAbsolute.Average()) / (double)BucketList.Count(t => t.ViolationStrings.Any())}," +
                           $"{(BucketList.Any(t => t.ViolationStrings.Count > 0) ? BucketList.Where(t => t.ViolationStrings.Count > 0).Average(t => t.MSE) : double.NaN)}," +
                           $"{(BucketList.Any(t => t.ViolationStrings.Count > 0) ? BucketList.Where(t => t.ViolationStrings.Count > 0).Average(t => t.RMSE) : double.NaN)}," +
                           $"{(BucketList.Any(t => t.ViolationStrings.Count > 0) ? BucketList.Where(t => t.ViolationStrings.Count > 0).Average(t => t.MAE) : double.NaN)}," +
                           $"{(BucketList.Any(t => t.ViolationStrings.Count > 0) ? BucketList.Where(t => t.ViolationStrings.Count > 0).Average(t => t.RSE) : double.NaN)}," +
                           $"{(BucketList.Any(t => t.ViolationStrings.Count > 0) ? BucketList.Where(t => t.ViolationStrings.Count > 0).Average(t => t.RRSE) : double.NaN)}," +
                           $"{(BucketList.Any(t => t.ViolationStrings.Count > 0) ? BucketList.Where(t => t.ViolationStrings.Count > 0).Average(t => t.RAE) : double.NaN)}," +
                           $"{(BucketList.Any(t => t.ViolationStrings.Count > 0) ? BucketList.Where(t => t.ViolationStrings.Count > 0).Average(t => t.Precision) : double.NaN)}," +
                           $"{(BucketList.Any(t => t.ViolationStrings.Count > 0) ? BucketList.Where(t => t.ViolationStrings.Count > 0).Average(t => t.Recall) : double.NaN)}," +
                           $"{(BucketList.Any(t => t.ViolationStrings.Count > 0) ? BucketList.Where(t => t.ViolationStrings.Count > 0).Average(t => t.Specificity) : double.NaN)}," +
                           $"{(BucketList.Any(t => t.ViolationStrings.Count > 0) ? BucketList.Where(t => t.ViolationStrings.Count > 0).Average(t => t.FalsePositiveRate) : double.NaN)}," +
                           $"{(BucketList.Any(t => t.ViolationStrings.Count > 0) ? BucketList.Where(t => t.ViolationStrings.Count > 0).Average(t => t.NegativePredictedValue) : double.NaN)}," +
                           $"{(BucketList.Any(t => t.ViolationStrings.Count > 0) ? BucketList.Where(t => t.ViolationStrings.Count > 0).Average(t => t.Accuracy) : double.NaN)}," +
                           $"{(BucketList.Any(t => t.ViolationStrings.Count > 0) ? BucketList.Where(t => t.ViolationStrings.Count > 0).Average(t => t.FMeasure) : double.NaN)}," +
                           $"{(BucketList.Any(t => t.ViolationStrings.Count > 0) ? BucketList.Where(t => t.ViolationStrings.Count > 0).Average(t => t.MCC) : double.NaN)}," +
                           $"{(BucketList.Any(t => t.ViolationStrings.Count > 0) ? BucketList.Where(t => t.ViolationStrings.Count > 0).Average(t => t.AvgReliability) : double.NaN)}"
            );

            ////export as csv to match LSTM input examples
            if (!String.IsNullOrWhiteSpace(file))
                Task.Run(() => File.WriteAllLines($"{file.Replace(".csv", "")}.edited.csv", exportrows));

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
                    bucket.Precision));
                recallSeries_target.Points.Add(new DataPoint(bucket.BucketLevel * BucketGranularity,
                    bucket.Recall));
                speceficitySeries_target.Points.Add(new DataPoint(bucket.BucketLevel * BucketGranularity,
                    bucket.Specificity));
                falsepositivesSeries_target.Points.Add(new DataPoint(bucket.BucketLevel * BucketGranularity,
                    bucket.FalsePositiveRate));
                negativepredictedSeries_target.Points.Add(new DataPoint(bucket.BucketLevel * BucketGranularity,
                    bucket.NegativePredictedValue));
                accuracySeries_target.Points.Add(new DataPoint(bucket.BucketLevel * BucketGranularity,
                    bucket.Accuracy));
                fmetricSeries_target.Points.Add(new DataPoint(bucket.BucketLevel * BucketGranularity,
                    bucket.FMeasure));
                MCCSeries_target.Points.Add(new DataPoint(bucket.BucketLevel * BucketGranularity, bucket.MCC));

                precisionSeries_target_global.Points.Add(new DataPoint(bucket.BucketLevel * BucketGranularity,
                    bucket.Precision));
                recallSeries_target_global.Points.Add(new DataPoint(bucket.BucketLevel * BucketGranularity,
                    bucket.Recall));
                speceficitySeries_target_global.Points.Add(new DataPoint(bucket.BucketLevel * BucketGranularity,
                    bucket.Specificity));
                falsepositivesSeries_target_global.Points.Add(new DataPoint(bucket.BucketLevel * BucketGranularity,
                    bucket.FalsePositiveRate));
                negativepredictedSeries_target_global.Points.Add(
                    new DataPoint(bucket.BucketLevel * BucketGranularity, bucket.NegativePredictedValue));
                accuracySeries_target_global.Points.Add(new DataPoint(bucket.BucketLevel * BucketGranularity,
                    bucket.Accuracy));
                fmetricSeries_target_global.Points.Add(new DataPoint(bucket.BucketLevel * BucketGranularity,
                    bucket.FMeasure));
                MCCSeries_target_global.Points.Add(new DataPoint(bucket.BucketLevel * BucketGranularity,
                    bucket.MCC));
            }

            model_target.Series.Add(precisionSeries_target);
            model_target.Series.Add(recallSeries_target);
            model_target.Series.Add(speceficitySeries_target);
            model_target.Series.Add(falsepositivesSeries_target);
            model_target.Series.Add(negativepredictedSeries_target);
            model_target.Series.Add(accuracySeries_target);
            model_target.Series.Add(fmetricSeries_target);
            model_target.Series.Add(MCCSeries_target);
            if (!String.IsNullOrWhiteSpace(file))
                Task.Run(() =>
                {
                    using (var filestream = new FileStream($"{file.Replace(".csv", "")}.plot_target.pdf",
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

            //add bucketlist to ensembleList
            ensembleBuckets.Add(BucketList);
            bagLines.Add(output);
            counter++;
            Logger.AddLogMessage($"[{type}] finished file {counter} in folder {folder} with {output.Count} lines", verbose);
        }

        public static List<Line> GetLinesFromData(String FullPathToFile, TextFieldParser parser, ref int rows, bool pPositiveIsViolation)
        {
            bool firstline = true;
            List<Line> output = new List<Line>();

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

                //extract target value (usually either bool or double)
                double target;
                if (!double.TryParse(fields[5], out target))
                    target = fields[5] == "True" ? 1 : 0;

                Line line = new Line()
                {
                    FullPathToFile = FullPathToFile,

                    SequenceID = int.Parse(fields[0]),
                    SequenceLength = int.Parse(fields[1]),
                    Prefix = int.Parse(fields[2]),
                    Completion = double.Parse(fields[3], CultureInfo.InvariantCulture),
                    TargetValue = double.Parse(fields[4], CultureInfo.InvariantCulture),
                    GT_TargetValue = target,
                    GT_Planned = double.Parse(fields[6]),
                    GT_InstanceID = fields[7],
                    PrefixActivities = fields[8],
                    SuffixActivities = fields[9]
                };

                //calculate accuracy values
                line.Violation_Effective = pPositiveIsViolation == (line.GT_TargetValue > line.GT_Planned);
                line.Violation_Predicted = pPositiveIsViolation == (line.TargetValue > line.GT_Planned);
                line.Accuracy = CalculateAccuracy(line.TargetValue, line.GT_TargetValue);
                output.Add(line);
            }
            return output;
        }

        public static List<Line> GetLinesFromEnsemble(Ensemble ensemble, bool IsBinaryPrediction, bool IsRGBencoding, double pReliabilityThreshold, bool pRemoveBelowReliabilityThreshold)
        {
            List<Line> output = new List<Line>();
            foreach (var enLine in ensemble.EnsembleLines)
            {
                if (!pRemoveBelowReliabilityThreshold || enLine.Reliability >= pReliabilityThreshold)
                {
                    var line = new Line()
                    {
                        FullPathToFile = "ensemble",
                        SequenceID = enLine.SequenceId,
                        SequenceLength = enLine.InstanceLength,
                        Prefix = enLine.Prefix,
                        TargetValue = enLine.MedianPrediction,
                        Completion = enLine.Completion,
                        GT_TargetValue = enLine.ActualValue,
                        GT_Planned = enLine.ActualPlanned,
                        GT_InstanceID = enLine.InstanceId,
                        PrefixActivities = enLine.PrefixActivities,
                        //PredictedActivities = fields[11],
                        SuffixActivities = enLine.SuffixActivities,

                        Violation_Effective = enLine.ActualViolation,
                        Violation_Predicted = enLine.PredictedViolation,

                        Reliability = enLine.Reliability
                    };

                    output.Add(line);
                }
            }

            return output;
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
            return new List<string>() { pParameterString, "100","0.1","20","1" };
            //return pParameterString.Split(' ').ToList();
        }

        public class Line
        {
            /// <summary>
            /// contains the full path to the file the line belongs to, for debugging purposes
            /// </summary>
            public String FullPathToFile { get; set; }

            public Bucket Bucket { get; set; }

            //input
            public int SequenceID { get; set; }
            public int SequenceLength { get; set; }
            public int Prefix { get; set; }
            public double TargetValue { get; set; }
            public double Completion { get; set; }
            public double GT_TargetValue { get; set; }
            public double GT_Planned { get; set; }
            public String GT_InstanceID { get; set; }
            public String PrefixActivities { get; set; }
            public String PredictedActivities { get; set; }
            public String SuffixActivities { get; set; }
            public bool Predicted_Violations { get; set; }
            public double? Reliability { get; set; }

            //output
            public double Accuracy { get; set; }
            public double DeviationFromTarget => TargetValue - GT_TargetValue;
            public bool Violation_Effective { get; set; }
            public bool Violation_Predicted { get; set; }
            public String Violation_String => CalculateViolationString(Violation_Effective, Violation_Predicted);

        }

        public class Bucket
        {
            public List<Line> Lines { get; set; }
            public int BucketLevel { get; set; }
            public List<String> Parameters { get; set; }

            //counts
            public int TPcount => ViolationStrings.Count(t => t == "TP");
            public int FPcount => ViolationStrings.Count(t => t == "FP");
            public int TNcount => ViolationStrings.Count(t => t == "TN");
            public int FNcount => ViolationStrings.Count(t => t == "FN");

            public List<Double> Prediction { get; set; }
            public List<String> ViolationStrings { get; set; }
            public List<Double> PredictionAccuracies { get; set; }
            public List<Double> DeviationsAbsolute { get; set; }
            //binary prediction
            public double Precision => NaNtoDefault((double)ViolationStrings.Count(t => t == "TP") / (double)ViolationStrings.Count(t => t == "TP" || t == "FP"),0);
            public double Recall => NaNtoDefault((double)ViolationStrings.Count(t => t == "TP") / (double)ViolationStrings.Count(t => t == "TP" || t == "FN"),0);
            public double Specificity => NaNtoDefault((double)ViolationStrings.Count(t => t == "TN") / (double)ViolationStrings.Count(t => t == "TN" || t == "FP"),0);
            public double FalsePositiveRate => NaNtoDefault((double)ViolationStrings.Count(t => t == "FP") / (double)ViolationStrings.Count(t => t == "FP" || t == "TN"),0);
            public double NegativePredictedValue => NaNtoDefault((double)ViolationStrings.Count(t => t == "TN") / (double)ViolationStrings.Count(t => t == "TN" || t == "TP"),0);
            public double Accuracy => NaNtoDefault((double)ViolationStrings.Count(t => t == "TN" || t == "TP") / (double)ViolationStrings.Count,0);
            public double FMeasure => NaNtoDefault(((1 + Math.Pow(FmetricBeta, 2)) * Precision * Recall) / ((Math.Pow(FmetricBeta, 2) * Precision) + Recall),0);
            public double MCC => NaNtoDefault((double)((TPcount * TNcount) - (FPcount * FNcount)) / Math.Sqrt((double)(TPcount + FPcount) * (TPcount + FNcount) * (TNcount + FPcount) * (TNcount + FNcount)),0);
            //regression prediction
            public double PredictionAccuraciesMedian => Median(PredictionAccuracies.ToArray());
            public double PredictionMedian => Median(Prediction.ToArray());

            //numeric metrics
            public double MSE => DeviationsAbsolute.Sum(t => Math.Pow(t, 2)) / DeviationsAbsolute.Count;
            public double RMSE => Math.Sqrt(DeviationsAbsolute.Sum(t => Math.Pow(t, 2)) / DeviationsAbsolute.Count);
            public double MAE => DeviationsAbsolute.Sum(t => Math.Abs(t)) / DeviationsAbsolute.Count;
            public double RSE => DeviationsAbsolute.Sum(t => Math.Pow(t, 2)) / (Prediction.Sum(t => Math.Pow(t - Prediction.Average(), 2)));
            public double RRSE => Math.Sqrt(DeviationsAbsolute.Sum(t => Math.Pow(t, 2)) / (Prediction.Sum(t => Math.Pow(t - Prediction.Average(), 2))));
            public double RAE => DeviationsAbsolute.Sum(t => Math.Abs(t)) / (Prediction.Sum(t => Math.Abs(t - Prediction.Average())));

            public double AvgReliability => Lines.Any(t => t.Reliability != null) ? Lines.Average(t => t.Reliability).Value : -1d;
        }

        public static double NaNtoDefault(double pValue, double pDefault)
        {
            if (double.IsNaN(pValue))
                return pDefault;
            return pValue;
        }

        public static double Median(double[] xs)
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
            return true;
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

        static Tuple<List<String>, List<String>, List<String>> FillStatisticData(String level, List<String> parameters, List<Bucket> buckets)
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
                    List<double> data1 = parameterbuckets1.Where(t => !double.IsNaN(t.MCC)).Select(t => t.MCC).ToList();
                    List<double> data2 = parameterbuckets2.Where(t => !double.IsNaN(t.MCC)).Select(t => t.MCC).ToList();

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
                        wilcoxpvalues.Add(new Accord.Statistics.Testing.MannWhitneyWilcoxonTest(data1.ToArray(), data2.ToArray(), exact: false).PValue);
                        ttestpvalues.Add(new Accord.Statistics.Testing.TwoSampleTTest(data1.ToArray(), data2.ToArray(), false).PValue);
                    }

                }
                anovaOutlines.Add(parameter1 + "," + String.Join(",", anovapvalues));
                wilcoxOutlines.Add(parameter1 + "," + String.Join(",", wilcoxpvalues));
                ttestOutlines.Add(parameter1 + "," + String.Join(",", ttestpvalues));
            }

            return Tuple.Create(anovaOutlines, wilcoxOutlines, ttestOutlines);
        }

        /// <summary>
        /// checks if the value is nan, returns a default value if it does
        /// </summary>
        /// <param name="pValue"></param>
        /// <returns></returns>
        static double CheckNaN(double pValue, double pDefault)
        {
            return double.IsNaN(pValue) ? pDefault : pValue;
        }
     }
}
