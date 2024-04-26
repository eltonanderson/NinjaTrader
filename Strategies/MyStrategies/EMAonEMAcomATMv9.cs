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
	public class EMAonEMAcomATMv9 : Strategy
	{
		private EMA EMA1;
		private EMA EMA2;
		private EMA EMA3;
		private EMA EMA4;
		private EMA EMA5;
		private EMA EMA6;
		
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

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Estrategia com ATM, só funciona em Real-Time data ou Market Replay";
				Name										= "EMAonEMAcomATMv9";
				Calculate									= Calculate.OnBarClose;
				EntriesPerDirection							= 1;
				EntryHandling								= EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy				= true;
				ExitOnSessionCloseSeconds					= 2700;
				IsFillLimitOnTouch							= false;
				MaximumBarsLookBack							= MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution							= OrderFillResolution.Standard;
				OrderFillResolutionType						= BarsPeriodType.Tick;
				OrderFillResolutionValue					= 1;
				Slippage									= 0;
				StartBehavior								= StartBehavior.WaitUntilFlat;
				TimeInForce									= TimeInForce.Day;
				TraceOrders									= true;
				RealtimeErrorHandling						= RealtimeErrorHandling.StopCancelCloseIgnoreRejects;
				StopTargetHandling							= StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade							= 2;
				ConnectionLossHandling 						= ConnectionLossHandling.KeepRunning;
				IncludeTradeHistoryInBacktest				= true;
			
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration	= true;
				MainEMA					= 9;
				SecEMA					= 34;
				CrossEMA				= 17;
				
				Distancia				= 1;
				Cancelamento			= 9;
				
				OpenSession				= DateTime.Parse("05:00", System.Globalization.CultureInfo.InvariantCulture);
				CloseSession			= DateTime.Parse("15:00", System.Globalization.CultureInfo.InvariantCulture);

				LastEntry				= DateTime.Parse("14:00", System.Globalization.CultureInfo.InvariantCulture);
				
				OpenBloodHour			= DateTime.Parse("09:00", System.Globalization.CultureInfo.InvariantCulture);
				CloseBloodHour			= DateTime.Parse("09:55", System.Globalization.CultureInfo.InvariantCulture);				
				
				
				DayGainStop				= 300.0;
				DayLossStop				= 500.0;
				
			}
			
			else if (State == State.Configure)
			{
				// Add a Renko 7R
				AddRenko(Instrument.FullName, 7, MarketDataType.Last);
			}
			
			else if (State == State.DataLoaded)
			{				
				EMA1				= EMA(Close, MainEMA);
				EMA2				= EMA(EMA1, SecEMA);
				EMA3				= EMA(Close, CrossEMA);
				EMA4				= EMA(Open, CrossEMA);
				
				EMA5 				= EMA(Closes[1], 9);
				EMA6 				= EMA(Closes[1], 12);
				
				EMA1.Plots[0].Brush = Brushes.Cyan;
				AddChartIndicator(EMA1);
				EMA2.Plots[0].Brush = Brushes.DarkCyan;
				AddChartIndicator(EMA2);
				EMA3.Plots[0].Brush = Brushes.Magenta;
				AddChartIndicator(EMA3);
				EMA4.Plots[0].Brush = Brushes.DarkMagenta;
				AddChartIndicator(EMA4);
				
                Draw.TextFixed(this,"Robo", "EMAonEMAcomATMv9", TextPosition.BottomLeft);
				
                StrategyReset();
				
			}
		}

		protected override void OnBarUpdate()
		{
			
			if (CurrentBar < BarsRequiredToTrade)
				return;

			// Make sure this strategy does not execute against historical data
			if(State == State.Historical)
				return;
			/*
			if(CurrentBar == TradingHours.PartialHolidays)
			{
				Print(Time[0] + " " + Instrument.FullName + " Feriado");
				return;
			}
			*/
			
            /*
			if(Time[0].DayOfWeek == DayOfWeek.Monday)
			{
                 Print(Time[0] + " " + Instrument.FullName + " Segunda-Feira");
				 return;
			}
                
            if(Time[0].DayOfWeek == DayOfWeek.Friday)
			{
                Print(Time[0] + " " + Instrument.FullName + " Sexta-Feira");
                return;
            }
			*/
					
			if ((Times[0][0].TimeOfDay <= OpenSession.TimeOfDay) || (Times[0][0].TimeOfDay >= CloseSession.TimeOfDay))
			{
				if (orderId.Length > 0)
				{
				  	AtmStrategyCancelEntryOrder(orderId);
					orderId = string.Empty;
				  	Print(Time[0] + " " + Instrument.FullName + " Todas Ordens Canceladas");
				}
				if (atmStrategyId.Length > 0)
				{
					AtmStrategyClose(atmStrategyId);
					atmStrategyId = string.Empty;
					Print(Time[0] + " " + Instrument.FullName + " Todas Posições Fechadas");
				}
			// Reset da Estrategia
				StrategyReset();
				return;
			}
	#region GoodToGo	
		if(goodToGo)
			{
			     //Meta Diaria
				 //ScreenUpdate();
				if (dailyPnL <= (-DayLossStop))
					{
						Print(Time[0] + " " + Instrument.FullName + " Meta Perda Diaria");
						goodToGo = false;
						return;
					}
				if (dailyPnL >= DayGainStop)
					{
						Print(Time[0] + " " + Instrument.FullName + " Meta Ganho Diario");
						goodToGo = false;
						return;
					}
					
			//Hora Sangrenta
			if ((Times[0][0].TimeOfDay >= OpenBloodHour.TimeOfDay) && (Times[0][0].TimeOfDay <= CloseBloodHour.TimeOfDay))
				{
						//ScreenUpdate();
						Print(Time[0] + " " + Instrument.FullName + " Hora Sangrenta!");
										
						if (orderId.Length > 0)
						{
						  	AtmStrategyCancelEntryOrder(orderId);
							orderId = string.Empty;
						  	Print(Time[0] + " " + Instrument.FullName + " Ordem CANCELADA");
						}
						return;
				}
			
			
			//Ultima Entrada
			if ((Times[0][0].TimeOfDay >= LastEntry.TimeOfDay) && (Times[0][0].TimeOfDay <= CloseSession.TimeOfDay))
			{
			    ScreenUpdate();
				Print(Time[0] + " " + Instrument.FullName + " Fechamento Próximo");
				Print(Time[0] + " " + Instrument.FullName + " Sem mais Entradas Hoje");

				if (orderId.Length > 0)
				{
				  	AtmStrategyCancelEntryOrder(orderId);
					orderId = string.Empty;
				  	Print(Time[0] + " " + Instrument.FullName + " Ordem CANCELADA");
				}
                goodToGo = false;
				return;
			}

            // Compra Longa
			if 	((orderId.Length == 0 && atmStrategyId.Length == 0)
				&& (EMA1[1] > EMA2[1]) && (EMA3[0] > EMA4[0]) && (EMA1[1] > EMA3[1]) && (Close[0] > Open[0]) && (Open[1] == Open[2]) && (EMA5[0] > EMA6[0]))
			{
				isAtmStrategyCreated = false;  // reset atm strategy created check to false
				atmStrategyId = GetAtmStrategyUniqueId();
				orderId = GetAtmStrategyUniqueId();
				AtmStrategyCreate(OrderAction.Buy, OrderType.Limit, (GetCurrentBid(0) - Distancia * TickSize), 0, TimeInForce.Gtc, orderId, "ATM_Trailing2Stages", atmStrategyId, (atmCallbackErrorCode, atmCallBackId) => {
					//check that the atm strategy create did not result in error, and that the requested atm strategy matches the id in callback
					if (atmCallbackErrorCode == ErrorCode.NoError && atmCallBackId == atmStrategyId)
						isAtmStrategyCreated = true;
				});
					comprado = true;
					vendido = false;
					preco = (GetCurrentBid(0) - Distancia * TickSize);
					Print(Time[0] + " " + Instrument.FullName + " Ordem de Compra Longa Pendente");
			}
			// Venda Longa
			if ((orderId.Length == 0 && atmStrategyId.Length == 0)
				&& (EMA1[1] < EMA2[1]) && (EMA3[0] < EMA4[0]) && (EMA1[1] < EMA3[1]) && (Close[0] < Open[0]) && (Open[1] == Open[2]) && (EMA5[0] < EMA6[0]))
			{
				isAtmStrategyCreated = false;  // reset atm strategy created check to false
				atmStrategyId = GetAtmStrategyUniqueId();
				orderId = GetAtmStrategyUniqueId();
				AtmStrategyCreate(OrderAction.Sell, OrderType.Limit, (GetCurrentAsk(0) + Distancia * TickSize), 0, TimeInForce.Gtc, orderId, "ATM_Trailing2Stages", atmStrategyId, (atmCallbackErrorCode, atmCallBackId) => {
					//check that the atm strategy create did not result in error, and that the requested atm strategy matches the id in callback
					if (atmCallbackErrorCode == ErrorCode.NoError && atmCallBackId == atmStrategyId)
						isAtmStrategyCreated = true;
				});
					vendido = true;
					comprado = false;
					preco = (GetCurrentAsk(0) + Distancia * TickSize);
					Print(Time[0] + " " + Instrument.FullName + " Ordem de Venda Longa Pendente");
			}
			
			// Compra Curta
			if ((orderId.Length == 0 && atmStrategyId.Length == 0)
				&& (EMA1[1] > EMA2[1]) && (EMA3[0] > EMA4[0]) && (EMA1[1] > EMA3[1]) && (Close[0] > Open[0]) && (Close[1] > Open[1]) && (EMA5[0] > EMA6[0]))
			{
				isAtmStrategyCreated = false;  // reset atm strategy created check to false
				atmStrategyId = GetAtmStrategyUniqueId();
				orderId = GetAtmStrategyUniqueId();
				AtmStrategyCreate(OrderAction.Buy, OrderType.Limit, (GetCurrentBid(0) - Distancia * TickSize), 0, TimeInForce.Gtc, orderId, "ATM_Trailing1Stages", atmStrategyId, (atmCallbackErrorCode, atmCallBackId) => {
					//check that the atm strategy create did not result in error, and that the requested atm strategy matches the id in callback
					if (atmCallbackErrorCode == ErrorCode.NoError && atmCallBackId == atmStrategyId)
						isAtmStrategyCreated = true;
				});
					comprado = true;
					vendido = false;
					preco = (GetCurrentBid(0) - Distancia * TickSize);
					Print(Time[0] + " " + Instrument.FullName + " Ordem de Compra Curta Pendente");
			}
			// Venda Curta
			if ((orderId.Length == 0 && atmStrategyId.Length == 0)
				&& (EMA1[1] < EMA2[1]) && (EMA3[0] < EMA4[0]) && (EMA1[1] < EMA3[1]) && (Close[0] < Open[0]) && (Close[1] < Open[1]) && (EMA5[0] < EMA6[0]))
			{
				isAtmStrategyCreated = false;  // reset atm strategy created check to false
				atmStrategyId = GetAtmStrategyUniqueId();
				orderId = GetAtmStrategyUniqueId();
				AtmStrategyCreate(OrderAction.Sell, OrderType.Limit, (GetCurrentAsk(0) + Distancia * TickSize), 0, TimeInForce.Gtc, orderId, "ATM_Trailing1Stages", atmStrategyId, (atmCallbackErrorCode, atmCallBackId) => {
					//check that the atm strategy create did not result in error, and that the requested atm strategy matches the id in callback
					if (atmCallbackErrorCode == ErrorCode.NoError && atmCallBackId == atmStrategyId)
						isAtmStrategyCreated = true;
				});
					vendido = true;
					comprado = false;
					preco = (GetCurrentAsk(0) + Distancia * TickSize);
					Print(Time[0] + " " + Instrument.FullName + " Ordem de Venda Curta Pendente");
			}
        }
	#endregion
			
			// Check that atm strategy was created before checking other properties
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
						Print(Time[0] + " " + Instrument.FullName + " State: " + status[2]);
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
				
				currAsk = GetCurrentAsk(0);
				currBid = GetCurrentBid(0);
					
					if (((GetAtmStrategyMarketPosition(atmStrategyId) == Cbi.MarketPosition.Flat) && (comprado == true && (currBid - Cancelamento * TickSize) >= preco)) 
						|| ((GetAtmStrategyMarketPosition(atmStrategyId) == Cbi.MarketPosition.Flat) && (vendido == true && (currAsk + Cancelamento * TickSize) <= preco)))
					{
							AtmStrategyCancelEntryOrder(orderId);
							Print(Time[0] + " " + Instrument.FullName + " Ordem CANCELADA");
							atmStrategyId = string.Empty;
							orderId = string.Empty;
							comprado = false;
							vendido = false;
					}
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
		
		#region ConnectionHandling
		protected override void OnConnectionStatusUpdate(ConnectionStatusEventArgs connectionStatusUpdate)
			{
			if(connectionStatusUpdate.Status == ConnectionStatus.Connected)
			  {
			    Print(Time[0] + " " + Instrument.FullName + " Connected at " + DateTime.Now);
				  if (atmStrategyId.Length > 0)
				  {
					AtmStrategyClose(atmStrategyId);
					atmStrategyId = string.Empty;
					Print(Time[0] + " " + Instrument.FullName + " Todas ATMs Fechadas");
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
					Print(Time[0] + " " + Instrument.FullName + " Todas Posições Fechadas");
				  }
			  }
			  
			  else if(connectionStatusUpdate.Status == ConnectionStatus.ConnectionLost)
			  {
			    Print(Time[0] + " " + Instrument.FullName + " Connection lost at: " + DateTime.Now);
				if (orderId.Length > 0)
				{
				  AtmStrategyCancelEntryOrder(orderId);
				  orderId = string.Empty;
				  Print(Time[0] + " " + Instrument.FullName + " Todas Ordens Canceladas");
				}
			  }
			}
        #endregion

		#region Properties
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="EMA Principal", Order=1, GroupName="Parameters")]
		public int MainEMA
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="EMA Secundaria", Order=2, GroupName="Parameters")]
		public int SecEMA
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="EMA (Abertura - Fechamento)", Order=3, GroupName="Parameters")]
		public int CrossEMA
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="Distancia (Ordem Limite)", Order=4, GroupName="Parameters")]
		public int Distancia
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="Cancelamento (Se Nao Executado)", Order=5, GroupName="Parameters")]
		public int Cancelamento
		{ get; set; }
		
		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="Open Session", Description="Horario de Inicio", Order=6, GroupName="Parameters")]
		public DateTime OpenSession
		{ get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="Close Session", Description="Horario de Fechamento", Order=7, GroupName="Parameters")]
		public DateTime CloseSession
		{ get; set; }		
		
		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="Inicio Hora Sangrenta", Description="Horario de Inicio", Order=8, GroupName="Parameters")]
		public DateTime OpenBloodHour
		{ get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="Termino Hora Sangrenta", Description="Horario de Fechamento", Order=9, GroupName="Parameters")]
		public DateTime CloseBloodHour
		{ get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="LastEntry", Description="Proximo ao Fechamento", Order=10, GroupName="Parameters")]
		public DateTime LastEntry
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name="Ganho Satisfatório", Order=11, GroupName="Parameters")]
		public double DayGainStop
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name="Perda Máxima", Order=12, GroupName="Parameters")]
		public double DayLossStop
		{ get; set; }

		#endregion

	}
}
