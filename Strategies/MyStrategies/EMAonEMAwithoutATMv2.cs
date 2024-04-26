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
	public class EMAonEMAwithoutATMv2 : Strategy
	{
		private EMA EMA1;
		private EMA EMA2;
		private EMA EMA3;
		private EMA EMA4;
		private EMA EMA5;
		private EMA EMA6;
		
		private bool	entradaLonga			= false;
		private bool	entradaCurta			= false;
		private bool    goodToGo				= true;

		private bool 	isMIT 					= true;
		private bool	disconnection			= false;
		
		private double dailyPnL;
		private double entryPrice;

		private double priorTradesCumProfit = 0;
		

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Estrategia com ATM, só funciona em Real-Time data ou Market Replay.
																Ajustada para M6E Micro S&P.
																usando 2 tempos graficos de Renko ( 4R e 7R)";
				Name										= "EMAonEMAwithoutATMv2";
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
				BarsRequiredToTrade							= 1;
				ConnectionLossHandling 						= ConnectionLossHandling.KeepRunning;
				IncludeTradeHistoryInBacktest				= true;
				IncludeCommission 							= true;
				NumberRestartAttempts 						= 5;
				IsInstantiatedOnEachOptimizationIteration	= true;
				DaysToLoad 									= 10;
				
				MainEMA										= 9;
				SecEMA										= 34;
				CrossEMA									= 17;
				
				Distancia									= 1;
				Cancelamento								= 9;
				Quantidade									= 1;					
				
				DayGainStop									= 3000.0;
				DayLossStop									= 5000.0;
				
				renkoMaior									= 7;
				
				LongStopLossTicks 							= 12;
				LongProfitTargetTicks 						= 18;
				ShortStopLossTicks 							= 7;
				ShortProfitTargetTicks 						= 14;
				
				OpenSession				= DateTime.Parse("15:00", System.Globalization.CultureInfo.InvariantCulture);
				CloseSession			= DateTime.Parse("19:00", System.Globalization.CultureInfo.InvariantCulture);
				
			}
			
			else if (State == State.Configure)
			{
				// Add a Renko
				AddRenko(Instrument.FullName, renkoMaior, MarketDataType.Last);
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
				
                Draw.TextFixed(this,"Robo", Name, TextPosition.BottomLeft);
				Draw.TextFixed(this,"Info", "Script Started", TextPosition.BottomRight);
				
				ClearOutputWindow();
			}
			else if (State == State.Terminated)
			{
				Print("Daily PnL = " + dailyPnL);
				Print("Robo Parado");
			}
		}

		protected override void OnBarUpdate()
		{
			
			if (CurrentBar < BarsRequiredToTrade)
				return;

			if (BarsInProgress != 0)
				return;

			if(State == State.Historical)
				return;
				
			dailyPnL = SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit - priorTradesCumProfit;
			Draw.TextFixed(this, "Info", "Daily PnL = " + dailyPnL, TextPosition.BottomRight);

			if (dailyPnL >= DayGainStop || dailyPnL <= -(DayLossStop))
			{
				return;
			}
					
			if ((Times[0][0].TimeOfDay > OpenSession.TimeOfDay) && (Times[0][0].TimeOfDay < CloseSession.TimeOfDay))
			{
				StrategyReset();
				return;
			}
			
	#region GoodToGo	
		if(goodToGo)
			{				
						// Compra Longa
						if 	((Position.MarketPosition == MarketPosition.Flat)
							&& (EMA1[1] > EMA2[1]) && (EMA3[0] > EMA4[0]) && (EMA1[1] > EMA3[1]) && (Close[0] > Open[0]) && (Open[1] == Open[2]) && (EMA5[0] > EMA6[0]))
						{
							Print ("------------------------------------------------");
							SetStopLoss(CalculationMode.Ticks, LongStopLossTicks);
							SetProfitTarget(CalculationMode.Ticks, LongProfitTargetTicks, isMIT);
							//EnterShort();
							EnterLongMIT(Quantidade, Close[0]);
							//EnterLongLimit(Close[1]);


							entradaLonga 	= true;
							entradaCurta 	= false;
							
							Print(Time[0] + " " + Instrument.FullName + " Compra Longa");
							Print ("------------------------------------------------");
						}
						// Venda Longa
						if ((Position.MarketPosition == MarketPosition.Flat)
							&& (EMA1[1] < EMA2[1]) && (EMA3[0] < EMA4[0]) && (EMA1[1] < EMA3[1]) && (Close[0] < Open[0]) && (Open[1] == Open[2]) && (EMA5[0] < EMA6[0]))
						{
							Print ("------------------------------------------------");
							SetStopLoss(CalculationMode.Ticks, LongStopLossTicks);
							SetProfitTarget(CalculationMode.Ticks, LongProfitTargetTicks, isMIT);
							//EnterShort();
							EnterShortMIT(Quantidade, Close[0]);
							//EnterLongLimit(Close[1]);


							entradaLonga 	= true;
							entradaCurta 	= false;
							
							Print(Time[0] + " " + Instrument.FullName + " Venda Longa");
							Print ("------------------------------------------------");
						}
						
						// Compra Curta
						if ((Position.MarketPosition == MarketPosition.Flat)
							&& (EMA1[1] > EMA2[1]) && (EMA3[0] > EMA4[0]) && (EMA1[1] > EMA3[1]) && (Close[0] > Open[0]) && (Close[1] > Open[1]) && (EMA5[0] > EMA6[0]))
						{
							Print ("------------------------------------------------");
							SetStopLoss(CalculationMode.Ticks, ShortStopLossTicks);
							SetProfitTarget(CalculationMode.Ticks, ShortProfitTargetTicks, isMIT);
							//EnterShort();
							EnterLongMIT(Quantidade, Close[0]);
							//EnterLongLimit(Close[1]);


							entradaLonga 	= false;
							entradaCurta 	= true;
							
							Print(Time[0] + " " + Instrument.FullName + " Compra Curta");
							Print ("------------------------------------------------");
						}
						// Venda Curta
						if ((Position.MarketPosition == MarketPosition.Flat)
							&& (EMA1[1] < EMA2[1]) && (EMA3[0] < EMA4[0]) && (EMA1[1] < EMA3[1]) && (Close[0] < Open[0]) && (Close[1] < Open[1]) && (EMA5[0] < EMA6[0]))
						{
							Print ("------------------------------------------------");
							SetStopLoss(CalculationMode.Ticks, ShortStopLossTicks);
							SetProfitTarget(CalculationMode.Ticks, ShortProfitTargetTicks, isMIT);
							//EnterShort();
							EnterShortMIT(Quantidade, Close[0]);
							//EnterLongLimit(Close[1]);


							entradaLonga 	= false;
							entradaCurta 	= true;
							
							Print(Time[0] + " " + Instrument.FullName + " Venda Curta");
							Print ("------------------------------------------------");
						}

        }
	#endregion
			
		}
		
		protected override void OnMarketData(MarketDataEventArgs marketDataUpdate)
				{	
						
						
					
				}
				
		protected override void OnExecutionUpdate(Execution execution, string executionId, double price, int quantity, MarketPosition marketPosition, string orderId, DateTime time)
				{
							 
				      	Print ("------------------------------------------------");
						entryPrice = execution.Price;
						Print("Preço de Execução = " + entryPrice);
						Print("Quantidade = " + execution.Quantity);
						Print("Hora da Execução = " + execution.Time);
						Print ("------------------------------------------------");
						
				}
				
		protected override void OnPositionUpdate(Position position, double averagePrice, int quantity, MarketPosition marketPosition)
				{
						 if (position.MarketPosition == MarketPosition.Flat)
							  {
							    entradaLonga 	= false;
								entradaCurta 	= false;
								goodToGo		= true;
								  
								Print ("------------------------------------------------");
								Print(Time[0] + " " + Instrument.FullName + " Posição = Zerado");
								Print ("------------------------------------------------");
							  }
							  
						if (position.MarketPosition == MarketPosition.Long)
							  {
							    goodToGo		= false;
								  
								Print ("------------------------------------------------");
								Print(Time[0] + " " + Instrument.FullName + " Posição = Comprado");
								Print ("------------------------------------------------");
							  }
							  
						if (position.MarketPosition == MarketPosition.Short)
							  {
							    goodToGo		= false;
								  
								Print ("------------------------------------------------");
								Print(Time[0] + " " + Instrument.FullName + " Posição = Vendido");
								Print ("------------------------------------------------");
							  }
				} 
				
		protected override void OnOrderUpdate(Order order, double limitPrice, double stopPrice, int quantity, int filled, double averageFillPrice, OrderState orderState, DateTime time, ErrorCode error, string nativeError)
				{
							 
						if (order.OrderState == OrderState.Rejected)
							  {
							    Print ("------------------------------------------------");
								Print(Time[0] + " " + Instrument.FullName + " Ordem Rejeitada");
								Print ("------------------------------------------------");
							  }
							  
						if (order.OrderState == OrderState.Cancelled)
							  {
							    entradaLonga 	= false;
								entradaCurta 	= false;
								goodToGo		= true;
								  
								Print ("------------------------------------------------");
								Print(Time[0] + " " + Instrument.FullName + " Ordem Cancelada");
								Print ("------------------------------------------------");
							  }
							  
						if (order.OrderState == OrderState.Filled)
							  {
							    Print ("------------------------------------------------");
								Print(Time[0] + " " + Instrument.FullName + " Ordem Totalmente Execudata");
								Print ("------------------------------------------------");
							  }
						
				}

#region StrategyReset
			public void StrategyReset()
			{
				entradaLonga 	= false;
				entradaCurta 	= false;
				goodToGo		= true;
				
				if (Bars.IsFirstBarOfSession) 
					priorTradesCumProfit = SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit;
				
				dailyPnL = SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit - priorTradesCumProfit;
				Draw.TextFixed(this,"Info", "Daily PnL = " + dailyPnL, TextPosition.BottomRight);
			}
#endregion
		
#region ConnectionHandling
			protected override void OnConnectionStatusUpdate(ConnectionStatusEventArgs connectionStatusUpdate)
			{
			if(connectionStatusUpdate.Status == ConnectionStatus.Connected)
			  {
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
					goodToGo = false;
				  }
				  
				  if (disconnection)
				  {
					Print(Time[0] + " " + Instrument.FullName + " Robo Parado por Perda de Conexão");
					SetState(State.Terminated);
    				return;
				  }
			  }
			  
			  else if(connectionStatusUpdate.Status == ConnectionStatus.ConnectionLost)
			  {
				  disconnection = true;			
			  }
			}
#endregion

#region Properties
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="EMA Principal", Order=1, GroupName="Indicadores")]
		public int MainEMA
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="EMA Secundaria", Order=2, GroupName="Indicadores")]
		public int SecEMA
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="EMA (Abertura - Fechamento)", Order=3, GroupName="Indicadores")]
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
		[Range(0, int.MaxValue)]
		[Display(Name="Quantidade", Description="Quantidade de Contratos", Order=6, GroupName="Parameters")]
		public int Quantidade
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="Tempo Gráfico Maior (Renko)", Description="Tempo Gráfico Maior", Order=7, GroupName="Parameters")]
		public int renkoMaior
		{ get; set; }
		
		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="Open Session", Description="Horario de Inicio", Order=7, GroupName="Horário")]
		public DateTime OpenSession
		{ get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="Close Session", Description="Horario de Fechamento", Order=8, GroupName="Horário")]
		public DateTime CloseSession
		{ get; set; }		
		
		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="Inicio Hora Sangrenta", Description="Horario de Inicio", Order=9, GroupName="Horário")]
		public DateTime OpenBloodHour
		{ get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="Termino Hora Sangrenta", Description="Horario de Fechamento", Order=10, GroupName="Horário")]
		public DateTime CloseBloodHour
		{ get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="LastEntry", Description="Proximo ao Fechamento", Order=11, GroupName="Horário")]
		public DateTime LastEntry
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name="Ganho Satisfatório", Order=12, GroupName="Meta Diária")]
		public double DayGainStop
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name="Perda Máxima", Order=13, GroupName="Meta Diária")]
		public double DayLossStop
		{ get; set; }
		
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Stop Longo", Order=14, GroupName="Alvos")]
		public int LongStopLossTicks
		{ get; set; }
			
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Alvo Longo", Order=15, GroupName="Alvos")]
		public int LongProfitTargetTicks
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Stop Curto", Order=16, GroupName="Alvos")]
		public int ShortStopLossTicks
		{ get; set; }
			
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Alvo Curto", Order=17, GroupName="Alvos")]
		public int ShortProfitTargetTicks
		{ get; set; }

#endregion

	}
}
