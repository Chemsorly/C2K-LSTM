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
                //@"C:\TT\Ensemble Results\colab\c2k\tpu test1",
                //@"C:\TT\Ensemble Results\colab\c2k\airportcode test1",
                //@"C:\TT\Ensemble Results\master\stateless cudnn gru",
                //@"C:\TT\Ensemble Results\master\stateless cudnn lstm batchsize 1",
                //@"C:\TT\Ensemble Results\master\stateless cudnn lstm batchsize 2",
                //@"C:\TT\Ensemble Results\master\stateless cudnn lstm batchsize 4",
                //@"C:\TT\Ensemble Results\master\stateless cudnn lstm batchsize 8",
                //@"C:\TT\Ensemble Results\master\stateless cudnn lstm batchsize 16",
                //@"C:\TT\Ensemble Results\master\stateless cudnn lstm batchsize 32",
                //@"C:\TT\Ensemble Results\master\stateless cudnn lstm batchsize 64",
                //@"C:\TT\Ensemble Results\master\stateless cudnn lstm batchsize 128",
                //@"C:\TT\Ensemble Results\master\stateless cudnn lstm batchsize 256",
                //@"C:\TT\Ensemble Results\master\stateless cudnn lstm clipvalue 0.1",
                //@"C:\TT\Ensemble Results\master\stateless cudnn lstm clipvalue 0.2",
                //@"C:\TT\Ensemble Results\master\stateless cudnn lstm clipvalue 0.4",
                //@"C:\TT\Ensemble Results\master\stateless cudnn lstm clipvalue 0.8",
                //@"C:\TT\Ensemble Results\master\stateless cudnn lstm clipvalue 1.6",
                //@"C:\TT\Ensemble Results\master\stateless cudnn lstm clipvalue 3.2",
                //@"C:\TT\Ensemble Results\master\stateless cudnn lstm clipvalue 6.4",
                //@"C:\TT\Ensemble Results\master\stateless cudnn lstm clipvalue 12.8",
                //@"C:\TT\Ensemble Results\master\stateless cudnn lstm networksize 100 1",
                //@"C:\TT\Ensemble Results\master\stateless cudnn lstm networksize 100 2",
                //@"C:\TT\Ensemble Results\master\stateless cudnn lstm networksize 100 3",
                //@"C:\TT\Ensemble Results\master\stateless cudnn lstm networksize 100 4",
                //@"C:\TT\Ensemble Results\master\stateless cudnn lstm networksize 100 5",
                //@"C:\TT\Ensemble Results\master\stateless cudnn lstm networksize 200 1",
                //@"C:\TT\Ensemble Results\master\stateless cudnn lstm networksize 200 2",
                //@"C:\TT\Ensemble Results\master\stateless cudnn lstm networksize 200 3",
                //@"C:\TT\Ensemble Results\master\stateless cudnn lstm networksize 200 4",
                //@"C:\TT\Ensemble Results\master\stateless cudnn lstm networksize 200 5",
                //@"C:\TT\Ensemble Results\master\stateless cudnn lstm networksize 300 1",
                //@"C:\TT\Ensemble Results\master\stateless cudnn lstm networksize 300 2",
                //@"C:\TT\Ensemble Results\master\stateless cudnn lstm networksize 300 3",
                //@"C:\TT\Ensemble Results\master\stateless cudnn lstm networksize 300 4",
                //@"C:\TT\Ensemble Results\master\stateless cudnn lstm networksize 300 5",
                //@"C:\TT\Ensemble Results\master\stateless cudnn lstm networksize 400 1",
                //@"C:\TT\Ensemble Results\master\stateless cudnn lstm networksize 400 2",
                //@"C:\TT\Ensemble Results\master\stateless cudnn lstm networksize 400 3",
                //@"C:\TT\Ensemble Results\master\stateless cudnn lstm networksize 400 4",
                //@"C:\TT\Ensemble Results\master\stateless cudnn lstm networksize 400 5",
                //@"C:\TT\Ensemble Results\master\stateless cudnn lstm networksize 500 1",
                //@"C:\TT\Ensemble Results\master\stateless cudnn lstm networksize 500 2",
                //@"C:\TT\Ensemble Results\master\stateless cudnn lstm networksize 500 3",
                //@"C:\TT\Ensemble Results\master\stateless cudnn lstm networksize 500 4",
                //@"C:\TT\Ensemble Results\master\stateless cudnn lstm networksize 500 5",
                
                //@"C:\TT\Ensemble Results\master\stateless cudnn bpi2012",
                @"C:\TT\Ensemble Results\master\stateless cudnn bpi2017",
                //@"C:\TT\Ensemble Results\master\stateless cudnn bpi2018",

                //@"C:\TT\Ensemble Results\master\stateless cudnn airportcode test1",
                //@"C:\TT\Ensemble Results\master\stateless cudnn generator1 baseline",
                //@"C:\TT\Ensemble Results\master\stateless cudnn generator1 generator",
                //@"C:\TT\Ensemble Results\master\stateless cudnn generator1 generator multi",
                //@"C:\TT\Ensemble Results\master\stateless cudnn patience 10 10",
                //@"C:\TT\Ensemble Results\master\stateless cudnn patience 20 10",
                //@"C:\TT\Ensemble Results\master\stateless cudnn patience 30 10",
                //@"C:\TT\Ensemble Results\master\stateless cudnn patience 40 10",
                //@"C:\TT\Ensemble Results\master\stateless cudnn patience 50 10",
                //@"C:\TT\Ensemble Results\master\stateless cudnn patience 10 20",
                //@"C:\TT\Ensemble Results\master\stateless cudnn patience 20 20",
                //@"C:\TT\Ensemble Results\master\stateless cudnn patience 30 20",
                //@"C:\TT\Ensemble Results\master\stateless cudnn patience 40 20",
                //@"C:\TT\Ensemble Results\master\stateless cudnn patience 50 20",
                //@"C:\TT\Ensemble Results\master\stateless cudnn patience 10 30",
                //@"C:\TT\Ensemble Results\master\stateless cudnn patience 20 30",
                //@"C:\TT\Ensemble Results\master\stateless cudnn patience 30 30",
                //@"C:\TT\Ensemble Results\master\stateless cudnn patience 40 30",
                //@"C:\TT\Ensemble Results\master\stateless cudnn patience 50 30",
                //@"C:\TT\Ensemble Results\master\stateless cudnn patience 10 40",
                //@"C:\TT\Ensemble Results\master\stateless cudnn patience 20 40",
                //@"C:\TT\Ensemble Results\master\stateless cudnn patience 30 40",
                //@"C:\TT\Ensemble Results\master\stateless cudnn patience 40 40",
                //@"C:\TT\Ensemble Results\master\stateless cudnn patience 50 40",
                //@"C:\TT\Ensemble Results\master\stateless cudnn patience 10 50",
                //@"C:\TT\Ensemble Results\master\stateless cudnn patience 20 50",
                //@"C:\TT\Ensemble Results\master\stateless cudnn patience 30 50",
                //@"C:\TT\Ensemble Results\master\stateless cudnn patience 40 50",
                //@"C:\TT\Ensemble Results\master\stateless cudnn patience 50 50",
            };
        }
    }
}
