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
	public class EMAonEMAcomATMv7 : Strategy
	{
		private EMA EMA1;
		private EMA EMA2;
		private EMA EMA3;
		private EMA EMA4;
		
		private string  atmStrategyId			= string.Empty;
		private string  orderId					= string.Empty;
		private bool	isAtmStrategyCreated	= false;
		private bool	comprado				= false;
		private bool	vendido					= false;
		
		private double preco;
		private double currAsk;
		private double currBid;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Estrategia com ATM, só funciona em Real-Time data ou Market Replay";
				Name										= "EMAonEMAcomATMv7";
				Calculate									= Calculate.OnBarClose;
				EntriesPerDirection							= 1;
				EntryHandling								= EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy				= true;
				ExitOnSessionCloseSeconds					= 2700;
				IsFillLimitOnTouch							= false;
				MaximumBarsLookBack							= MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution							= OrderFillResolution.High;
				OrderFillResolutionType						= BarsPeriodType.Tick;
				OrderFillResolutionValue					= 1;
				Slippage									= 0;
				StartBehavior								= StartBehavior.WaitUntilFlat;
				TimeInForce									= TimeInForce.Day;
				TraceOrders									= true;
				RealtimeErrorHandling						= RealtimeErrorHandling.StopCancelCloseIgnoreRejects;
				StopTargetHandling							= StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade							= 20;
				ConnectionLossHandling 						= ConnectionLossHandling.KeepRunning;
				//DisconnectDelaySeconds 						= 300;
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration	= true;
				MainEMA					= 9;
				SecEMA					= 34;
				CrossEMA				= 17;
				
				Distancia				= 1;
				Cancelamento			= 9;
				
				OpenSession				= DateTime.Parse("01:00", System.Globalization.CultureInfo.InvariantCulture);
				LastEntry				= DateTime.Parse("14:00", System.Globalization.CultureInfo.InvariantCulture);
				CloseSession			= DateTime.Parse("15:00", System.Globalization.CultureInfo.InvariantCulture);

				OpenBloodHour1			= DateTime.Parse("08:50", System.Globalization.CultureInfo.InvariantCulture);
				CloseBloodHour1			= DateTime.Parse("09:20", System.Globalization.CultureInfo.InvariantCulture);
				OpenBloodHour2			= DateTime.Parse("10:50", System.Globalization.CultureInfo.InvariantCulture);
				CloseBloodHour2			= DateTime.Parse("11:20", System.Globalization.CultureInfo.InvariantCulture);				
				
				/*
				DayGainStop				= 500;
				DayLossStop				= 300;
				*/
			}
			else if (State == State.DataLoaded)
			{				
				EMA1				= EMA(Close, MainEMA);
				EMA2				= EMA(EMA1, SecEMA);
				EMA3				= EMA(Close, CrossEMA);
				EMA4				= EMA(Open, CrossEMA);
				
				EMA1.Plots[0].Brush = Brushes.Cyan;
				AddChartIndicator(EMA1);
				EMA2.Plots[0].Brush = Brushes.DarkCyan;
				AddChartIndicator(EMA2);
				EMA3.Plots[0].Brush = Brushes.Magenta;
				AddChartIndicator(EMA3);
				EMA4.Plots[0].Brush = Brushes.DarkMagenta;
				AddChartIndicator(EMA4);
			}
		}

		protected override void OnBarUpdate()
		{
						
			if ((Times[0][0].TimeOfDay >= CloseSession.TimeOfDay) || (Times[0][0].TimeOfDay <= OpenSession.TimeOfDay))
			{
				if (orderId.Length > 0)
				{
				  AtmStrategyCancelEntryOrder(orderId);
				  Print(Time[0] + " " + Instrument.FullName + " Todas Ordens Canceladas");
				}
				if (atmStrategyId.Length > 0)
				{
					AtmStrategyClose(atmStrategyId);
					atmStrategyId = string.Empty;
					Print(Time[0] + " " + Instrument.FullName + " Todas Posições Fechadas");
				}
				return;
			}	
			
			if (CurrentBar < BarsRequiredToTrade)
				return;

			// Make sure this strategy does not execute against historical data
			if(State == State.Historical)
				return;

			/*
			// Profit and Loss performance
			var currProfit = SystemPerformance.RealTimeTrades.TradesPerformance.GrossProfit;
			var currLoss = SystemPerformance.RealTimeTrades.TradesPerformance.GrossLoss;
			
			if ((currLoss >= DayLossStop) || (currProfit >= DayGainStop))
			{
				Print(Time[0] + " " + Instrument.FullName + " Meta Ganho/Perda");
				return;
			}
		    */
			
			//Hora Sangrenta
			if ((Times[0][0].TimeOfDay >= OpenBloodHour1.TimeOfDay) && (Times[0][0].TimeOfDay <= CloseBloodHour1.TimeOfDay))
			{
			    Print(Time[0] + " " + Instrument.FullName + " Hora Sangrenta 1");
				return;
			}

			if ((Times[0][0].TimeOfDay >= OpenBloodHour2.TimeOfDay) && (Times[0][0].TimeOfDay <= CloseBloodHour2.TimeOfDay))
			{
			    Print(Time[0] + " " + Instrument.FullName + " Hora Sangrenta 2");
				return;
			}

			//Ultima Entrada
			if ((Times[0][0].TimeOfDay >= LastEntry.TimeOfDay) && (Times[0][0].TimeOfDay <= CloseSession.TimeOfDay))
			{
			    Print(Time[0] + " " + Instrument.FullName + " Fechamento");
				Print(Time[0] + " " + Instrument.FullName + " Sem mais Entradas Hoje");

				if (orderId.Length > 0)
				{
				  AtmStrategyCancelEntryOrder(orderId);
				  Print(Time[0] + " " + Instrument.FullName + " Todas Ordens Canceladas");
				}
				return;
			}
			
			// Compra Longa
			if 	((orderId.Length == 0 && atmStrategyId.Length == 0)
				&& (EMA1[1] > EMA2[1]) && (EMA3[0] > EMA4[0]) && (EMA1[1] > EMA3[1]) && (Close[0] > Open[0]) && (Open[1] == Open[2]))
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
				&& (EMA1[1] < EMA2[1]) && (EMA3[0] < EMA4[0]) && (EMA1[1] < EMA3[1]) && (Close[0] < Open[0]) && (Open[1] == Open[2]))
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
				&& (EMA1[1] > EMA2[1]) && (EMA3[0] > EMA4[0]) && (EMA1[1] > EMA3[1]) && (Close[0] > Open[0]) && (Close[1] > Open[1]))
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
				&& (EMA1[1] < EMA2[1]) && (EMA3[0] < EMA4[0]) && (EMA1[1] < EMA3[1]) && (Close[0] < Open[0]) && (Close[1] < Open[1]))
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
				atmStrategyId = string.Empty;

			//Cancelaqmento de Ordem nao executada
			if (atmStrategyId.Length > 0)
			{
				currAsk = GetCurrentAsk(0);
				currBid = GetCurrentBid(0);
					
					if (((GetAtmStrategyMarketPosition(atmStrategyId) == Cbi.MarketPosition.Flat) && (comprado == true && (currBid - Cancelamento * TickSize) >= preco)) 
						|| ((GetAtmStrategyMarketPosition(atmStrategyId) == Cbi.MarketPosition.Flat) && (vendido == true && (currAsk + Cancelamento * TickSize) <= preco)))
					{
							AtmStrategyCancelEntryOrder(orderId);
							Print(Time[0] + " " + Instrument.FullName + " Ordem de Compra Cancelada");
							atmStrategyId = string.Empty;
							orderId = string.Empty;
							comprado = false;
							vendido = false;
					}
			}
				
		}
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
		[Display(Name="OpenSession", Description="Horario de Inicio", Order=6, GroupName="Parameters")]
		public DateTime OpenSession
		{ get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="CloseSession", Description="Horario de Fechamento", Order=7, GroupName="Parameters")]
		public DateTime CloseSession
		{ get; set; }
		
		/*
		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name="Ganho Satisfatório", Order=8, GroupName="Parameters")]
		public double DayGainStop
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name="Perda Máxima", Order=9, GroupName="Parameters")]
		public double DayLossStop
		{ get; set; }
		*/
		
		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="Inicio Hora Sangrenta 1", Description="Horario de Inicio", Order=10, GroupName="Parameters")]
		public DateTime OpenBloodHour1
		{ get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="Termino Hora Sangrenta 1", Description="Horario de Fechamento", Order=11, GroupName="Parameters")]
		public DateTime CloseBloodHour1
		{ get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="Inicio Hora Sangrenta 2", Description="Horario de Inicio", Order=12, GroupName="Parameters")]
		public DateTime OpenBloodHour2
		{ get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="Termino Hora Sangrenta 2", Description="Horario de Fechamento", Order=13, GroupName="Parameters")]
		public DateTime CloseBloodHour2
		{ get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="Ultima Entrada", Description="Proximo ao Fechamento", Order=14, GroupName="Parameters")]
		public DateTime LastEntry
		{ get; set; }

		#endregion

	}
}
