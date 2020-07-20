/// <summary>
/// NBBO Test - Nomura US (C) 2018
/// </summary>
namespace NbboService
{
    public interface IService
    {
        /// <summary>
        /// 
        /// data format:
        /// "Name=IBM,Ask=5.34,AskSize=100,Bid=4.11,BidSize=200,Last=3.4,Exch=NYSE";
        /// "Name=VOD,Ask=3.12,AskSize=100,Last=2.6,Exch=ARCA";
        /// "Name=VOD,Bid=1.1,BidSize=300,Exch=ARCA";
        /// </summary>
        /// <param name="data"></param>
        void OnTick(string data);

        /// <summary>
        /// Provides string with current market offers for the product
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        object GetMarketData(string name);

        /// <summary>
        /// Provides string with NBBO market offers for the product
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        object GetNbboData(string name);

        /// <summary>
        /// Returns number of Ticks received
        /// </summary>
        /// <returns></returns>
        int Count();

    }
}
