using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Analyser.Program;

namespace Analyser
{
    public class Ensemble
    {
        public int EnsembleSize { get; }
        internal double reliabilityThreshold;

        public Ensemble(List<List<Line>> pAllLines, double pReliabilityThreshold)
        {
            reliabilityThreshold = pReliabilityThreshold;
            EnsembleSize = pAllLines.Count;
            for (int i = 0; i < pAllLines[0].Count; i++)
            {
                // get all line objects from each model with process instance id i
                List<Line> processInstanceLines = new List<Line>();
                foreach (var model in pAllLines)
                    processInstanceLines.Add(model[i]);

                EnsembleLines.Add(new EnsembleLine(processInstanceLines, this));
            }
        }

        public List<EnsembleLine> EnsembleLines { get; } = new List<EnsembleLine>();

        public double MCC => (double)((TPcount * TNcount) - (FPcount * FNcount)) / Math.Sqrt((double)(TPcount + FPcount) * (double)(TPcount + FNcount) * (double)(TNcount + FPcount) * (double)(TNcount + FNcount));

        public double Reliability => EnsembleLines.Any() ? EnsembleLines.Average(t => t.Reliability) : 0;

        public double GetReliabilityForBucket(double pBucketLevel, double pBucketGranularity)
        {
            var lines = EnsembleLines.Where(line => line.Completion >= pBucketLevel * pBucketGranularity && line.Completion < (pBucketLevel + 1) * pBucketGranularity);
            if (lines.Any())
                return lines.Average(t => t.Reliability);
            else
                return 0;
        }

        public List<double> GetReliabilitiesForBucket(double pBucketLevel, double pBucketGranularity)
        {
            return EnsembleLines.Where(line => line.Completion >= pBucketLevel * pBucketGranularity && line.Completion < (pBucketLevel + 1) * pBucketGranularity).Select(t => t.Reliability).ToList();
        }

        int TPcount => EnsembleLines.Count(t => t.ActualViolation && t.PredictedViolation);
        int FPcount => EnsembleLines.Count(t => !t.ActualViolation && t.PredictedViolation);
        int FNcount => EnsembleLines.Count(t => t.ActualViolation && !t.PredictedViolation);
        int TNcount => EnsembleLines.Count(t => !t.ActualViolation && !t.PredictedViolation);

        public List<String> ExportToCsv()
        {
            List<String> outlist = new List<string>();
            outlist.Add("id,prefix,length,violation,gt_violation,median,average,gt_value,reliability"); //header
            foreach (var ensembleLine in EnsembleLines)
            {
                outlist.Add($"{ensembleLine.InstanceId}," +
                    $"{ensembleLine.Prefix}," +
                    $"{ensembleLine.InstanceLength}," +
                    $"{ensembleLine.PredictedViolation}," +
                    $"{ensembleLine.ActualViolation}," +
                    $"{ensembleLine.MedianPrediction}," +
                    $"{ensembleLine.AveragePrediction}," +
                    $"{ensembleLine.ActualValue}," +
                    $"{ensembleLine.Reliability},");
            }
            return outlist;
        }
    }

    public class EnsembleLine
    {
        double reliabilityThreshold;

        public EnsembleLine(List<Line> pLines, Ensemble pEnsemble)
        {
            //set vlaues
            reliabilityThreshold = pEnsemble.reliabilityThreshold;

            //create single votes
            foreach (var line in pLines)
                EnsembleVotes.Add(new EnsembleVote(line.Violation_Predicted, line.TargetValue));

            //set ground truth values; should be equal across all input lines
            SequenceId = pLines.First().SequenceID;
            ActualViolation = pLines.First().Violation_Effective;
            ActualValue = pLines.First().GT_TargetValue;
            Prefix = pLines.First().Prefix;
            InstanceLength = pLines.First().SequenceLength;
            InstanceId = pLines.First().GT_InstanceID;
            ActualPlanned = pLines.First().GT_Planned;
            PrefixActivities = pLines.First().PrefixActivities;
            SuffixActivities = pLines.First().SuffixActivities;
            Completion = pLines.First().Completion;
        }

        public int SequenceId { get; set; }
        List<EnsembleVote> EnsembleVotes { get; } = new List<EnsembleVote>();
        public bool ActualViolation { get; }
        public double ActualValue { get; }
        public double ActualPlanned { get; }
        public double Completion { get; }
        public int Prefix { get; }
        public int InstanceLength { get; }
        public String InstanceId { get; }
        public String PrefixActivities { get; }
        public String SuffixActivities { get; }

        public bool PredictedViolation => ((double)EnsembleVotes.Count(t => t.Violation == true) / (double)EnsembleVotes.Count) >= reliabilityThreshold; //>= 0.5;
        public double MedianPrediction => Program.Median(EnsembleVotes.Select(t => t.Prediction).ToArray());
        public double AveragePrediction => EnsembleVotes.Average(t => t.Prediction);
        public double Reliability => ((double)EnsembleVotes.Count(t => t.Violation == true) / (double)EnsembleVotes.Count) >= 0.5 ?
            ((double)EnsembleVotes.Count(t => t.Violation == true) / (double)EnsembleVotes.Count) :
            ((double)EnsembleVotes.Count(t => t.Violation == false) / (double)EnsembleVotes.Count);
    }

    class EnsembleVote
    {
        public EnsembleVote(bool pViolation, double pPrediction)
        {
            Violation = pViolation;
            Prediction = pPrediction;
        }

        internal bool Violation { get; }
        internal double Prediction { get; }
    }
}
