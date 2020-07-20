using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
//
//
// Prices feed generator for NBBO test - Nomura US (C) 2018
//
//
namespace NbboService
{
    public sealed class PricesGenerator
    {
        private IService _service;
        private readonly Random _random = new Random(DateTime.Now.Millisecond);
        private readonly string[] _names = new string[] { "MSFT", "AAPL", "IBM", "SPX", "NMR", "F", "SPY", "VOD", "QOOG", "N225" };
        
        private readonly Dictionary<string, double> EODPrices = new Dictionary<string, double>();

        private readonly string[] _exchanges = new string[] { "NYSE", "US-C", "ARCA", "BAT", "LSE", "TOT" };
        private Task _task;
        private bool _stopFlag;

        private void loadEODPrices()
        {
            for(int i = 0; i < _names.Length; i++)
            {
                EODPrices[_names[i]] = 100 + _random.NextDouble() * 100;
            }
        }

        public PricesGenerator(IService service)
        {
            _service = service;
            loadEODPrices();
        }

        public ICollection<string> GetNames()
        {
            return _names;
        }
        public string GenerateData()
        {
            var name = _names[_random.Next(0, _names.Length-1)];
            var exchange = _exchanges[_random.Next(0, _exchanges.Length - 1)];
            
            //var lastPrice = _random.NextDouble() * 10;
            // mitigate crossed market slightly
            var lastPrice = EODPrices[name] + _random.NextDouble() * 2;

            var ask = lastPrice +_random.NextDouble();
            var bid = lastPrice - _random.NextDouble();
            var sb = new StringBuilder("Name=").Append(name);

            /* this generated crossed bid/ask on same exchange.
             * bool isAskAdded = false;
             * 
            if (_random.Next(100) > 50)
            {
                sb.Append(",Ask=").Append(ask).Append(",AskSize=").Append(_random.Next(1, 500));
                isAskAdded = true;
            }
            if (_random.Next(100) > 50 || !isAskAdded)
            {
                sb.Append(",Bid=").Append(bid).Append(",BidSize=").Append(_random.Next(1, 500));
            }
            */

            // still have crossed bid/ask from different exchanges
            sb.Append(",Ask=").Append(string.Format("{0:0.######}", ask)).Append(",AskSize=").Append(_random.Next(1, 500));
            sb.Append(",Bid=").Append(string.Format("{0:0.######}", bid)).Append(",BidSize=").Append(_random.Next(1, 500));

            sb.Append(",Last=").Append(lastPrice);
            sb.Append(",Exch=").Append(exchange);
            return sb.ToString();
        }

        public void Start()
        {
            _task = Task.Run(new Action(GenerateTicks));
        }

        public void GenerateTicks()
        {
            const int threads = 5;
            Parallel.ForEach(Enumerable.Range(0,threads), new ParallelOptions {MaxDegreeOfParallelism = threads}, id =>
            {
                while (!_stopFlag)
                {
                    var data = GenerateData();
                    _service.OnTick(data);
                    Thread.Sleep(_random.Next(250));
                }
            });
        }

        public void Stop()
        {
            _stopFlag = true;
            while (_task.Status == TaskStatus.Running) ;
        }
    }
}
