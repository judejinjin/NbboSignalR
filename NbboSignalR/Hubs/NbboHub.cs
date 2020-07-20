using System;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using NbboService;

namespace NbboSignalR.Hubs
{
    public class NbboHub : Hub
    {
        private readonly NbboServiceImpl _nbboService;
        //private ConcurrentDictionary<>

        public NbboHub(NbboServiceImpl nbboService)
        {
            _nbboService = nbboService;
        }

        public IEnumerable<NBBO> GetAllStocks()
        {
            return _nbboService.GetAll();
        }

        public ChannelReader<NBBO> StreamNBBOs()
        {
            return _nbboService.Stream().AsChannelReader(10);
        }

        public ChannelReader<Quote> StreamDepth(string symbol)
        {
            IObservable<Quote> observable = _nbboService.SubscribeToDepth(symbol);
            ChannelReader<Quote> reader = observable.AsChannelReader(10);
            
            return reader;
        }
    }
}