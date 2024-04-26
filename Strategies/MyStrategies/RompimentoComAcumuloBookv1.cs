                     using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PTLRuntime.NETScript.Indicators;
using PTLRuntime.NETScript;

namespace RompimentoComAcumuloBookv1
{
    public class RompimentoComAcumuloBookv1 : NETStrategy
    {
        public RompimentoComAcumuloBookv1()
            : base()
        {
            #region Initialization
            base.ProjectName = "RompimentoComAcumuloBookv1";
            base.Comments = "Rompimento do candle com Acumulo de ordens no book";
            base.Version = "1.0";
            #endregion
        }

        #region Parameters
      
        [InputParameter("Amount", 1, 0, 500)]
        public int Amount = 1;

        [InputParameter("Distancia", 2, 0, 500)]
        public int distancia = 1;

        [InputParameter("Ordem", 6, new object[]
                        {
                            "Limit", OrdersType.Limit,
                            "Market", OrdersType.Market
                        })]
        public OrdersType tipo = OrdersType.Market;

        [InputParameter("SL", 7, 0, 500)]
        public int SL = 7;

        [InputParameter("TP", 8, 0, 500)]
        public int TP = 2;

        [InputParameter("Session end", 9)]
        public TimeSpan sessionEnd = new TimeSpan(22, 00, 0);
        
        
        #endregion
        
		TimeSpan time;

		double high, low;

        BarData curData;
               
		bool  pBID, pASK;
        
        string posId;
        
        Instrument instr;
        
		Level2[] ask, bid;
		
		double tBid, tBid1, tBid2, tBid3, tBid4, tAsk, tAsk1, tAsk2, tAsk3, tAsk4, porcBid, porcAsk = 0;
		
		int a1, a2, a3, a4, b1, b2, b3, b4 = 0;

        public override void Init()
        {
            curData = CurrentData as BarData;
			high = 0;
			low = 0;
			
			Instruments.NewLevel2 += OnLevel2;
			
			instr = Instruments.Current;
			
			Instruments.Subscribe(instr, QuoteTypes.Level2);
        }
        

		public override void OnQuote()
		{
			time = CurrentData.Time().TimeOfDay;
			
			bool buy = high > 0 && Instruments.Current.LastQuote.Last >= high + distancia * Point && pASK;
			bool sell = low > 0 && Instruments.Current.LastQuote.Last <= low - distancia * Point && pBID;
				
			if (buy || sell)
			{
				var request = NewRequest((buy) ? Operation.Buy : Operation.Sell);
				if (request != null)
				{
					posId = Orders.Send(request);
					
				}

				return;
			}
		}

		public override void NextBar()
		{
			if (time > sessionEnd)
			{
				Print("Session End " + time);
				high = 0;
				low = 0;
				return;
			}
			
			else
			{				
				high = curData.High(1);
				low = curData.Low(1);
			}
			
			
			Print("Time = " + time);
			Print("High = " + high);
			Print("Low = " + low);
			Print("-----------");
		}
		
		private void OnLevel2(Instrument instr, Level2 level2)
		{
			bid = Instruments.Current.getBidsDepth();
			ask = Instruments.Current.getAsksDepth();
			
			
			for (int i = 0; bid[i].Price == bid[0].Price; i++)
			{
				tBid += bid[i].Size;
				
				a1 = i + 1;
				
				for (int a = a1; bid[a].Price == bid[0].Price - 1 * Point; a++)
				{
					tBid1 += bid[a].Size;
					
					a2 = a + 1;
					
					for (int b = a2; bid[b].Price == bid[0].Price - 2 * Point; b++)
					{
						tBid2 += bid[b].Size;
						
						a3 = b + 1;
						
						for (int c = a3; bid[c].Price == bid[0].Price - 3 * Point; c++)
						{
							tBid3 += bid[c].Size;
							
							a4 = c + 1;
							
							for (int d = a4; bid[d].Price == bid[0].Price - 4 * Point; d++)
							{
								tBid4 += bid[d].Size;
							}
						}
					}
				}
			}
			
			for (int i = 0; ask[i].Price == ask[0].Price; i++)
			{
				tAsk += ask[i].Size;
				
				b1 = i + 1;
				
				for (int a = b1; ask[a].Price == ask[0].Price + 1 * Point; a++)
				{
					tAsk1 += ask[a].Size;
					
					b2 = a + 1;
					
					for (int b = b2; ask[b].Price == ask[0].Price + 2 * Point; b++)
					{
						tAsk2 += ask[b].Size;
						
						b3 = b + 1;
						
						for (int c = b3; ask[c].Price == ask[0].Price + 3 * Point; c++)
						{
							tAsk3 += ask[c].Size;
							
							b4 = c + 1;
							
							for (int d = b4; ask[d].Price == ask[0].Price + 4 * Point; d++)
							{
								tAsk4 += ask[d].Size;
							}

						}
					}
				}
				
			}
			
			var resultBid = tBid + tBid1 + tBid2 + tBid3 + tBid4;
			var resultAsk = tAsk + tAsk1 + tAsk2 + tAsk3 + tAsk4;
			porcBid = Math.Round((((resultBid) / (resultBid + resultAsk)) * 100), 2);
			porcAsk = Math.Round((((resultAsk) / (resultBid + resultAsk)) * 100), 2);
			
			if (porcBid >= 52)
			{
				pBID = true;
			}
			
			else
				pBID = false;
			
			if (porcAsk >= 52)
			{
				pASK = true;
			}
			
			else
				pASK = false;
			
			
			Comment(/*"\t Bid = " + tBid + "		\t Ask = " + tAsk 
			        + "\n \t Bd1 = " + tBid1 + "		\t Ak1 = " + tAsk1
			        + "\n \t Bd2 = " + tBid2 + "		\t Ak2 = " + tAsk2
			        + "\n \t Bd3 = " + tBid3 + "		\t Ak3 = " + tAsk3
			        + "\n \t Bd4 = " + tBid4 + "		\t Ak4 = " + tAsk4
			        +*/ "\n \t % Bid = " + porcBid + "%" + "\t % Ask = " + porcAsk + "%"
					+ "\n \t Bool BID = " + pBID + "\t Bool ASK = " + pASK);

			
			tBid = 0;
			tBid1 = 0;
			tBid2 = 0;
			tBid3 = 0;
			tBid4 = 0;
			tAsk = 0;
			tAsk1 = 0;
			tAsk2 = 0;
			tAsk3 = 0;
			tAsk4 = 0;
			resultBid = 0;
			resultAsk = 0;
			porcAsk = 0;
			porcBid = 0;
		}

        public override void Complete()
        {
        	Instruments.NewLevel2 -= OnLevel2;
			Instruments.Unsubscribe(instr, QuoteTypes.Level2);
        }

        private NewOrderRequest NewRequest(Operation side)
        {
            NewOrderRequest request = null;

            if (Positions.GetPositionByOpenOrderId(posId) == null)
            {
                request = new NewOrderRequest()
                {
                    Side = side,
                    Type = tipo,
                    Amount = Amount,
                    Account = Accounts.Current,
                    Instrument = Instruments.Current,
                    StopLossOffset = SL * Point,
                    TakeProfitOffset = TP * Point,
                    
                };
            }

            return request;
        }


    }
}
