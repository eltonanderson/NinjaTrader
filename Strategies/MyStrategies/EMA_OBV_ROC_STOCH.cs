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
using NinjaTrader.NinjaScript.Indicators.MyIndicators;
using NinjaTrader.NinjaScript.DrawingTools;
using System.Security.Cryptography;
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies.MyStrategies
{
    public class EMAOBVROCSTOCH : Strategy
    {

        #region Tipos		
        private EMA2 _ema;
        private OBV2 _obv;
        private ROC2 _roc;
        private STOCH2 _sto;

        private int _trailingTrigger;
        private int _trailingStep;

        private double _trailingPrice;
        private double _entryPrice;
        private double _diffPrice;

        private bool _goodToGo;
        private bool _bought;
        private bool _sold;
        private bool _trailingStop;
        private bool _disconnection;
        #endregion

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                #region Defaults
                Description = @"";
                Name = "EMAOBVROCSTOCH";
                Calculate = Calculate.OnBarClose;
                EntriesPerDirection = 1;
                EntryHandling = EntryHandling.AllEntries;
                IsExitOnSessionCloseStrategy = true;
                ExitOnSessionCloseSeconds = 30;
                IsFillLimitOnTouch = false;
                MaximumBarsLookBack = MaximumBarsLookBack.TwoHundredFiftySix;
                OrderFillResolution = OrderFillResolution.Standard;
                Slippage = 0;
                StartBehavior = StartBehavior.WaitUntilFlat;
                TimeInForce = TimeInForce.Day;
                TraceOrders = false;
                RealtimeErrorHandling = RealtimeErrorHandling.IgnoreAllErrors;
                StopTargetHandling = StopTargetHandling.PerEntryExecution;
                BarsRequiredToTrade = 20;
                ConnectionLossHandling = ConnectionLossHandling.KeepRunning;
                // Disable this property for performance gains in Strategy Analyzer optimizations
                // See the Help Guide for additional information
                IsInstantiatedOnEachOptimizationIteration = true;
                #endregion

                #region Variaveis Backtest

                Quantidade = 1;

                periodEMA1 = 8;
                periodEMA2 = 12;
                periodEMA3 = 17;
                espacamentoEMA = 0.14;      //0.13

                periodOBV = 15;             //13
                espacamentoOBV = 35;        //20

                periodROC = 5;              //4
                emaROC = 13;                //4

                periodSTO = 17;
                emaSTO = 12;
                signalSTO = 17;

                pivROC = 19;                //20  

                StopLossTicks = 10;         //13
                ProfitTargetTicks = 37;     //31
                TrailingStep = 6;           //7
                TrailingTrigger = 6;        //6

                OpenSession = DateTime.Parse("20:00", System.Globalization.CultureInfo.InvariantCulture);
                CloseSession = DateTime.Parse("17:00", System.Globalization.CultureInfo.InvariantCulture);
                #endregion

            }

            #region Indicadores Graficos

            else if (State == State.Configure)
            {
                _ema = EMA2(periodEMA1, periodEMA2, periodEMA3, espacamentoEMA);
                _obv = OBV2(periodOBV, espacamentoOBV);
                _roc = ROC2(periodROC, emaROC);
                _sto = STOCH2(periodSTO, emaSTO, signalSTO);

                _disconnection = false;
            }

            else if (State == State.DataLoaded)
            {
                AddChartIndicator(_ema);
                AddChartIndicator(_obv);
                AddChartIndicator(_roc);
                AddChartIndicator(_sto);

                ClearOutputWindow();
                Draw.TextFixed(this, "Robot", Name, TextPosition.BottomLeft);
            }
            #endregion

        }

        protected override void OnBarUpdate()
        {

            if (CurrentBar < BarsRequiredToTrade)
                return;

            if (BarsInProgress != 0)
                return;

            //if(State == State.Historical)
            //	return;

            if (Position.MarketPosition != MarketPosition.Flat)
            {

                #region Stop Management	(OnBarUpdate)

                if (_trailingStop)

                {

                    if (Position.MarketPosition == MarketPosition.Long && Close[0] >= _trailingPrice)
                    {
                        _trailingPrice = Close[0];
                        _diffPrice = (_trailingPrice - _entryPrice) / TickSize;
                        Print("----------------------------------------------------------------------------------------");
                        Print(Time[0] + " " + Instrument.FullName + " Preço de Entrada = " + _entryPrice);
                        Print(Time[0] + " " + Instrument.FullName + " Alcançe do Preco = " + _trailingPrice);
                        Print(Time[0] + " " + Instrument.FullName + " Diff Entrada/Alcançe (Ticks) = " + _diffPrice);
                        Print("----------------------------------------------------------------------------------------");
                    }

                    if (Position.MarketPosition == MarketPosition.Short && Close[0] <= _trailingPrice)
                    {
                        _trailingPrice = Close[0];
                        _diffPrice = (_entryPrice - _trailingPrice) / TickSize;
                        Print("----------------------------------------------------------------------------------------");
                        Print(Time[0] + " " + Instrument.FullName + " Preço de Entrada = " + _entryPrice);
                        Print(Time[0] + " " + Instrument.FullName + " Alcance do Preco = " + _trailingPrice);
                        Print(Time[0] + " " + Instrument.FullName + " Diff Entrada/Alcançe (Ticks) = " + _diffPrice);
                        Print("----------------------------------------------------------------------------------------");
                    }

                    if (_diffPrice > 0 && _diffPrice >= _trailingTrigger)
                    {
                        SetStopLoss(CalculationMode.Ticks, (StopLossTicks - _trailingStep));

                        _trailingTrigger = _trailingTrigger + TrailingTrigger;
                        _trailingStep = _trailingStep + TrailingStep;

                        Print("----------------------------------------------------------------------------------------");
                        Print(Time[0] + " " + Instrument.FullName + " Trailing Step");
                        Print(Time[0] + " " + Instrument.FullName + " Preço de Entrada = " + _entryPrice);
                        Print(Time[0] + " " + Instrument.FullName + " Trailing Trigger Price = " + Close[0]);
                        Print("----------------------------------------------------------------------------------------");
                    }


                }
                #endregion

                return;

            }

            if((Times[0][0].TimeOfDay < OpenSession.TimeOfDay) && (Times[0][0].TimeOfDay > CloseSession.TimeOfDay))
            {
                _goodToGo = false;
                BackBrush = Brushes.LightGray;
            }
                

            else
                _goodToGo = true;

            if (_goodToGo && !_disconnection)
            {

                #region Condição de COMPRA/VENDA

                // Buying Condition

                if ((Position.MarketPosition == MarketPosition.Flat)
                    && (Close[0] > Open[0] && Open[1] == Open[0])
                    && _ema.boolLongSeries[0] && _ema.boolLongSeries[1] && _ema.boolLongSeries[2]
                    && _obv.boolLongSeries[0] //&& _obv.boolLongSeries[1]
                    && _roc.boolLongSeries[0] //&& _roc.boolLongSeries[1]
                    && _sto.boolLongSeries[0] //&& _sto.boolLongSeries[1]
                    )
                {
                    Print("----------------------------------------------------------------------------------------");
                    SetStopLoss(CalculationMode.Ticks, StopLossTicks);
                    SetProfitTarget(CalculationMode.Ticks, ProfitTargetTicks, true);

                    //EnterLongMIT(Quantidade, Close[0]);
                    EnterLong(Quantidade);

                    _trailingPrice = Close[0];
                    _entryPrice = Close[0];

                    Print(Time[0] + " " + Instrument.FullName + " Ordem de COMPRA Enviada");
                    Print("----------------------------------------------------------------------------------------");
                }

                // Selling Condition

                if ((Position.MarketPosition == MarketPosition.Flat)
                    && (Close[0] < Open[0] && Open[1] == Open[0])
                    && _ema.boolShortSeries[0] && _ema.boolShortSeries[1] && _ema.boolShortSeries[2]
                    && _obv.boolShortSeries[0] //&& _obv.boolShortSeries[1]
                    && _roc.boolShortSeries[0] //&& _roc.boolShortSeries[1]
                    && _sto.boolShortSeries[0] //&& _sto.boolShortSeries[1]
                    )
                {
                    Print("----------------------------------------------------------------------------------------");
                    SetStopLoss(CalculationMode.Ticks, StopLossTicks);
                    SetProfitTarget(CalculationMode.Ticks, ProfitTargetTicks, true);

                    //EnterShortMIT(Quantidade, Close[0]);
                    EnterShort(Quantidade);

                    _trailingPrice = Close[0];
                    _entryPrice = Close[0];

                    Print(Time[0] + " " + Instrument.FullName + " Ordem de VENDA Enviada");
                    Print("----------------------------------------------------------------------------------------");
                }
                #endregion

            }

        }


        #region Stop Management	(OnMarketData)
        		
                protected override void OnMarketData(MarketDataEventArgs marketDataUpdate)
                {

                    if (_trailingStop && marketDataUpdate.Price > 0)

                                {

                                    if(Position.MarketPosition == MarketPosition.Long && marketDataUpdate.Price > _trailingPrice)
                                            {
                                                _trailingPrice = marketDataUpdate.Price;
                                                _diffPrice = (_trailingPrice - _entryPrice)  / TickSize;
                                                Print ("------------------------------------------------");
                                                Print("Preço de Entrada = " + _entryPrice);										
                                                Print("Alcançe do Preco = " + _trailingPrice);
                                                Print("Diff Entrada/Alcançe = " + _diffPrice);
                                                Print ("------------------------------------------------");
                                            }

                                    if(Position.MarketPosition == MarketPosition.Short && marketDataUpdate.Price < _trailingPrice)
                                            {
                                                _trailingPrice = marketDataUpdate.Price;
                                                _diffPrice = (_entryPrice - _trailingPrice) / TickSize;
                                                Print ("------------------------------------------------");
                                                Print("Preço de Entrada = " + _entryPrice);										
                                                Print("Alcance do Preco = " + _trailingPrice);
                                                Print("Diff Entrada/Alcançe = " + _diffPrice);
                                                Print ("------------------------------------------------");
                                            }

                                    if(_diffPrice > 0 && _diffPrice >= _trailingTrigger)
                                            {
                                                SetStopLoss(CalculationMode.Ticks, (StopLossTicks - _trailingStep));

                                                _trailingTrigger += TrailingTrigger;
                                                _trailingStep += TrailingStep;

                                                Print ("------------------------------------------------");
                                                Print ("Trailing Step");
                                                Print("Preço de Entrada = " + _entryPrice);										
                                                Print("Trailing Trigger Price = " + marketDataUpdate.Price);
                                                Print ("------------------------------------------------");
                                            }
                                }
                }
        
        #endregion


        #region Execution/Position Update

        protected override void OnExecutionUpdate(Execution execution, string executionId, double price, int quantity, MarketPosition marketPosition, string orderId, DateTime time)
        {
            //entryPrice = execution.Price;

            Print("----------------------------------------------------------------------------------------");
            Print(Time[0] + " " + Instrument.FullName + " Preço de Execução = " + execution.Price);
            Print(Time[0] + " " + Instrument.FullName + " Quantidade = " + execution.Quantity);
            //Print(Time[0] + " " + Instrument.FullName + " Hora da Execução = " + execution.Time);
            Print("----------------------------------------------------------------------------------------");
        }

        protected override void OnOrderUpdate(Order order, double limitPrice, double stopPrice, int quantity, int filled, double averageFillPrice, OrderState orderState, DateTime time, ErrorCode error, string nativeError)
        {

            if (order.OrderState == OrderState.Rejected)
            {
                Print("----------------------------------------------------------------------------------------");
                Print(Time[0] + " " + Instrument.FullName + " Ordem Rejeitada");
                Print("----------------------------------------------------------------------------------------");
            }

            if (order.OrderState == OrderState.Cancelled)
            {
                _bought = false;
                _sold = false;

                Print("----------------------------------------------------------------------------------------");
                Print(Time[0] + " " + Instrument.FullName + " Ordem Cancelada");
                Print("----------------------------------------------------------------------------------------");
            }

            if (order.OrderState == OrderState.Filled)
            {
                Print("----------------------------------------------------------------------------------------");
                Print(Time[0] + " " + Instrument.FullName + " Ordem Totalmente Execudata");
                Print("----------------------------------------------------------------------------------------");
            }
        }

        protected override void OnPositionUpdate(Position position, double averagePrice, int quantity, MarketPosition marketPosition)
        {
            if (position.MarketPosition == MarketPosition.Flat)
            {
                _bought = false;
                _sold = false;
                _trailingStop = false;

                _trailingTrigger = TrailingTrigger;
                _trailingStep = TrailingStep;
                _diffPrice = 0;

                Print("----------------------------------------------------------------------------------------");
                Print(Time[0] + " " + Instrument.FullName + " Posição = Zerado");
                //Print("Rested Trailing Stop Value = " + _trailingStop);
                Print("----------------------------------------------------------------------------------------");
            }

            if (position.MarketPosition == MarketPosition.Long)
            {
                _bought = true;
                _sold = false;
                _trailingStop = true;

                _trailingTrigger = TrailingTrigger;
                _trailingStep = TrailingStep;
                _diffPrice = 0;

                Print("----------------------------------------------------------------------------------------");
                Print(Time[0] + " " + Instrument.FullName + " Posição = Comprado");
                Print("----------------------------------------------------------------------------------------");
            }

            if (position.MarketPosition == MarketPosition.Short)
            {
                _bought = false;
                _sold = true;
                _trailingStop = true;

                _trailingTrigger = TrailingTrigger;
                _trailingStep = TrailingStep;
                _diffPrice = 0;

                Print("----------------------------------------------------------------------------------------");
                Print(Time[0] + " " + Instrument.FullName + " Posição = Vendido");
                Print("----------------------------------------------------------------------------------------");
            }
        }
        #endregion


        #region Properties

        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
        [Display(Name = "Quantidade", Description = "Quantidade de Contratos", Order = 1, GroupName = "Parameters")]
        public int Quantidade
        { get; set; }

        [NinjaScriptProperty]
        [PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
        [Display(Name = "Open Session", Description = "Horario de Inicio", Order = 7, GroupName = "Horário")]
        public DateTime OpenSession
        { get; set; }

        [NinjaScriptProperty]
        [PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
        [Display(Name = "Close Session", Description = "Horario de Fechamento", Order = 8, GroupName = "Horário")]
        public DateTime CloseSession
        { get; set; }

        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
        [Display(Name = "Periodo EMA 1", Description = "Periodo EMA 1", Order = 2, GroupName = "Indicadores")]
        public int periodEMA1
        { get; set; }

        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
        [Display(Name = "Periodo EMA 2", Description = "Periodo EMA 2", Order = 3, GroupName = "Indicadores")]
        public int periodEMA2
        { get; set; }

        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
        [Display(Name = "Periodo EMA 3", Description = "Periodo EMA 3", Order = 4, GroupName = "Indicadores")]
        public int periodEMA3
        { get; set; }

        [NinjaScriptProperty]
        [Range(0, double.MaxValue)]
        [Display(Name = "Espacamento EMA", Description = "Espacamento EMA", Order = 5, GroupName = "Indicadores")]
        public double espacamentoEMA
        { get; set; }

        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
        [Display(Name = "Period OBV", Description = "Period OBV", Order = 6, GroupName = "Indicadores")]
        public int periodOBV
        { get; set; }

        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
        [Display(Name = "Espacamento OBV", Description = "Espacamento OBV", Order = 7, GroupName = "Indicadores")]
        public int espacamentoOBV
        { get; set; }

        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
        [Display(Name = "Period ROC", Description = "Period ROC", Order = 8, GroupName = "Indicadores")]
        public int periodROC
        { get; set; }

        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
        [Display(Name = "EMA ROC", Description = "EMA ROC", Order = 9, GroupName = "Indicadores")]
        public int emaROC
        { get; set; }

        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
        [Display(Name = "Period STOCH", Description = "Period STOCH", Order = 10, GroupName = "Indicadores")]
        public int periodSTO
        { get; set; }

        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
        [Display(Name = "EMA STOCH", Description = "EMA STOCH", Order = 11, GroupName = "Indicadores")]
        public int emaSTO
        { get; set; }

        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
        [Display(Name = "Signal STOCH", Description = "Signal STOCH", Order = 12, GroupName = "Indicadores")]
        public int signalSTO
        { get; set; }

        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
        [Display(Name = "Pivot ROC", Description = "Pivot ROC", Order = 13, GroupName = "Indicadores")]
        public int pivROC
        { get; set; }


        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Stop Loss", Order = 14, GroupName = "Alvos")]
        public int StopLossTicks
        { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Alvo", Order = 15, GroupName = "Alvos")]
        public int ProfitTargetTicks
        { get; set; }


        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Trailing Step", Order = 14, GroupName = "Trailing")]
        public int TrailingStep
        { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Trailing Trigger", Order = 15, GroupName = "Trailing")]
        public int TrailingTrigger
        { get; set; }


        #endregion


        #region Connection Handling

        protected override void OnConnectionStatusUpdate(ConnectionStatusEventArgs connectionStatusUpdate)
        {
            if (connectionStatusUpdate.Status == ConnectionStatus.Connected)
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
                    Print("----------------------------------------------------------------------------------------");
                    Print(Time[0] + " " + Instrument.FullName + " Todas Posições Fechadas");
                    Print("----------------------------------------------------------------------------------------");
                    _goodToGo = false;
                }

                if (_disconnection)
                {
                    Print("----------------------------------------------------------------------------------------");
                    Print(Time[0] + " " + Instrument.FullName + " Robo Parado por Perda de Conexão");
                    Print("----------------------------------------------------------------------------------------");
                    SetState(State.Terminated);
                    return;
                }
            }

            else if (connectionStatusUpdate.Status == ConnectionStatus.ConnectionLost)
            {
                _disconnection = true;
            }
        }
        #endregion


    }
}
