using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Reactive.Linq;
using System.Reactive.Subjects;

/// <summary>
/// NBBO Test - Nomura US (C) 2018
/// </summary>
namespace NbboService
{
    public class NbboServiceImpl : IService
    {
        private int counter;
        private ConcurrentDictionary<string, NbboWithDepth> cache = new ConcurrentDictionary<string, NbboWithDepth>();


        // for streaming
        private readonly Subject<NBBO> nbboSubject = new Subject<NBBO>();
        private ConcurrentDictionary<string, Subject<Quote>> depthSubjects = new ConcurrentDictionary<string, Subject<Quote>>();

        public NbboServiceImpl()
        {
            PricesGenerator generator = new PricesGenerator(this);
            generator.Start();
        }

        private Quote parse(string data)
        {
            String[] parts = data.Split(',');
            string symbol = "", exch = "";
            decimal bid = 0, ask = 0, last = 0;
            int bidSize = 0, askSize = 0;
            for (int i = 0; i < parts.Length; i++)
            {
                String[] tokens = parts[i].Split('=');
                if(tokens[0].Equals("Name"))
                {
                    symbol = tokens[1];
                }
                else if (tokens[0].Equals("Bid"))
                {
                    bid = Decimal.Parse(tokens[1]);
                }
                else if (tokens[0].Equals("BidSize"))
                {
                    bidSize = Int32.Parse(tokens[1]);
                }
                else if (tokens[0].Equals("Ask"))
                {
                    ask = Decimal.Parse(tokens[1]);
                }
                else if (tokens[0].Equals("AskSize"))
                {
                    askSize = Int32.Parse(tokens[1]);
                }
                else if (tokens[0].Equals("Last"))
                {
                    last = Decimal.Parse(tokens[1]);
                }
                else if(tokens[0].Equals("Exch"))
                {
                    exch = tokens[1];
                }
            }
            Quote q = new Quote { Symbol = symbol, Bid = bid, BidSize = bidSize, Ask = ask, AskSize = askSize, Last = last, Exchange = exch };
            return q;
        }
        /// <summary>
        /// 
        /// data format:
        /// "Name=IBM,Ask=5.34,AskSize=100,Bid=4.11,BidSize=200,Last=3.4,Exch=NYSE";
        /// "Name=VOD,Ask=3.12,AskSize=100,Last=2.6,Exch=ARCA";
        /// "Name=VOD,Bid=1.1,BidSize=300,Exch=ARCA";
        /// </summary>
        /// <param name="data"></param>
        public void OnTick(string data)
        {
            Interlocked.Increment(ref counter);
            Quote q = parse(data);

            NbboWithDepth nbboWithDepth;
            ConcurrentDictionary<string, Quote> depth;
            NBBO nbbo;
            bool exists = cache.TryGetValue(q.Symbol, out nbboWithDepth);

            if (!exists)
            {
                depth = new ConcurrentDictionary<string, Quote>();
                nbbo = new NBBO();
                nbboWithDepth = new NbboWithDepth { Nbbo = nbbo, Depth = depth };
                cache.TryAdd(q.Symbol, nbboWithDepth);
            }
            else
            {
                depth = nbboWithDepth.Depth;
                nbbo = nbboWithDepth.Nbbo;
            }

            Quote oldQ;
            exists = depth.TryGetValue(q.Exchange, out oldQ);
      
            if (!exists)
            {
                depth.TryAdd(q.Exchange, q);
            }
            else
            {
                // combine bid and ask into one quote object
                Quote newQuote;
                if (q.Bid > 0 && q.Ask > 0)
                {
                    newQuote = new Quote { Symbol=q.Symbol, Bid = q.Bid, BidSize = q.BidSize, Ask = q.Ask, AskSize = q.AskSize, Exchange=q.Exchange, Last=q.Last };
                }
                else if(q.Ask > 0)
                {
                    newQuote = new Quote { Symbol = q.Symbol, Bid = oldQ.Bid, BidSize = oldQ.BidSize, Ask = q.Ask, AskSize = q.AskSize, Exchange = q.Exchange, Last = q.Last };
                }
                else if (q.Bid > 0)
                {
                    newQuote = new Quote { Symbol = q.Symbol, Bid = q.Bid, BidSize = q.BidSize, Ask = oldQ.Ask, AskSize = oldQ.AskSize, Exchange = q.Exchange, Last = q.Last };
                }
                else
                {
                    newQuote = new Quote { Symbol = q.Symbol, Bid = oldQ.Bid, BidSize = oldQ.BidSize, Ask = oldQ.Ask, AskSize = oldQ.AskSize, Exchange = q.Exchange, Last = q.Last };
                }
                depth.AddOrUpdate(q.Exchange, newQuote, (key, oldValue) => newQuote);
                q = newQuote;
            }

            Subject<Quote> depthSubject;
            exists = depthSubjects.TryGetValue(q.Symbol, out depthSubject);

            if (!exists)
            {
                depthSubject = new Subject<Quote>();
                depthSubjects.TryAdd(q.Symbol, depthSubject);
            }

            depthSubject.OnNext(q);


            //if bid ask is clearly outside out NBBO, no need to update nbbo.
            if (q.Bid < nbbo.Bid && q.Ask > nbbo.Ask && nbbo.Bid != 0 && nbbo.Ask != 0)
                return;

            decimal nbboBid = decimal.MinValue , nbboAsk = decimal.MaxValue;
            int bidSize = 0, askSize = 0;
            foreach (KeyValuePair<string, Quote> item in depth)
            {
                if(item.Value.Bid > nbboBid)
                {
                    nbboBid = item.Value.Bid;
                    bidSize = item.Value.BidSize;
                }
                else if(item.Value.Bid == nbboBid)
                {
                    bidSize += item.Value.BidSize;
                }

                if (item.Value.Ask < nbboAsk)
                {
                    nbboAsk = item.Value.Ask;
                    askSize = item.Value.AskSize;
                }
                else if (item.Value.Ask == nbboAsk)
                {
                    askSize += item.Value.AskSize;
                }
            }

            NBBO newNbbo = new NBBO { Symbol = q.Symbol, Bid = nbboBid, BidSize = bidSize, Ask = nbboAsk, AskSize = askSize };
            
            nbboWithDepth = new NbboWithDepth { Nbbo = newNbbo, Depth = depth };

            cache.AddOrUpdate(q.Symbol, nbboWithDepth, (key, oldValue) => nbboWithDepth);

            nbboSubject.OnNext(newNbbo);
        }

        public IObservable<NBBO> Stream()
        {
            return nbboSubject;
        }

        public IObservable<Quote> SubscribeToDepth(string symbol)
        {
            Subject<Quote> depthSubject;
            bool exists = depthSubjects.TryGetValue(symbol, out depthSubject);

            if (!exists)
            {
                depthSubject = new Subject<Quote>();
                depthSubjects.TryAdd(symbol, depthSubject);
            }
            return depthSubject;
        }

        /// <summary>
        /// Provides string with current market offers for the product
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public object GetMarketData(string name)
        {
            NbboWithDepth nbbo;
            bool exists = cache.TryGetValue(name, out nbbo);

            if (exists)
            {
                if (nbbo.Depth != null)
                    return nbbo.Depth.ToArray();
                else
                    return null;
            }
            else
                return null;
        }

        /// <summary>
        /// Provides string with NBBO market offers for the product
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public object GetNbboData(string name)
        {
            NbboWithDepth nbbo;
            bool exists = cache.TryGetValue(name, out nbbo);
            
            if (exists)
            {
                if (nbbo.Nbbo != null)
                    return nbbo.Nbbo;
                else
                    return null;

            }
            else
                return null;
        }

        public List<NBBO> GetAll()
        {
            List<NBBO> all = new List<NBBO>();
            foreach (KeyValuePair<string, NbboWithDepth> item in cache)
            {
                all.Add(item.Value.Nbbo);
            }
            return all;
        }

        /// <summary>
        /// Returns number of Ticks received
        /// </summary>
        /// <returns></returns>
        public int Count()
        {
            return counter;
        }

        public static void UnusedMain(string[] args)
        {
            NbboServiceImpl s = new NbboServiceImpl();

            /*
            test(s);
            NBBO nbbo = (NBBO)s.GetNbboData("IBM");
            Console.WriteLine(nbbo);
            */
            
            while (true)
            {
                Console.Write("enter nbbo ticker or depth ticker(ctrl-c to kill):");
                string cmd = Console.ReadLine();
                string[] tokens = cmd.Split(' ');
                if(tokens.Length > 1)
                {
                    if (tokens[0].Equals("nbbo"))
                    {
                        NBBO nbbo = (NBBO)s.GetNbboData(tokens[1].ToUpper());
                        if (nbbo != null)
                            Console.WriteLine(nbbo);
                        else
                            Console.WriteLine("ticker not found");
                    }
                    else
                    {
                        KeyValuePair<string, Quote>[] depth = (KeyValuePair<string, Quote>[])s.GetMarketData(tokens[1].ToUpper());
                        if (depth != null)
                        {
                            foreach (KeyValuePair<string, Quote> item in depth)
                            {
                                Console.WriteLine(item.Value);
                            }
                        }
                        else
                            Console.WriteLine("ticker not found");
                    }
                }
            }

        }

        public static void test(NbboServiceImpl s)
        {
            s.OnTick("Name=IBM,Bid=150,BidSize=100,Last=150,Exch=NYSE");
            s.OnTick("Name=IBM,Ask=151,AskSize=200,Last=150,Exch=NYSE");
            s.OnTick("Name=IBM,Bid=150.1,BidSize=50,Last=150,Exch=US-C");
            s.OnTick("Name=IBM,Ask=151.1,AskSize=150,Last=150,Exch=US-C");
            s.OnTick("Name=IBM,Bid=150,BidSize=25,Last=150,Exch=ARCA");
            s.OnTick("Name=IBM,Ask=151,AskSize=250,Last=150,Exch=ARCA");
            s.OnTick("Name=IBM,Bid=150.2,BidSize=200,Last=150,Exch=BAT");
            s.OnTick("Name=IBM,Ask=151.2,AskSize=20,Last=150,Exch=BAT");
            s.OnTick("Name=IBM,Bid=150.2,BidSize=210,Last=150,Exch=LSE");
            s.OnTick("Name=IBM,Ask=151.2,AskSize=10,Last=150,Exch=LSE");
            s.OnTick("Name=IBM,Bid=150.2,BidSize=90,Last=150,Exch=TOT");
            s.OnTick("Name=IBM,Ask=151.2,AskSize=190,Last=150,Exch=TOT");
        }
    }
}
