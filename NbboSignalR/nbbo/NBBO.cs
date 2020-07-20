using System;
using System.Collections.Concurrent;

namespace NbboService
{
    public class NBBO
    {

        public string Symbol { get; set; }

        public decimal Bid { get; set; }

        public decimal BidSize { get; set; }

        public decimal Ask { get; set; }

        public decimal AskSize { get; set; }

        public override string ToString()
        {
            return "Symbol=" + Symbol + ",Bid=" + Bid + ",BidSize=" + BidSize + ",Ask=" + Ask + ",AskSize=" + AskSize;
        }

    }
}
