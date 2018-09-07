using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyser
{
    public static class Folderlist
    {
        public static List<String> GetFolderlist()
        {
            return new List<String>()
            {
                //@"C:\TT\Ensemble Results\c2k\stateless cudnn baseline", done
                //@"C:\TT\Ensemble Results\c2k\stateless cudnn no_overlap_s2i0", done
                //@"C:\TT\Ensemble Results\c2k\stateless cudnn no_overlap_s2i1", done
                //@"C:\TT\Ensemble Results\c2k\stateless cudnn sorting1 v1.31", done
                //@"C:\TT\Ensemble Results\c2k\stateless cudnn sorting2 v1.32", done
                //@"C:\TT\Ensemble Results\c2k\stateless cudnn sorting3 v1.33", done
                //@"C:\TT\Ensemble Results\c2k\stateless cudnn sorting4 v1.34", done
                //@"C:\TT\Ensemble Results\c2k\stateless cudnn sorting5 v1.35", done
                //@"C:\TT\Ensemble Results\c2k\stateless cudnn no_overlap_s4i0", done
                //@"C:\TT\Ensemble Results\c2k\stateless cudnn no_overlap_s4i1", done
                //@"C:\TT\Ensemble Results\c2k\stateless cudnn no_overlap_s4i2", done
                //@"C:\TT\Ensemble Results\c2k\stateless cudnn no_overlap_s4i3", done
                //@"C:\TT\Ensemble Results\c2k\stateless cudnn no_overlap_s8i0", done
                //@"C:\TT\Ensemble Results\c2k\stateless cudnn no_overlap_s8i1", done
                //@"C:\TT\Ensemble Results\c2k\stateless cudnn no_overlap_s8i2", done
                //@"C:\TT\Ensemble Results\c2k\stateless cudnn no_overlap_s8i3", done
                //@"C:\TT\Ensemble Results\c2k\stateless cudnn no_overlap_s8i4", done
                //@"C:\TT\Ensemble Results\c2k\stateless cudnn no_overlap_s8i5", done
                //@"C:\TT\Ensemble Results\c2k\stateless cudnn no_overlap_s8i6", done 
                //@"C:\TT\Ensemble Results\c2k\stateless cudnn no_overlap_s8i7", done
                //@"C:\TT\Ensemble Results\c2k\stateless lstm v2bagging_10_v1.61",
                //@"C:\TT\Ensemble Results\c2k\stateless lstm v2bagging_09_v1.62",
                //@"D:\Sciebo\TT\Ensemble Results\c2k\stateless lstm v2bagging_08_v1.63",
                //@"C:\TT\Ensemble Results\c2k\stateless lstm v2bagging_07_v1.64",
                //@"C:\TT\Ensemble Results\c2k\stateless lstm v2bagging_06_v1.65",
                //@"C:\TT\Ensemble Results\c2k\stateless lstm v2bagging_05_v1.66",
                //@"C:\TT\Ensemble Results\c2k\stateless lstm v2bagging_04_v1.67",
                //@"C:\TT\Ensemble Results\c2k\stateless lstm v2bagging_03_v1.68",
                //@"C:\TT\Ensemble Results\c2k\stateless lstm v2bagging_02_v1.69",
                //@"C:\TT\Ensemble Results\c2k\stateless lstm v2bagging_01_v1.610",
                //@"C:\TT\Ensemble Results\c2k\stateless lstm v2bagging_11_v1.611",
                //@"C:\TT\Ensemble Results\c2k\stateless lstm v2bagging_12_v1.612",
                //@"C:\TT\Ensemble Results\c2k\stateless lstm v2bagging_13_v1.613",
                //@"C:\TT\Ensemble Results\c2k\stateless lstm v2bagging_14_v1.614",
                //@"C:\TT\Ensemble Results\c2k\stateless lstm v2bagging_15_v1.615",
                //@"C:\TT\Ensemble Results\c2k\stateless lstm v2bagging_16_v1.616",
                //@"C:\TT\Ensemble Results\c2k\stateless lstm v2bagging_17_v1.617",
                //@"C:\TT\Ensemble Results\c2k\stateless lstm v2bagging_18_v1.618",
                //@"C:\TT\Ensemble Results\c2k\stateless lstm v2bagging_19_v1.619",
                //@"C:\TT\Ensemble Results\c2k\stateless lstm v2bagging_20_v1.620",
                //@"C:\TT\Ensemble Results\c2k\stateless lstm v2duplicates_005_v1.71",
                //@"C:\TT\Ensemble Results\c2k\stateless lstm v2duplicates_010_v1.72",
                //@"C:\TT\Ensemble Results\c2k\stateless lstm v2duplicates_015_v1.73",
                //@"C:\TT\Ensemble Results\c2k\stateless lstm v2duplicates_020_v1.74",
                //@"C:\TT\Ensemble Results\c2k\stateless lstm v2duplicates_025_v1.75",
                //@"C:\TT\Ensemble Results\c2k\stateless lstm v2duplicates_030_v1.76",
                //@"C:\TT\Ensemble Results\c2k\stateless lstm v2duplicates_035_v1.77",
                //@"C:\TT\Ensemble Results\c2k\stateless lstm v2duplicates_040_v1.78",
                //@"C:\TT\Ensemble Results\c2k\stateless lstm v2duplicates_045_v1.79",
                //@"C:\TT\Ensemble Results\c2k\stateless lstm v2duplicates_050_v1.710",
                //@"C:\TT\Ensemble Results\c2k\stateless lstm v2duplicates_055_v1.711",
                //@"C:\TT\Ensemble Results\c2k\stateless lstm v2duplicates_060_v1.712",
                //@"C:\TT\Ensemble Results\c2k\stateless lstm v2duplicates_065_v1.713",
                //@"C:\TT\Ensemble Results\c2k\stateless lstm v2duplicates_070_v1.714",
                //@"C:\TT\Ensemble Results\c2k\stateless lstm v2duplicates_075_v1.715",
                //@"C:\TT\Ensemble Results\c2k\stateless lstm v2duplicates_080_v1.716",
                //@"C:\TT\Ensemble Results\c2k\stateless lstm v2duplicates_085_v1.717",
                //@"C:\TT\Ensemble Results\c2k\stateless lstm v2duplicates_090_v1.718",
                //@"C:\TT\Ensemble Results\c2k\stateless lstm v2duplicates_095_v1.719",
                //@"C:\TT\Ensemble Results\c2k\stateless lstm v2duplicates_100_v1.720",
                //@"C:\TT\Ensemble Results\c2k\stateless lstm v2bagging_wo-zl_01_v1.81",
                //@"C:\TT\Ensemble Results\c2k\stateless lstm v2bagging_wo-zl_02_v1.82",
                //@"C:\TT\Ensemble Results\c2k\stateless lstm v2bagging_wo-zl_03_v1.83",
                //@"C:\TT\Ensemble Results\c2k\stateless lstm v2bagging_wo-zl_04_v1.84",
                //@"C:\TT\Ensemble Results\c2k\stateless lstm v2bagging_wo-zl_05_v1.85",
                //@"C:\TT\Ensemble Results\c2k\stateless lstm v2bagging_wo-zl_06_v1.86",
                //@"C:\TT\Ensemble Results\c2k\stateless lstm v2bagging_wo-zl_07_v1.87",
                //@"C:\TT\Ensemble Results\c2k\stateless lstm v2bagging_wo-zl_08_v1.88",
                //@"C:\TT\Ensemble Results\c2k\stateless lstm v2bagging_wo-zl_09_v1.89",
                //@"C:\TT\Ensemble Results\c2k\stateless lstm v2bagging_wo-zl_010_v1.810",
                //@"D:\Sciebo\TT\Ensemble Results\c2k\stateless cudnn nosubsequences",
                @"D:\Sciebo\TT\Ensemble Results\tt\v2.11"
            };
        }
    }
}
