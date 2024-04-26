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
	public class FullTimeSeriesStochRenko : Strategy
	{
		private StochasticsFast StochasticsFast1;
		
		private StochasticsFast StochasticsFast2;
		private StochasticsFast StochasticsFast3;
		private StochasticsFast StochasticsFast4;
		private StochasticsFast StochasticsFast5;
		private StochasticsFast StochasticsFast6;
		
		private EMA EMA1;
		private MACD MACD1;
		
		
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
				Description									= @"3 fractais em 3 tempos graficos 60, 15 e 4. Fractais acima de 61.8 ou abaixo de 38.2 confirmam tendencia nos tempos maiores. Entradas no grafico de 4 minutos. Compra pequena cruzando 38.2 e aumenta a mao cruzando 61.8. E o inverso para a venda.";
				Name										= "FullTimeSeriesStochRenko";
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
				Fractal1				= 17;
				Fractal2				= 72;
				Fractal3			    = 305;
				LinhaS					= 50;
				LinhaI				    = 50;
				
				Distancia				= 1;
				Cancelamento			= 9;
				
				OpenSession1			= DateTime.Parse("17:30", System.Globalization.CultureInfo.InvariantCulture);
				CloseSession1			= DateTime.Parse("15:00", System.Globalization.CultureInfo.InvariantCulture);
				
//				OpenSession2			= DateTime.Parse("09:55", System.Globalization.CultureInfo.InvariantCulture);
//				CloseSession2			= DateTime.Parse("13:00", System.Globalization.CultureInfo.InvariantCulture);
				
//				OpenSession3			= DateTime.Parse("13:00", System.Globalization.CultureInfo.InvariantCulture);
//				CloseSession3			= DateTime.Parse("15:00", System.Globalization.CultureInfo.InvariantCulture);

//				LastEntry				= DateTime.Parse("14:00", System.Globalization.CultureInfo.InvariantCulture);
				
//				OpenBloodHour			= DateTime.Parse("09:00", System.Globalization.CultureInfo.InvariantCulture);
//				CloseBloodHour			= DateTime.Parse("09:55", System.Globalization.CultureInfo.InvariantCulture);				
				
				
				DayGainStop				= 1500.0;
				DayLossStop				= 500.0;
				
			}
			
			else if (State == State.Configure)
			{
				AddDataSeries(Data.BarsPeriodType.Minute, 15);
				AddDataSeries(Data.BarsPeriodType.Minute, 60);
			}
			
			else if (State == State.DataLoaded)
			{				
				StochasticsFast1				= StochasticsFast(Closes[2], 3, Convert.ToInt32(Fractal1));
				StochasticsFast2				= StochasticsFast(Closes[2], 3, Convert.ToInt32(Fractal2));
				StochasticsFast3				= StochasticsFast(Closes[2], 3, Convert.ToInt32(Fractal3));
				StochasticsFast4				= StochasticsFast(Closes[1], 3, Convert.ToInt32(Fractal1));
				StochasticsFast5				= StochasticsFast(Closes[1], 3, Convert.ToInt32(Fractal2));
				StochasticsFast6				= StochasticsFast(Closes[1], 3, Convert.ToInt32(Fractal3));
				EMA1				= EMA(Close, 17);
				MACD1				= MACD(Close, 17, 72, 34);
				
                Draw.TextFixed(this,"Robo", "FullTimeSeriesStochRenko", TextPosition.BottomLeft);
				
                StrategyReset();
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < BarsRequiredToTrade)
				return;
			
			if (CurrentBars[0] < 1
			|| CurrentBars[1] < 1
			|| CurrentBars[2] < 1)
			return;

			// Make sure this strategy does not execute against historical data
			if(State == State.Historical)
				return;	
					
			if ((Times[0][0].TimeOfDay <= OpenSession1.TimeOfDay) && (Times[0][0].TimeOfDay >= CloseSession1.TimeOfDay))
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
			// Reset da Estrategia
				StrategyReset();
				return;
			}	
					
			//Hora Sangrenta
//			if ((Times[0][0].TimeOfDay >= OpenBloodHour.TimeOfDay) && (Times[0][0].TimeOfDay <= CloseBloodHour.TimeOfDay))
//				{
//						//ScreenUpdate();
//						Print(Time[0] + " " + Instrument.FullName + " Hora Sangrenta!");
										
//						if (orderId.Length > 0)
//						{
//						  AtmStrategyCancelEntryOrder(orderId);
//						  Print(Time[0] + " " + Instrument.FullName + " Ordem CANCELADA");
//						}
//						return;
//				}
			//Meta Diaria
			if(goodToGo)
			{
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
			}
			
			//Ultima Entrada
//			if (goodToGo && (Times[0][0].TimeOfDay >= LastEntry.TimeOfDay) && (Times[0][0].TimeOfDay <= CloseSession3.TimeOfDay))
//			{
//			    ScreenUpdate();
//				Print(Time[0] + " " + Instrument.FullName + " Fechamento Próximo");
//				Print(Time[0] + " " + Instrument.FullName + " Sem mais Entradas Hoje");

//				if (orderId.Length > 0)
//				{
//				  AtmStrategyCancelEntryOrder(orderId);
//				  Print(Time[0] + " " + Instrument.FullName + " Ordem CANCELADA");
//				}
//                goodToGo = false;
//				return;
//			}
        if(goodToGo)
        {
            // Compra Maior
			if 	((orderId.Length == 0 && atmStrategyId.Length == 0)
				 && (StochasticsFast1.K[0] > LinhaS)  // 60m - 17   > Linha Superior
				 && (StochasticsFast2.K[0] > LinhaS)  // 60m - 72   > Linha Superior
				 && (StochasticsFast3.K[0] > LinhaS)  // 60m - 305  > Linha Superior
				 && (StochasticsFast4.K[0] > LinhaS)  // 15m - 17   > Linha Superior
				 && (StochasticsFast5.K[0] > LinhaS)  // 15m - 72   > Linha Superior
				 && (StochasticsFast6.K[0] > LinhaS)  // 15m - 305  > Linha Superior
				 && (EMA1[0] > EMA1[1])
				 && (MACD1.Diff[0] > MACD1.Diff[1])
				 && (StochasticsFast4.K[0] > StochasticsFast4.K[1]))
			{
				isAtmStrategyCreated = false;  // reset atm strategy created check to false
				atmStrategyId = GetAtmStrategyUniqueId();
				orderId = GetAtmStrategyUniqueId();
				AtmStrategyCreate(OrderAction.Buy, OrderType.Limit, (GetCurrentBid(0) - Distancia * TickSize), 0, TimeInForce.Gtc, orderId, "ATM_TimeSeriesStoch2", atmStrategyId, (atmCallbackErrorCode, atmCallBackId) => {
					//check that the atm strategy create did not result in error, and that the requested atm strategy matches the id in callback
					if (atmCallbackErrorCode == ErrorCode.NoError && atmCallBackId == atmStrategyId)
						isAtmStrategyCreated = true;
				});
					comprado = true;
					vendido = false;
					preco = (GetCurrentBid(0) - Distancia * TickSize);
					Print(Time[0] + " " + Instrument.FullName + " Ordem de Compra Longa Pendente");
			}
			// Venda Maior
			if ((orderId.Length == 0 && atmStrategyId.Length == 0)
				&& (StochasticsFast1.K[0] < LinhaI)  // 60m - 17   > Linha Superior
				&& (StochasticsFast2.K[0] < LinhaI)  // 60m - 72   > Linha Superior
				&& (StochasticsFast3.K[0] < LinhaI)  // 60m - 305  > Linha Superior
				&& (StochasticsFast4.K[0] < LinhaI)  // 15m - 17   > Linha Superior
				&& (StochasticsFast5.K[0] < LinhaI)  // 15m - 72   > Linha Superior
				&& (StochasticsFast6.K[0] < LinhaI)  // 15m - 305  > Linha Superior
				&& (EMA1[0] < EMA1[1])
				&& (MACD1.Diff[0] < MACD1.Diff[1])
				&& (StochasticsFast4.K[0] < StochasticsFast4.K[1]))
			{
				isAtmStrategyCreated = false;  // reset atm strategy created check to false
				atmStrategyId = GetAtmStrategyUniqueId();
				orderId = GetAtmStrategyUniqueId();
				AtmStrategyCreate(OrderAction.Sell, OrderType.Limit, (GetCurrentAsk(0) + Distancia * TickSize), 0, TimeInForce.Gtc, orderId, "ATM_TimeSeriesStoch2", atmStrategyId, (atmCallbackErrorCode, atmCallBackId) => {
					//check that the atm strategy create did not result in error, and that the requested atm strategy matches the id in callback
					if (atmCallbackErrorCode == ErrorCode.NoError && atmCallBackId == atmStrategyId)
						isAtmStrategyCreated = true;
				});
					vendido = true;
					comprado = false;
					preco = (GetCurrentAsk(0) + Distancia * TickSize);
					Print(Time[0] + " " + Instrument.FullName + " Ordem de Venda Longa Pendente");
			}
			
			// Compra Menor
//			if ((orderId.Length == 0 && atmStrategyId.Length == 0)
//				&& (StochasticsFast1.K[0] > LinhaS)  // 60m - 17   > Linha Superior
//				 && (StochasticsFast2.K[0] > LinhaS)  // 60m - 72   > Linha Superior
//				 && (StochasticsFast3.K[0] > LinhaS)  // 60m - 305  > Linha Superior
//				 && (StochasticsFast4.K[0] > LinhaS)  // 15m - 17   > Linha Superior
//				 && (StochasticsFast5.K[0] > LinhaS)  // 15m - 72   > Linha Superior
//				 && (StochasticsFast6.K[0] > LinhaS)  // 15m - 305  > Linha Superior
//				 && (StochasticsFast7.K[1] < LinhaI)  // Curr - 17  < Linha Inferior**
//				 && (StochasticsFast7.K[0] > LinhaI)  // Curr - 17  > Linha Inferior**
//				 //&& (StochasticsFast7.K[1] < LinhaS)  // Curr - 17  < Linha Superior**
//				 //&& (StochasticsFast7.K[0] > LinhaS)  // Curr - 17  > Linha Superior**
//				 && (StochasticsFast8.K[0] > LinhaS)  // Curr - 72  > Linha Superior
//				 && (StochasticsFast9.K[0] > LinhaS)) // Curr - 305 > Linha Superior)
//			{
//				isAtmStrategyCreated = false;  // reset atm strategy created check to false
//				atmStrategyId = GetAtmStrategyUniqueId();
//				orderId = GetAtmStrategyUniqueId();
//				AtmStrategyCreate(OrderAction.Buy, OrderType.Limit, (GetCurrentBid(0) - Distancia * TickSize), 0, TimeInForce.Gtc, orderId, "ATM_TimeSeriesStoch1", atmStrategyId, (atmCallbackErrorCode, atmCallBackId) => {
//					//check that the atm strategy create did not result in error, and that the requested atm strategy matches the id in callback
//					if (atmCallbackErrorCode == ErrorCode.NoError && atmCallBackId == atmStrategyId)
//						isAtmStrategyCreated = true;
//				});
//					comprado = true;
//					vendido = false;
//					preco = (GetCurrentBid(0) - Distancia * TickSize);
//					Print(Time[0] + " " + Instrument.FullName + " Ordem de Compra Curta Pendente");
//			}
//			// Venda Menor
//			if ((orderId.Length == 0 && atmStrategyId.Length == 0)
//				&& (StochasticsFast1.K[0] < LinhaI)  // 60m - 17   > Linha Superior
//				&& (StochasticsFast2.K[0] < LinhaI)  // 60m - 72   > Linha Superior
//				&& (StochasticsFast3.K[0] < LinhaI)  // 60m - 305  > Linha Superior
//				&& (StochasticsFast4.K[0] < LinhaI)  // 15m - 17   > Linha Superior
//				&& (StochasticsFast5.K[0] < LinhaI)  // 15m - 72   > Linha Superior
//				&& (StochasticsFast6.K[0] < LinhaI)  // 15m - 305  > Linha Superior
//				//&& (StochasticsFast7.K[1] > LinhaI)  // Prev - 17  < Linha Inferior**
//				//&& (StochasticsFast7.K[0] < LinhaI)  // Curr - 17  > Linha Inferior**
//				&& (StochasticsFast7.K[1] > LinhaS)  // Prev - 17  < Linha Superior**
//				&& (StochasticsFast7.K[0] < LinhaS)  // Curr - 17  > Linha Superior**
//				&& (StochasticsFast8.K[0] < LinhaI)  // Curr - 72  > Linha Superior
//				&& (StochasticsFast9.K[0] < LinhaI)) // Curr - 305 > Linha Superior))
//			{
//				isAtmStrategyCreated = false;  // reset atm strategy created check to false
//				atmStrategyId = GetAtmStrategyUniqueId();
//				orderId = GetAtmStrategyUniqueId();
//				AtmStrategyCreate(OrderAction.Sell, OrderType.Limit, (GetCurrentAsk(0) + Distancia * TickSize), 0, TimeInForce.Gtc, orderId, "ATM_TimeSeriesStoch1", atmStrategyId, (atmCallbackErrorCode, atmCallBackId) => {
//					//check that the atm strategy create did not result in error, and that the requested atm strategy matches the id in callback
//					if (atmCallbackErrorCode == ErrorCode.NoError && atmCallBackId == atmStrategyId)
//						isAtmStrategyCreated = true;
//				});
//					vendido = true;
//					comprado = false;
//					preco = (GetCurrentAsk(0) + Distancia * TickSize);
//					Print(Time[0] + " " + Instrument.FullName + " Ordem de Venda Curta Pendente");
//			}
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
		[Display(Name="Fractal1", Description="Fractal Menor", Order=1, GroupName="Parameters")]
		public int Fractal1
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Fractal2", Description="Fractal Intermediario", Order=2, GroupName="Parameters")]
		public int Fractal2
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Fractal3", Description="Fractal Maior", Order=3, GroupName="Parameters")]
		public int Fractal3
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name="LinhaS", Description="Linha Superior do Estocastico", Order=4, GroupName="Parameters")]
		public double LinhaS
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name="LinhaI", Description="Linha Inferior do Estocastico", Order=5, GroupName="Parameters")]
		public double LinhaI
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="Distancia (Ordem Limite)", Order=6, GroupName="Parameters")]
		public int Distancia
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="Cancelamento (Se Nao Executado)", Order=7, GroupName="Parameters")]
		public int Cancelamento
		{ get; set; }
		
		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="Open Session 1", Description="Horario de Inicio", Order=8, GroupName="Parameters")]
		public DateTime OpenSession1
		{ get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="Close Session 1", Description="Horario de Fechamento", Order=9, GroupName="Parameters")]
		public DateTime CloseSession1
		{ get; set; }
		
//		[NinjaScriptProperty]
//		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
//		[Display(Name="Open Session 2", Description="Horario de Inicio", Order=8, GroupName="Parameters")]
//		public DateTime OpenSession2
//		{ get; set; }

//		[NinjaScriptProperty]
//		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
//		[Display(Name="Close Session 2", Description="Horario de Fechamento", Order=9, GroupName="Parameters")]
//		public DateTime CloseSession2
//		{ get; set; }
		
//		[NinjaScriptProperty]
//		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
//		[Display(Name="Open Session 3", Description="Horario de Inicio", Order=10, GroupName="Parameters")]
//		public DateTime OpenSession3
//		{ get; set; }

//		[NinjaScriptProperty]
//		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
//		[Display(Name="Close Session 3", Description="Horario de Fechamento", Order=11, GroupName="Parameters")]
//		public DateTime CloseSession3
//		{ get; set; }
		
		
//		[NinjaScriptProperty]
//		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
//		[Display(Name="Inicio Hora Sangrenta", Description="Horario de Inicio", Order=12, GroupName="Parameters")]
//		public DateTime OpenBloodHour
//		{ get; set; }

//		[NinjaScriptProperty]
//		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
//		[Display(Name="Termino Hora Sangrenta", Description="Horario de Fechamento", Order=13, GroupName="Parameters")]
//		public DateTime CloseBloodHour
//		{ get; set; }

//		[NinjaScriptProperty]
//		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
//		[Display(Name="LastEntry", Description="Proximo ao Fechamento", Order=14, GroupName="Parameters")]
//		public DateTime LastEntry
//		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name="Ganho Satisfatório", Order=10, GroupName="Parameters")]
		public double DayGainStop
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name="Perda Máxima", Order=11, GroupName="Parameters")]
		public double DayLossStop
		{ get; set; }

		#endregion

	}
}
