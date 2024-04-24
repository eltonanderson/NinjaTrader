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
        private EMA2 ema;
        private OBV2 obv;
        private ROC2 roc;
        private STOCH2 sto;

        private int PIVcompra;
        private int PIVvenda;
        private int EMAcompra;
        private int EMAvenda;
        private int OBVcompra;
        private int OBVvenda;
        private int ROCcompra;
        private int ROCvenda;
        private int STOcompra;
        private int STOvenda;

        private int _TrailingTrigger;
        private int _TrailingStep;

        private double trailingPrice;
        private double entryPrice;
        private double diffPrice;

        private bool goodToGo;
        private bool comprado;
        private bool vendido;
        private bool trailingStop;
        private bool disconnection;
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

                periodOBV = 15;     //13
                espacamentoOBV = 35;        //20

                periodROC = 5;      //4
                emaROC = 13;        //4

                periodSTO = 17;
                emaSTO = 12;
                signalSTO = 17;

                pivROC = 19;        //20  

                StopLossTicks = 10;     //13
                ProfitTargetTicks = 37;     //31
                TrailingStep = 6;       //7
                TrailingTrigger = 6;        //6

                OpenSession = DateTime.Parse("19:00", System.Globalization.CultureInfo.InvariantCulture);
                CloseSession = DateTime.Parse("17:00", System.Globalization.CultureInfo.InvariantCulture);
                #endregion

            }

            #region Indicadores Graficos

            else if (State == State.Configure)
            {
                ema = EMA2(periodEMA1, periodEMA2, periodEMA3, espacamentoEMA);
                obv = OBV2(periodOBV, espacamentoOBV);
                roc = ROC2(periodROC, emaROC);
                sto = STOCH2(periodSTO, emaSTO, signalSTO);

                disconnection = false;
            }

            else if (State == State.DataLoaded)
            {
                AddChartIndicator(ema);
                AddChartIndicator(obv);
                AddChartIndicator(roc);
                AddChartIndicator(sto);

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

                if (trailingStop)

                {

                    if (Position.MarketPosition == MarketPosition.Long && Close[0] >= trailingPrice)
                    {
                        trailingPrice = Close[0];
                        diffPrice = (trailingPrice - entryPrice) / TickSize;
                        Print("----------------------------------------------------------------------------------------");
                        Print(Time[0] + " " + Instrument.FullName + " Preço de Entrada = " + entryPrice);
                        Print(Time[0] + " " + Instrument.FullName + " Alcançe do Preco = " + trailingPrice);
                        Print(Time[0] + " " + Instrument.FullName + " Diff Entrada/Alcançe (Ticks) = " + diffPrice);
                        Print("----------------------------------------------------------------------------------------");
                    }

                    if (Position.MarketPosition == MarketPosition.Short && Close[0] <= trailingPrice)
                    {
                        trailingPrice = Close[0];
                        diffPrice = (entryPrice - trailingPrice) / TickSize;
                        Print("----------------------------------------------------------------------------------------");
                        Print(Time[0] + " " + Instrument.FullName + " Preço de Entrada = " + entryPrice);
                        Print(Time[0] + " " + Instrument.FullName + " Alcance do Preco = " + trailingPrice);
                        Print(Time[0] + " " + Instrument.FullName + " Diff Entrada/Alcançe (Ticks) = " + diffPrice);
                        Print("----------------------------------------------------------------------------------------");
                    }

                    if (diffPrice > 0 && diffPrice >= _TrailingTrigger)
                    {
                        SetStopLoss(CalculationMode.Ticks, (StopLossTicks - _TrailingStep));

                        _TrailingTrigger = _TrailingTrigger + TrailingTrigger;
                        _TrailingStep = _TrailingStep + TrailingStep;

                        Print("----------------------------------------------------------------------------------------");
                        Print(Time[0] + " " + Instrument.FullName + " Trailing Step");
                        Print(Time[0] + " " + Instrument.FullName + " Preço de Entrada = " + entryPrice);
                        Print(Time[0] + " " + Instrument.FullName + " Trailing Trigger Price = " + Close[0]);
                        Print("----------------------------------------------------------------------------------------");
                    }


                }
                #endregion

                return;

            }

            if ((Times[0][0].TimeOfDay < OpenSession.TimeOfDay) && (Times[0][0].TimeOfDay > CloseSession.TimeOfDay))
                goodToGo = false;

            else
                goodToGo = true;

            if (goodToGo)
            {

                #region Formação das Condições de Entrada

                // Formação do PIVOT
                if (
                    (Close[0] > Open[0] && Open[1] == Open[0] && roc.CurrROC[0] < pivROC)
                    //|| (Close[0] > Open[0] && Close[1] > Open[1] && Open[2] == Open[1]  && roc.CurrROC[0] < pivROC)
                    )
                {
                    PIVcompra = 1;
                    PIVvenda = 0;

                    //Print(Time[0] + "		" + "ROC Value =" + " " + roc.CurrROC[0]);
                    //Print(Time[0] + "		" + "PIVOT Compra =" + " " + PIVcompra);
                    //Print(Time[0] + "		" + "PIVOT Venda =" + " " + PIVvenda);
                    //Print("--------------------------------------------------------");
                }
                else if (
                    (Close[0] < Open[0] && Open[1] == Open[0] && roc.CurrROC[0] > -pivROC)
                    //|| (Close[0] < Open[0] && Close[1] < Open[1] && Open[2] == Open[1] && roc.CurrROC[0] > -pivROC)
                    )
                {
                    PIVcompra = 0;
                    PIVvenda = 1;

                    //Print(Time[0] + "		" + "ROC Value =" + " " + roc.CurrROC[0]);
                    //Print(Time[0] + "		" + "PIVOT Compra =" + " " + PIVcompra);
                    //Print(Time[0] + "		" + "PIVOT Venda =" + " " + PIVvenda);
                    //Print("--------------------------------------------------------");
                }
                else
                {
                    PIVcompra = 0;
                    PIVvenda = 0;

                    //Print(Time[0] + "		" + "ROC Value =" + " " + roc.CurrROC[0]);
                    //Print(Time[0] + "		" + "PIVOT Compra =" + " " + PIVcompra);
                    //Print(Time[0] + "		" + "PIVOT Venda =" + " " + PIVvenda);
                    //Print("--------------------------------------------------------");
                }

                // Formação EMA2
                if (ema.boolcompra[0] != null)
                {
                    if (ema.boolcompra[0] == true)
                        EMAcompra = 1;
                    else
                        EMAcompra = 0;

                    //bool booltest = ema.boolcompra[0];			
                    //Print(Time[0] + "		" + "EMA2 Compra =" + " " + booltest);
                    //Print(Time[0] + "		" + "EMA2 Compra =" + " " + EMAcompra);
                }

                if (ema.boolvenda[0] != null)
                {
                    if (ema.boolvenda[0] == true)
                        EMAvenda = 1;
                    else
                        EMAvenda = 0;

                    //bool booltest5 = ema.boolvenda[0];			
                    //Print(Time[0] + "		" + "EMA2 Venda =" + " " + booltest5);
                    //Print(Time[0] + "		" + "EMA2 Venda =" + " " + EMAvenda);
                    //Print("--------------------------------------------------------");
                }

                // Formação OBV2
                if (obv.boolcompra[0] != null)
                {
                    if (obv.boolcompra[0] == true)
                        OBVcompra = 1;
                    else
                        OBVcompra = 0;

                    //bool booltest2 = obv.boolcompra[0];			
                    //Print(Time[0] + "		" + "OBV2 Compra =" + " " + booltest2);
                    //Print(Time[0] + "		" + "OBV2 Compra =" + " " + OBVcompra);
                }

                if (obv.boolvenda[0] != null)
                {
                    if (obv.boolvenda[0] == true)
                        OBVvenda = 1;
                    else
                        OBVvenda = 0;

                    //bool booltest6 = obv.boolvenda[0];			
                    //Print(Time[0] + "		" + "OBV2 Venda =" + " " + booltest6);
                    //Print(Time[0] + "		" + "OBV2 Venda =" + " " + OBVvenda);
                    //Print("--------------------------------------------------------");
                }

                // Formação ROC2
                if (roc.boolcompra[0] != null)
                {
                    if (roc.boolcompra[0] == true)
                        ROCcompra = 1;
                    else
                        ROCcompra = 0;

                    //bool booltest3 = roc.boolcompra[0];			
                    //Print(Time[0] + "		" + "ROC2 Compra =" + " " + booltest3);
                    //Print(Time[0] + "		" + "ROC2 Compra =" + " " + ROCcompra);
                }

                if (roc.boolvenda[0] != null)
                {
                    if (roc.boolvenda[0] == true)
                        ROCvenda = 1;
                    else
                        ROCvenda = 0;

                    //bool booltest7 = roc.boolvenda[0];			
                    //Print(Time[0] + "		" + "ROC2 Venda =" + " " + booltest7);
                    //Print(Time[0] + "		" + "ROC2 Venda =" + " " + ROCvenda);
                    //Print("--------------------------------------------------------");
                }

                // Formação STO2
                if (sto.boolcompra[0] != null)
                {
                    if (sto.boolcompra[0] == true)
                        STOcompra = 1;
                    else
                        STOcompra = 0;

                    //bool booltest4 = sto.boolcompra[0];			
                    //Print(Time[0] + "		" + "STO2 Compra =" + " " + booltest4);
                    //Print(Time[0] + "		" + "STO2 Compra =" + " " + STOcompra);
                }

                if (sto.boolvenda[0] != null)
                {
                    if (sto.boolvenda[0] == true)
                        STOvenda = 1;
                    else
                        STOvenda = 0;

                    //bool booltest8 = sto.boolvenda[0];			
                    //Print(Time[0] + "		" + "STO2 Venda =" + " " + booltest8);
                    //Print(Time[0] + "		" + "STO2 Venda =" + " " + STOvenda);
                    //Print("--------------------------------------------------------");
                }
                #endregion



                #region Condição de COMPRA/VENDA

                // Condição de COMPRA


                if ((Position.MarketPosition == MarketPosition.Flat)
                    //condicoes de compra aqui
                    && ((PIVcompra == 1 && EMAcompra == 1 && OBVcompra == 1 && STOcompra == 1)
                    || (PIVcompra == 1 && EMAcompra == 1 && ROCcompra == 1 && STOcompra == 1)
                    //|| (PIVcompra == 1 && OBVcompra == 1 && ROCcompra == 1 && STOcompra == 1)
                    || (PIVcompra == 1 && EMAcompra == 1 && OBVcompra == 1 && ROCcompra == 1))

                    //&& STOcompra == 1 && Close[0] > Open[0]

                    )
                {
                    Print("----------------------------------------------------------------------------------------");
                    SetStopLoss(CalculationMode.Ticks, StopLossTicks);
                    SetProfitTarget(CalculationMode.Ticks, ProfitTargetTicks, true);

                    //EnterLongMIT(Quantidade, Close[0]);
                    EnterLong(Quantidade);

                    trailingPrice = Close[0];
                    entryPrice = Close[0];

                    Print(Time[0] + " " + Instrument.FullName + " Ordem de COMPRA Enviada");
                    Print("----------------------------------------------------------------------------------------");
                }

                // Condição de VENDA


                if ((Position.MarketPosition == MarketPosition.Flat)
                    //condicoes de venda aqui
                    && ((PIVvenda == 1 && EMAvenda == 1 && OBVvenda == 1 && STOvenda == 1)
                    || (PIVvenda == 1 && EMAvenda == 1 && ROCvenda == 1 && STOvenda == 1)
                    //|| (PIVvenda == 1 && OBVvenda == 1 && ROCvenda == 1 && STOvenda == 1)
                    || (PIVvenda == 1 && EMAvenda == 1 && OBVvenda == 1 && ROCvenda == 1))

                    //&& STOvenda == 1 && Close[0] < Open[0]

                    )
                {
                    Print("----------------------------------------------------------------------------------------");
                    SetStopLoss(CalculationMode.Ticks, StopLossTicks);
                    SetProfitTarget(CalculationMode.Ticks, ProfitTargetTicks, true);

                    //EnterShortMIT(Quantidade, Close[0]);
                    EnterShort(Quantidade);

                    trailingPrice = Close[0];
                    entryPrice = Close[0];

                    Print(Time[0] + " " + Instrument.FullName + " Ordem de VENDA Enviada");
                    Print("----------------------------------------------------------------------------------------");
                }
                #endregion

            }

        }


        #region Stop Management	(OnMarketData)
        /*		
                protected override void OnMarketData(MarketDataEventArgs marketDataUpdate)
                {

                    if (trailingStop && marketDataUpdate.Price > 0)

                                {

                                    if(Position.MarketPosition == MarketPosition.Long && marketDataUpdate.Price > trailingPrice)
                                            {
                                                trailingPrice = marketDataUpdate.Price;
                                                diffPrice = (trailingPrice - entryPrice)  / TickSize;
                                                Print ("------------------------------------------------");
                                                Print("Preço de Entrada = " + entryPrice);										
                                                Print("Alcançe do Preco = " + trailingPrice);
                                                Print("Diff Entrada/Alcançe = " + diffPrice);
                                                Print ("------------------------------------------------");
                                            }

                                    if(Position.MarketPosition == MarketPosition.Short && marketDataUpdate.Price < trailingPrice)
                                            {
                                                trailingPrice = marketDataUpdate.Price;
                                                diffPrice = (entryPrice - trailingPrice) / TickSize;
                                                Print ("------------------------------------------------");
                                                Print("Preço de Entrada = " + entryPrice);										
                                                Print("Alcance do Preco = " + trailingPrice);
                                                Print("Diff Entrada/Alcançe = " + diffPrice);
                                                Print ("------------------------------------------------");
                                            }

                                    if(diffPrice > 0 && diffPrice >= _TrailingTrigger)
                                            {
                                                SetStopLoss(CalculationMode.Ticks, (StopLossTicks - _TrailingStep));

                                                _TrailingTrigger = _TrailingTrigger + TrailingTrigger;
                                                _TrailingStep = _TrailingStep + TrailingStep;

                                                Print ("------------------------------------------------");
                                                Print ("Trailing Step");
                                                Print("Preço de Entrada = " + entryPrice);										
                                                Print("Trailing Trigger Price = " + marketDataUpdate.Price);
                                                Print ("------------------------------------------------");
                                            }


                                }

                }
        */
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
                comprado = false;
                vendido = false;

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
                comprado = false;
                vendido = false;
                trailingStop = false;

                _TrailingTrigger = TrailingTrigger;
                _TrailingStep = TrailingStep;
                diffPrice = 0;

                Print("----------------------------------------------------------------------------------------");
                Print(Time[0] + " " + Instrument.FullName + " Posição = Zerado");
                //Print("Rested Trailing Stop Value = " + _trailingStop);
                Print("----------------------------------------------------------------------------------------");
            }

            if (position.MarketPosition == MarketPosition.Long)
            {
                comprado = true;
                vendido = false;
                trailingStop = true;

                _TrailingTrigger = TrailingTrigger;
                _TrailingStep = TrailingStep;
                diffPrice = 0;

                Print("----------------------------------------------------------------------------------------");
                Print(Time[0] + " " + Instrument.FullName + " Posição = Comprado");
                Print("----------------------------------------------------------------------------------------");
            }

            if (position.MarketPosition == MarketPosition.Short)
            {
                comprado = false;
                vendido = true;
                trailingStop = true;

                _TrailingTrigger = TrailingTrigger;
                _TrailingStep = TrailingStep;
                diffPrice = 0;

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
                    goodToGo = false;
                }

                if (disconnection)
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
                disconnection = true;
            }
        }
        #endregion

        #region Outros Metodos Uteis

        protected override void OnAccountItemUpdate(Cbi.Account account, Cbi.AccountItem accountItem, double value)
        {

        }


        protected override void OnMarketDepth(MarketDepthEventArgs marketDepthUpdate)
        {

        }


        #endregion

    }
}
