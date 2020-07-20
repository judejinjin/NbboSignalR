using System;
using System.Collections.Concurrent;

namespace NbboService
{
    public class NbboWithDepth
    {

        public NBBO Nbbo { get; set; }

        public ConcurrentDictionary<string, Quote> Depth { get; set; }
        
    }
}
