#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies
{
	public class PVTMACDv1 : Strategy
	{
		private PVT PVT1;
		private SMA SMA1;
		private MACD MACD1;
		private EMA EMA1;
		
		private string  atmStrategyId			= string.Empty;
		private string  orderId					= string.Empty;
		
		private bool	isAtmStrategyCreated	= false;
		private bool	comprado				= false;
		private bool	vendido					= false;
		private bool    goodToGo				= true;
		
		private double preco;
		private double currAsk;
		private double currBid;
		private double currentPnL;
		private double dailyPnL;

		private int Cancelamento			= 7;
		private int DayGainStop				= 1200;
		private int DayLossStop				= 300;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Cruzamento PVT com SMA mais MACD Histograma em tendencia";
				Name										= "PVTMACDv1";
				Calculate									= Calculate.OnBarClose;
				EntriesPerDirection							= 1;
				EntryHandling								= EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy				= true;
				ExitOnSessionCloseSeconds					= 2700;
				IsFillLimitOnTouch							= false;
				MaximumBarsLookBack							= MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution							= OrderFillResolution.Standard;
				Slippage									= 0;
				StartBehavior								= StartBehavior.WaitUntilFlat;
				TimeInForce									= TimeInForce.Day;
				TraceOrders									= false;
				RealtimeErrorHandling						= RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling							= StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade							= 20;
				IsInstantiatedOnEachOptimizationIteration	= true;							
			}
			else if (State == State.Configure)
			{
				
			}
			else if (State == State.DataLoaded)
			{				
				PVT1							= PVT(Close);
				SMA1							= SMA(PVT1, 50);
				MACD1							= MACD(17,72,34);
				EMA1							= EMA(MACD1.Diff, 8);
				
              	Draw.TextFixed(this,"Robo", "PVTMACDv1", TextPosition.BottomLeft);
				
                StrategyReset();
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < BarsRequiredToTrade)
				return;
			
			// OnBarUpdate() will be called on incoming tick events on all Bars objects added to the strategy
			// We only want to process events on our primary Bars object (index = 0) which is set when adding
			// the strategy to a chart
			if (BarsInProgress != 0)
				return;
			
			// Make sure this strategy does not execute against historical data
			if(State == State.Historical)
				return;	
					
			if ((ToTime(Time[0]) <= 190000 && ToTime(Time[0]) >= 170000))
			{
				if (orderId.Length > 0)
				{
				  	AtmStrategyCancelEntryOrder(orderId);
					orderId = string.Empty;
				  	Print(Time[0] +" PVTMACDv1 "+ Instrument.FullName + " Todas Ordens Canceladas");
				}
				if (atmStrategyId.Length > 0)
				{
					AtmStrategyClose(atmStrategyId);
					atmStrategyId = string.Empty;
					Print(Time[0] +" PVTMACDv1 "+ Instrument.FullName + " Todas Posições Fechadas");
				}
			// Reset da Estrategia
				StrategyReset();
				return;
			}
			
			if(goodToGo)
        	{				
						//Meta Diaria
						if (dailyPnL <= (-DayLossStop))
							{
								Print(Time[0] +" PVTMACDv1 "+ Instrument.FullName + " Meta Perda Diaria");
								goodToGo = false;
								return;
							}
						if (dailyPnL >= DayGainStop)
							{
								Print(Time[0] +" PVTMACDv1 "+ Instrument.FullName + " Meta Ganho Diario");
								goodToGo = false;
								return;
							}
							

						// Compra
						if ((orderId.Length == 0 && atmStrategyId.Length == 0)
							
							&& (PVT1[0] > SMA1[0])
							&& (MACD1.Diff[0] > EMA1[0])
							&& (MACD1.Diff[0] > MACD1.Diff[1])
							
							&& (Close[0] > Open[0])
							) 
						{
							isAtmStrategyCreated = false;  // reset atm strategy created check to false
							atmStrategyId = GetAtmStrategyUniqueId();
							orderId = GetAtmStrategyUniqueId();
							AtmStrategyCreate(OrderAction.Buy, OrderType.Limit, Close[0], 0, TimeInForce.Day, orderId, "ATM_PVTMACD", atmStrategyId, (atmCallbackErrorCode, atmCallBackId) => {
								//check that the atm strategy create did not result in error, and that the requested atm strategy matches the id in callback
								if (atmCallbackErrorCode == ErrorCode.NoError && atmCallBackId == atmStrategyId)
									isAtmStrategyCreated = true;
							});
								comprado = true;
								vendido = false;
								preco = Close[1];
								Print(Time[0] +" PVTMACDv1 "+ Instrument.FullName + " Comprado");
						}
						// Venda
						if ((orderId.Length == 0 && atmStrategyId.Length == 0)
							
							&& (PVT1[0] < SMA1[0])
							&& (MACD1.Diff[0] < EMA1[0])
							&& (MACD1.Diff[0] < MACD1.Diff[1])
							
							&& (Close[0] < Open[0])
							)
						{
							isAtmStrategyCreated = false;  // reset atm strategy created check to false
							atmStrategyId = GetAtmStrategyUniqueId();
							orderId = GetAtmStrategyUniqueId();
							AtmStrategyCreate(OrderAction.Sell, OrderType.Limit, Close[0], 0, TimeInForce.Day, orderId, "ATM_PVTMACD", atmStrategyId, (atmCallbackErrorCode, atmCallBackId) => {
								//check that the atm strategy create did not result in error, and that the requested atm strategy matches the id in callback
								if (atmCallbackErrorCode == ErrorCode.NoError && atmCallBackId == atmStrategyId)
									isAtmStrategyCreated = true;
							});
								vendido = true;
								comprado = false;
								preco = Close[1];
								Print(Time[0] +" PVTMACDv1 "+ Instrument.FullName + " Vendido");
						}

       		}
			
			if (!isAtmStrategyCreated)
				return;
		
			// Check for a pending entry order
			if (orderId.Length > 0)
			{
				string[] status = GetAtmStrategyEntryOrderStatus(orderId);

				// If the status call can't find the order specified, the return array length will be zero otherwise it will hold elements
				if (status.GetLength(0) > 0)
				{
					// If the order state is terminal, reset the order id value
					if (status[2] == "Filled" || status[2] == "Cancelled" || status[2] == "Rejected")
					{
						Print(Time[0] +" PVTMACDv1 "+ Instrument.FullName + " State: " + status[2]);
						orderId = string.Empty;
					}
				}
			} 
			
			// If the strategy has terminated reset the strategy id
			else if (atmStrategyId.Length > 0 && GetAtmStrategyMarketPosition(atmStrategyId) == Cbi.MarketPosition.Flat)
			{
				ScreenUpdate();
				atmStrategyId = string.Empty;
			}

			//Cancelaqmento de Ordem nao executada 
			if (atmStrategyId.Length > 0)
			{
				
				CancelamentoDeOrdem();
			}
								
		}
		
		public void ScreenUpdate()
		{
			if (atmStrategyId.Length > 0)
				currentPnL = GetAtmStrategyRealizedProfitLoss(atmStrategyId);
			else
			 	currentPnL = 0;
			
			dailyPnL = dailyPnL + currentPnL;
			Draw.TextFixed(this,"Info", "Daily PnL = " + dailyPnL, TextPosition.BottomRight);
		}
		
		public void StrategyReset()
		{
			goodToGo = true;	
			dailyPnL = 0;
			currentPnL = 0;
			Draw.TextFixed(this,"Info", "Daily PnL = " + dailyPnL, TextPosition.BottomRight);
	
		}
		
		public void CancelamentoDeOrdem()
		{
				
				currAsk = GetCurrentAsk(0);
				currBid = GetCurrentBid(0);
					
					if (((GetAtmStrategyMarketPosition(atmStrategyId) == Cbi.MarketPosition.Flat) && (comprado == true && (currBid - Cancelamento * TickSize) >= preco)) 
						|| ((GetAtmStrategyMarketPosition(atmStrategyId) == Cbi.MarketPosition.Flat) && (vendido == true && (currAsk + Cancelamento * TickSize) <= preco)))
					{
							AtmStrategyCancelEntryOrder(orderId);
							Print(Time[0] +" PVTMACDv1 "+ Instrument.FullName + " Ordem NAO Executada CANCELADA");
							atmStrategyId = string.Empty;
							orderId = string.Empty;
							comprado = false;
							vendido = false;
					}
		}
		
		#region ConnectionHandling
		protected override void OnConnectionStatusUpdate(ConnectionStatusEventArgs connectionStatusUpdate)
			{
			if(connectionStatusUpdate.Status == ConnectionStatus.Connected)
			  {
			    Print("PVTMACDv1 "+ Instrument.FullName + " Connected at " + DateTime.Now);
				  if (atmStrategyId.Length > 0)
				  {
					AtmStrategyClose(atmStrategyId);
					atmStrategyId = string.Empty;
					Print(DateTime.Now +" PVTMACDv1 "+ Instrument.FullName + " Todas ATMs Fechadas");
				  }
				  
				  if (orderId.Length > 0)
					{
						AtmStrategyCancelEntryOrder(orderId);
						orderId = string.Empty;
						Print(DateTime.Now +" PVTMACDv1 "+ Instrument.FullName + " Todas Ordens Canceladas");
					}
				  
				  if (PositionAccount.MarketPosition != MarketPosition.Flat)
				  {
					if (PositionAccount.MarketPosition == MarketPosition.Long)
					{
						ExitLong();
					}
					else
					{
						ExitShort();
					}
					Print(DateTime.Now +" PVTMACDv1 "+ Instrument.FullName + " Todas Posições Fechadas");
				  }
			  }
			  
			  else if(connectionStatusUpdate.Status == ConnectionStatus.ConnectionLost)
			  {
			    Print("PVTMACDv1 "+ Instrument.FullName + " Connection lost at: " + DateTime.Now);
				if (orderId.Length > 0)
				{
				  AtmStrategyCancelEntryOrder(orderId);
				  orderId = string.Empty;
				  Print(DateTime.Now +" PVTMACDv1 "+ Instrument.FullName + " Todas Ordens Canceladas");
				}
			  }
			}
        #endregion

	}
}
