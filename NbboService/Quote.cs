using System;

namespace NbboService
{
    public class Quote
    {
        public string Symbol { get; set; }

        public string Exchange { get; set; }

        public decimal Bid { get; set; }

        public int BidSize { get; set; }

        public decimal Ask { get; set; }

        public int AskSize { get; set; }

        public decimal Last { get; set; }

        public override string ToString()
        {
            return "Symbol=" + Symbol + ",Bid=" + Bid + ",BidSize=" + BidSize + ",Ask=" + Ask + ",AskSize=" + AskSize + ",Last=" + Last + ",Exch=" + Exchange;
        }
    }
}
