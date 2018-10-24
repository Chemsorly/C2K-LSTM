using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Analyser.Program;

namespace Analyser
{
    public static class Bucketing
    {
        public static List<Bucket> CreateBuckets(double BucketGranularity, List<String> Parameters, TargetData TargetData, int BucketingType, List<Line> output, bool IsRGBencoding, bool IsNopathEncoding)
        {
            //create buckets
            List<Bucket> BucketList = new List<Bucket>();
            for (int i = 0; i * BucketGranularity <= 1; i++)
                BucketList.Add(new Bucket()
                {
                    BucketLevel = i,
                    Parameters = Parameters,
                    Target = TargetData,
                    Lines = new List<Line>(),
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
                            BucketList[i].Lines.Add(line);
                            line.Bucket = BucketList[i];
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
                        midbucket.Lines.Add(line);
                        line.Bucket = midbucket;
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
                                BucketList[i].Lines.Add(line);
                                line.Bucket = BucketList[i];
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
                                BucketList[i].Lines.Add(line);
                                line.Bucket = BucketList[i];
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

            return BucketList;
        }
    }
}
