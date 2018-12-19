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
        public static List<Bucket> CreateBuckets(double BucketGranularity, List<String> Parameters, int BucketingType, List<Line> output)
        {
            //create buckets
            List<Bucket> BucketList = new List<Bucket>();
            for (int i = 0; i * BucketGranularity <= 1; i++)
                BucketList.Add(new Bucket()
                {
                    BucketLevel = i,
                    Parameters = Parameters,
                    Lines = new List<Line>(),
                    Prediction = new List<double>(),
                    ViolationStrings = new List<string>(),
                    PredictionAccuracies = new List<double>(),
                    DeviationsAbsolute = new List<double>()
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
                            BucketList[i].Prediction.Add(line.TargetValue);
                            BucketList[i].ViolationStrings.Add(line.Violation_String);
                            BucketList[i].PredictionAccuracies.Add(line.Accuracy);
                            BucketList[i].DeviationsAbsolute.Add(line.DeviationFromTarget);
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
                    indexout = listout.IndexOf("13");

                    if (indexout == -1)
                        throw new Exception("indexing failed");

                    //case prediction = 13 (50% marker)
                    if (indexout == line.Prefix)
                    {
                        line.Completion = 0.5d;
                        midbucket.Lines.Add(line);
                        line.Bucket = midbucket;
                        midbucket.Prediction.Add(line.TargetValue);
                        midbucket.ViolationStrings.Add(line.Violation_String);
                        midbucket.PredictionAccuracies.Add(line.Accuracy);
                        midbucket.DeviationsAbsolute.Add(line.DeviationFromTarget);
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
                                BucketList[i].Prediction.Add(line.TargetValue);
                                BucketList[i].ViolationStrings.Add(line.Violation_String);
                                BucketList[i].PredictionAccuracies.Add(line.Accuracy);
                                BucketList[i].DeviationsAbsolute.Add(line.DeviationFromTarget);
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
                                BucketList[i].Prediction.Add(line.TargetValue);
                                BucketList[i].ViolationStrings.Add(line.Violation_String);
                                BucketList[i].PredictionAccuracies.Add(line.Accuracy);
                                BucketList[i].DeviationsAbsolute.Add(line.DeviationFromTarget);
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
