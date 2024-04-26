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
	public class MACDHistoStochEMAv1 : Strategy
	{
		
		private SMA SMA1;
		private SMA SMA2;
		private SMA SMA3;
		private SMA SMA4;
		private SMA SMA5;
		private SMA SMA6;
		private HistoStochEMA HistoStoch1;
		private MACD MACDdiff;

		private string  atmStrategyId			= string.Empty;
		private string  orderId					= string.Empty;
		
		private string setaCompra					= "1000";
        private string setaVenda					= "2000";
		
		
		private bool	isAtmStrategyCreated	= false;
		private bool	comprado				= false;
		private bool	vendido					= false;
		private bool    goodToGo				= true;
		private bool    reseted					= false;
		private bool	setas					= false;
		
		private double preco;
		private double currAsk;
		private double currBid;
		private double currentPnL;
		private double dailyPnL;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Power Stocastics and Support/Resistence Strategies";
				Name										= "MACDHistoStochEMAv1";
				Calculate									= Calculate.OnEachTick;
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
				
				
				Cancelamento			= 7;
				DayGainStop				= 4000;
				DayLossStop				= 1500;
				RenkoBox				= 5;
				
			}
			else if (State == State.Configure)
			{
				
			}
			else if (State == State.DataLoaded)
			{				
				
				
				SMA1				= SMA(Close, 17);
				SMA2				= SMA(Open, 17);
				
				HistoStoch1				= HistoStochEMA(34,34,17);
				SMA3					= SMA(HistoStoch1, 4);
				SMA4					= SMA(HistoStoch1, 8);
				
				MACDdiff				= MACD(17,72,34);
				SMA5					= SMA(MACDdiff.Diff, 4);
				SMA6					= SMA(MACDdiff.Diff, 8);
				
				Draw.TextFixed(this,"Robo", "MACDHistoStochEMAv1", TextPosition.BottomLeft);
				
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
			
					
			if ((ToTime(Time[0]) <= 190000 && ToTime(Time[0]) >= 143000))
			{
				Draw.TextFixed(this,"Info", "Paused", TextPosition.TopRight);
				
				if (orderId.Length > 0)
				{
				  	AtmStrategyCancelEntryOrder(orderId);
					orderId = string.Empty;
				  	Print(Time[0] +" - MACDHistoStochEMAv1 - "+ Instrument.MasterInstrument + " - Todas Ordens Canceladas");
				}
				if (atmStrategyId.Length > 0)
				{
					AtmStrategyClose(atmStrategyId);
					atmStrategyId = string.Empty;
					Print(Time[0] +" - MACDHistoStochEMAv1 - "+ Instrument.MasterInstrument + " - Todas Posições Fechadas");
				}
			// Reset da Estrategia
				if (!reseted)
				{
					Print(Time[0] +" - MACDHistoStochEMAv1 - "+ Instrument.MasterInstrument + " - End Of Day PnL = " + dailyPnL);
					
					StrategyReset();
				}
				return;
			}
			
			if(goodToGo)
        	{			
						reseted = false;
						Draw.TextFixed(this,"Info", "Running", TextPosition.TopRight);
						if (orderId.Length == 0 && atmStrategyId.Length == 0)
							Draw.TextFixed(this,"Robo", "MACDHistoStochEMAv1", TextPosition.BottomLeft);
						
						if (setas)
						{						
							setaCompra = (int.Parse(setaCompra) + 1).ToString();
        					setaVenda = (int.Parse(setaVenda) + 1).ToString(); 
							
							setas = false;
						}
							
						//Meta Diaria
						if (dailyPnL <= (-DayLossStop))
							{
								Print(Time[0] +" - MACDHistoStochEMAv1 - "+ Instrument.MasterInstrument + " - Meta Perda Diaria");
								goodToGo = false;
								return;
							}
						if (dailyPnL >= DayGainStop)
							{
								Print(Time[0] +" - MACDHistoStochEMAv1 - "+ Instrument.MasterInstrument + " - Meta Ganho Diario");
								goodToGo = false;
								return;
							}
							
						//Saida	
						if (atmStrategyId.Length > 0)
							{
								if (comprado 
									&& (CrossBelow(SMA5, SMA6, 1))
									)
									
									{
										AtmStrategyClose(atmStrategyId);
										ScreenUpdate();
										atmStrategyId = string.Empty;
										//return;
									}
									
								if (vendido 
									&& (CrossAbove(SMA5, SMA6, 1))
									)
									
									{
										AtmStrategyClose(atmStrategyId);
										ScreenUpdate();
										atmStrategyId = string.Empty;
										//return;
									}
							}
							
						//if ((ToTime(Time[0]) <= 110000 && ToTime(Time[0]) >= 070000))
						//	return;
							
						
					#region MACDHistoStochEMA Strategy	
			            //Compra
						if 	((orderId.Length == 0 && atmStrategyId.Length == 0)
							
							&& (Close[0] > Open[0]) 
							&& (Close[1] > Open[1])
							&& (Open[1] == Open[2])
							&& (Close[0] > SMA1[0])
							
							&& (SMA1[0] > SMA2[0])
							&& (SMA1[1] > SMA2[1])
							&& (SMA1[2] > SMA2[2])
							
							&& (Close[1] - SMA1[1] <= (2 * RenkoBox * TickSize))
							
							//&& (HistoStoch1[0] > HistoStoch1[1])
							
							&& (SMA3[0] > SMA4[0]) 
							&& (SMA3[1] > SMA4[1]) 
							&& (SMA3[2] > SMA4[2])
							
							&& (MACDdiff.Diff[0] > MACDdiff.Diff[1])
							
							&& (SMA5[0] > SMA6[0]) 
							&& (SMA5[1] > SMA6[1]) 
							&& (SMA5[2] > SMA6[2])
							)
						{
							preco = Close[0];
							isAtmStrategyCreated = false;  // reset atm strategy created check to false
							atmStrategyId = GetAtmStrategyUniqueId();
							orderId = GetAtmStrategyUniqueId();
							AtmStrategyCreate(OrderAction.Buy, OrderType.Market, preco, 0, TimeInForce.Day, orderId, "ATM_HistoStochEMA", atmStrategyId, (atmCallbackErrorCode, atmCallBackId) => {
								//check that the atm strategy create did not result in error, and that the requested atm strategy matches the id in callback
								if (atmCallbackErrorCode == ErrorCode.NoError && atmCallBackId == atmStrategyId)
									isAtmStrategyCreated = true;
							});
								comprado = true;
								vendido = false;
								setas = true;
								
							Print(Time[0] + " - " + Instrument.MasterInstrument + " Compra a " + preco);
							Draw.ArrowUp(this, setaCompra, true, 0, Low[0] - TickSize, Brushes.Green);
							Draw.TextFixed(this,"Robo", "MACDHistoStochEMAv1 - Compra", TextPosition.BottomLeft);
						}
						// Venda
						if ((orderId.Length == 0 && atmStrategyId.Length == 0)
														
							&& (Close[0] < Open[0]) 
							&& (Close[1] < Open[1])
							&& (Open[1] == Open[2])
							&& (Close[0] < SMA1[0])
							
							&& (SMA1[0] < SMA2[0])
							&& (SMA1[1] < SMA2[1])
							&& (SMA1[2] < SMA2[2])
							
							&& (SMA1[1] - Close[1] <= (2 * RenkoBox * TickSize))
							
							//&& (HistoStoch1[0] < HistoStoch1[1])
							
							&& (SMA3[0] < SMA4[0]) 
							&& (SMA3[1] < SMA4[1]) 
							&& (SMA3[2] < SMA4[2])
							
							&& (MACDdiff.Diff[0] < MACDdiff.Diff[1])
							
							&& (SMA5[0] < SMA6[0]) 
							&& (SMA5[1] < SMA6[1]) 
							&& (SMA5[2] < SMA6[2])
							)
						{
							preco = Close[0];
							isAtmStrategyCreated = false;  // reset atm strategy created check to false
							atmStrategyId = GetAtmStrategyUniqueId();
							orderId = GetAtmStrategyUniqueId();
							AtmStrategyCreate(OrderAction.Sell, OrderType.Market, preco, 0, TimeInForce.Day, orderId, "ATM_HistoStochEMA", atmStrategyId, (atmCallbackErrorCode, atmCallBackId) => {
								//check that the atm strategy create did not result in error, and that the requested atm strategy matches the id in callback
								if (atmCallbackErrorCode == ErrorCode.NoError && atmCallBackId == atmStrategyId)
									isAtmStrategyCreated = true;
							});
								vendido = true;
								comprado = false;
								setas = true;
								
							Print(Time[0] + " - " + Instrument.MasterInstrument + " Venda a " + preco);
							Draw.ArrowDown(this, setaVenda, true, 0, High[0] + TickSize, Brushes.Red);
							Draw.TextFixed(this,"Robo", "MACDHistoStochEMAv1 - Venda", TextPosition.BottomLeft);
						}
						#endregion
						
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
						Print(Time[0] +" - MACDHistoStochEMAv1 - "+ Instrument.MasterInstrument + " - State: " + status[2]);
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
			Draw.TextFixed(this,"PnL", "Daily PnL = " + dailyPnL, TextPosition.BottomRight);
		}
		
		public void StrategyReset()
		{
			goodToGo = true;
			reseted = true;
			dailyPnL = 0;
			currentPnL = 0;
			Draw.TextFixed(this,"PnL", "Daily PnL = " + dailyPnL, TextPosition.BottomRight);
			Draw.TextFixed(this,"Info", "Reset", TextPosition.TopRight);
		}
		
		public void CancelamentoDeOrdem()
		{
				
				currAsk = GetCurrentAsk(0);
				currBid = GetCurrentBid(0);
					
					if (((GetAtmStrategyMarketPosition(atmStrategyId) == Cbi.MarketPosition.Flat) && (comprado == true && (currBid - Cancelamento * TickSize) >= preco)) 
						|| ((GetAtmStrategyMarketPosition(atmStrategyId) == Cbi.MarketPosition.Flat) && (vendido == true && (currAsk + Cancelamento * TickSize) <= preco)))
					{
							AtmStrategyCancelEntryOrder(orderId);
							Print(Time[0] + " - MACDHistoStochEMAv1 - "+ Instrument.MasterInstrument + " - Ordem CANCELADA");
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
			    Print("MACDHistoStochEMAv1 - "+ Instrument.MasterInstrument + " - Connected at " + DateTime.Now);
				Draw.TextFixed(this,"Robo", "MACDHistoStochEMAv1", TextPosition.BottomLeft);
				  if (atmStrategyId.Length > 0)
				  {
					AtmStrategyClose(atmStrategyId);
					atmStrategyId = string.Empty;
					Print("MACDHistoStochEMAv1 - "+ Instrument.MasterInstrument + " - Todas ATMs Fechadas");
				  }
				  
				  if (orderId.Length > 0)
					{
						AtmStrategyCancelEntryOrder(orderId);
						orderId = string.Empty;
						Print("MACDHistoStochEMAv1 - "+ Instrument.MasterInstrument + " - Todas Ordens Canceladas");
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
					Print("MACDHistoStochEMAv1 - "+ Instrument.MasterInstrument + " - Todas Posições Fechadas");
				  }
			  }
			  
			  else if(connectionStatusUpdate.Status == ConnectionStatus.ConnectionLost)
			  {
			    Print("MACDHistoStochEMAv1 - "+ Instrument.MasterInstrument + " - Connection lost at: " + DateTime.Now);
				Draw.TextFixed(this,"Robo", "MACDHistoStochEMAv1 - Connection Loss", TextPosition.BottomLeft);
				if (orderId.Length > 0)
				{
				  AtmStrategyCancelEntryOrder(orderId);
				  orderId = string.Empty;
				  Print("MACDHistoStochEMAv1 - "+ Instrument.MasterInstrument + " - Todas Ordens Canceladas");
				}
			  }
			}
        #endregion
		
			
		#region Properties
			[NinjaScriptProperty]
			[Range(1, int.MaxValue)]
			[Display(Name="RenkoBox", Order=1, GroupName="Parameters")]
			public int RenkoBox
			{ get; set; }

			[NinjaScriptProperty]
			[Range(1, int.MaxValue)]
			[Display(Name="Cancelamento", Order=2, GroupName="Parameters")]
			public int Cancelamento
			{ get; set; }

			[NinjaScriptProperty]
			[Range(1, int.MaxValue)]
			[Display(Name="Meta Gain", Order=3, GroupName="Parameters")]
			public int DayGainStop
			{ get; set; }
			
			[NinjaScriptProperty]
			[Range(1, int.MaxValue)]
			[Display(Name="Meta Loss", Order=4, GroupName="Parameters")]
			public int DayLossStop
			{ get; set; }
		
			#endregion

	}
}
