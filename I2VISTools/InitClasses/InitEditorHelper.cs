using I2VISTools.Config;
using I2VISTools.Subclasses;
using I2VISTools.Tools;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace I2VISTools.InitClasses
{
    public static class InitEditorHelper
    {
        public static LineSeries RuleX;
        public static LineSeries RuleY;

        public static HIstoryView History = new HIstoryView();
        
        public static DataGrid GeometryDataGrid;
        public static DataGrid ThermoDataGrid;
        public static CheckBox GeothermsBox;

        public static void AttachMovingEvents(InitConfig config, PlotModel graphModel, LineSeries points)
        {
            int indexOfPointToMove = -1;

            // Subscribe to the mouse down event on the line series
            points.MouseDown += (s, ev) =>
            {
                // only handle the left mouse button (right button can still be used to pan)
                if (ev.ChangedButton == OxyMouseButton.Left)
                {

                    int indexOfNearestPoint = (int)Math.Round(ev.HitTestResult.Index);
                    var nearestPoint = points.Transform(points.Points[indexOfNearestPoint]);

                    // Check if we are near a point
                    if ((nearestPoint - ev.Position).Length < 10)
                    {
                        // Start editing this point
                        indexOfPointToMove = indexOfNearestPoint;
                    }
                    else
                    {
                        return;
                    }


                    // Change the linestyle while editing
                    points.LineStyle = LineStyle.DashDot;

                    RuleX = new LineSeries();
                    RuleY = new LineSeries();

                    RuleX.Points.Add(new DataPoint(points.Points[indexOfPointToMove].X, graphModel.Axes[0].ActualMaximum));
                    RuleX.Points.Add(new DataPoint(points.Points[indexOfPointToMove].X, points.Points[indexOfPointToMove].Y));
                    RuleX.LineStyle = LineStyle.Dot;
                    RuleX.StrokeThickness = 1;

                    RuleY.Points.Add(new DataPoint(graphModel.Axes[1].ActualMinimum, points.Points[indexOfPointToMove].Y));
                    RuleY.Points.Add(new DataPoint(points.Points[indexOfPointToMove].X, points.Points[indexOfPointToMove].Y));
                    RuleY.LineStyle = LineStyle.Dot;
                    RuleY.StrokeThickness = 1;

                    RuleX.Color = OxyColors.Red;
                    RuleY.Color = OxyColors.Red;

                    graphModel.Series.Add(RuleX);
                    graphModel.Series.Add(RuleY);

                    // Remember to refresh/invalidate of the plot
                    graphModel.InvalidatePlot(false);


                    IBox rb;
                    if (points.Tag.ToString().Contains("Geotherm"))
                    {
                        rb = config.Geotherms.FirstOrDefault(x => x.Name == points.Tag.ToString());
                    }
                    else
                    {
                        rb = config.RockBoxes.FirstOrDefault(x => x.Name == points.Tag.ToString());
                    }


                    if (rb != null)
                    {
                        rb.FreezeLogging = true;
                        var hl = new HistoryLog();

                        var x0 = rb.Apex0.X;
                        var y0 = rb.Apex0.Y;
                        var x1 = rb.Apex1.X;
                        var y1 = rb.Apex1.Y;
                        var x2 = rb.Apex2.X;
                        var y2 = rb.Apex2.Y;
                        var x3 = rb.Apex3.X;
                        var y3 = rb.Apex3.Y;

                        hl.Undo = () =>
                        {
                            rb.FreezeLogging = true;
                            rb.Apex0.X = x0;
                            rb.Apex0.Y = y0;
                            rb.Apex1.X = x1;
                            rb.Apex1.Y = y1;
                            rb.Apex2.X = x2;
                            rb.Apex2.Y = y2;
                            rb.Apex3.X = x3;
                            rb.Apex3.Y = y3;
                            rb.FreezeLogging = false;
                        };
                        History.Add(hl);

                    }

                    // Set the event arguments to handled - no other handlers will be called.
                    ev.Handled = true;
                }
            };

            points.MouseMove += (s, ev) =>
            {
                if (indexOfPointToMove >= 0)
                {
                    if (indexOfPointToMove == 4)
                    {
                        points.Points[0] = points.InverseTransform(ev.Position);
                        //_graphModel.InvalidatePlot(false);
                        //ev.Handled = true;
                    }
                    if (indexOfPointToMove == 0)
                    {
                        points.Points[4] = points.InverseTransform(ev.Position);
                        //_graphModel.InvalidatePlot(false);
                        //ev.Handled = true;
                    }

                    points.Points[indexOfPointToMove] = (ev.IsControlDown) ? AdjustDataPoint(points.InverseTransform(ev.Position), (int)graphModel.Axes[0].ActualMinorStep, (int)graphModel.Axes[1].ActualMinorStep) : points.InverseTransform(ev.Position);

                    RuleX.Points[0] = new DataPoint(points.Points[indexOfPointToMove].X, graphModel.Axes[0].ActualMaximum);
                    RuleX.Points[1] = new DataPoint(points.Points[indexOfPointToMove].X, points.Points[indexOfPointToMove].Y);
                    RuleY.Points[0] = new DataPoint(Int32.MinValue, points.Points[indexOfPointToMove].Y);
                    RuleY.Points[1] = new DataPoint(points.Points[indexOfPointToMove].X, points.Points[indexOfPointToMove].Y);

                    graphModel.InvalidatePlot(false);
                    ev.Handled = true;
                }
            };

            points.MouseUp += (s, ev) =>
            {
                //var currentBox = areas[points.Tag.ToString()];

                // IBox rb = (points.Tag.ToString().Contains("Geotherm")) ? _currentConfig.Geotherms.FirstOrDefault(x=>x.Name == points.Tag.ToString()) : _currentConfig.RockBoxes.FirstOrDefault(x => x.Name == points.Tag.ToString());

                IBox rb;

                if ((points.Tag.ToString().Contains("Geotherm")))
                {
                    rb = config.Geotherms.FirstOrDefault(x => x.Name == points.Tag.ToString());
                }
                else
                {
                    rb = config.RockBoxes.FirstOrDefault(x => x.Name == points.Tag.ToString());
                }

                if (rb != null)
                {

                    if (indexOfPointToMove == 0 || indexOfPointToMove == 4)
                    {
                        rb.Apex0 = new ModPoint((Convert.ToInt32(points.Points[indexOfPointToMove].X) / 100) * 100, (Convert.ToInt32(points.Points[indexOfPointToMove].Y / 100) * 100));
                    }
                    if (indexOfPointToMove == 1)
                    {
                        rb.Apex1 = new ModPoint((Convert.ToInt32(points.Points[indexOfPointToMove].X) / 100) * 100, (Convert.ToInt32(points.Points[indexOfPointToMove].Y / 100) * 100));

                    }
                    if (indexOfPointToMove == 2)
                    {
                        rb.Apex3 = new ModPoint((Convert.ToInt32(points.Points[indexOfPointToMove].X) / 100) * 100, (Convert.ToInt32(points.Points[indexOfPointToMove].Y / 100) * 100));
                    }
                    if (indexOfPointToMove == 3)
                    {
                        rb.Apex2 = new ModPoint((Convert.ToInt32(points.Points[indexOfPointToMove].X) / 100) * 100, (Convert.ToInt32(points.Points[indexOfPointToMove].Y / 100) * 100));
                    }

                    if (History.Logs != null && History.Logs.Count > 0)
                    {
                        var hl = History.Logs.Last();
                        if (hl.Do == null)
                        {
                            var x0 = rb.Apex0.X;
                            var y0 = rb.Apex0.Y;
                            var x1 = rb.Apex1.X;
                            var y1 = rb.Apex1.Y;
                            var x2 = rb.Apex2.X;
                            var y2 = rb.Apex2.Y;
                            var x3 = rb.Apex3.X;
                            var y3 = rb.Apex3.Y;

                            hl.Do = () =>
                            {
                                rb.FreezeLogging = true;
                                rb.Apex0.X = x0;
                                rb.Apex0.Y = y0;
                                rb.Apex1.X = x1;
                                rb.Apex1.Y = y1;
                                rb.Apex2.X = x2;
                                rb.Apex2.Y = y2;
                                rb.Apex3.X = x3;
                                rb.Apex3.Y = y3;
                                rb.FreezeLogging = false;
                            };
                        }
                    }




                    rb.FreezeLogging = false;
                }

                GeometryDataGrid.Items.Refresh();

                graphModel.Series.Remove(RuleX);
                graphModel.Series.Remove(RuleY);

                // Stop editing
                indexOfPointToMove = -1;
                points.LineStyle = LineStyle.Solid;
                graphModel.InvalidatePlot(false);
                ev.Handled = true;

            };



        }

        public static void AttachThermopointsEvents(InitConfig config, PlotModel graphModel)
        {
            foreach (var termoBox in config.Geotherms)
            {
                var points = new LineSeries
                {
                    Tag = termoBox.Name,
                    MarkerFill = OxyColors.Orange,
                    MarkerType = MarkerType.Square,
                    MarkerSize = 4,
                    Color = OxyColors.Orange
                };
                points.Points.Add(new DataPoint(termoBox.Apex0.X, termoBox.Apex0.Y));
                points.Points.Add(new DataPoint(termoBox.Apex1.X, termoBox.Apex1.Y));
                points.Points.Add(new DataPoint(termoBox.Apex3.X, termoBox.Apex3.Y));
                points.Points.Add(new DataPoint(termoBox.Apex2.X, termoBox.Apex2.Y));
                points.Points.Add(new DataPoint(termoBox.Apex0.X, termoBox.Apex0.Y));


                var t0 = new PointAnnotation
                {
                    X = termoBox.Apex0.X,
                    Y = termoBox.Apex0.Y,
                    Text = termoBox.T0.ToString(CultureInfo.CurrentCulture),
                    Fill = OxyColors.Transparent,
                    TextColor = OxyColors.Red,
                    Tag = termoBox.Name + "T0"
                };

                var t1 = new PointAnnotation
                {
                    X = termoBox.Apex1.X,
                    Y = termoBox.Apex1.Y,
                    Text = termoBox.T1.ToString(CultureInfo.CurrentCulture),
                    Fill = OxyColors.Transparent,
                    TextColor = OxyColors.Red,
                    Tag = termoBox.Name + "T1"
                };

                var t2 = new PointAnnotation
                {
                    X = termoBox.Apex2.X,
                    Y = termoBox.Apex2.Y,
                    Text = termoBox.T2.ToString(CultureInfo.CurrentCulture),
                    Fill = OxyColors.Transparent,
                    TextColor = OxyColors.Red,
                    Tag = termoBox.Name + "T2"
                };

                var t3 = new PointAnnotation
                {
                    X = termoBox.Apex3.X,
                    Y = termoBox.Apex3.Y,
                    Text = termoBox.T3.ToString(CultureInfo.CurrentCulture),
                    Fill = OxyColors.Transparent,
                    TextColor = OxyColors.Red,
                    Tag = termoBox.Name + "T3"
                };

                graphModel.Annotations.Add(t0);
                graphModel.Annotations.Add(t1);
                graphModel.Annotations.Add(t2);
                graphModel.Annotations.Add(t3);

                AttachMovingEvents(config, graphModel, points);

                points.IsVisible = (GeothermsBox.IsChecked == true);
                foreach (var annotation in graphModel.Annotations)
                {
                    annotation.TextColor = (GeothermsBox.IsChecked == true) ? OxyColors.Red : OxyColors.Transparent;
                }

                AttachChangeEvents(graphModel, termoBox);
                graphModel.Series.Add(points);
            }
        }


        public static void AttachChangeEvents(PlotModel graphModel, Geotherm termoBox)
        {
            termoBox.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == "T0")
                {
                    var geoTherm = sender as Geotherm;
                    if (geoTherm == null) return;

                    PointAnnotation curAnnot =
                        graphModel.Annotations.FirstOrDefault(x => (string)x.Tag == geoTherm.Name + "T0") as PointAnnotation;
                    if (curAnnot == null) return;

                    var newAnot = new PointAnnotation
                    {
                        X = curAnnot.X,
                        Y = curAnnot.Y,
                        Text = geoTherm.T0.ToString(CultureInfo.CurrentCulture),
                        Fill = OxyColors.Transparent,
                        TextColor = OxyColors.Red,
                        Tag = curAnnot.Tag
                    };

                    graphModel.Annotations.Remove(curAnnot);
                    graphModel.Annotations.Add(newAnot);
                    graphModel.InvalidatePlot(false);

                }
                if (args.PropertyName == "T1")
                {
                    var geoTherm = sender as Geotherm;
                    if (geoTherm == null) return;

                    PointAnnotation curAnnot =
                        graphModel.Annotations.FirstOrDefault(x => (string)x.Tag == geoTherm.Name + "T1") as PointAnnotation;
                    if (curAnnot == null) return;

                    var newAnot = new PointAnnotation
                    {
                        X = curAnnot.X,
                        Y = curAnnot.Y,
                        Text = geoTherm.T1.ToString(CultureInfo.CurrentCulture),
                        Fill = OxyColors.Transparent,
                        TextColor = OxyColors.Red,
                        Tag = curAnnot.Tag
                    };

                    graphModel.Annotations.Remove(curAnnot);
                    graphModel.Annotations.Add(newAnot);
                    graphModel.InvalidatePlot(false);
                }
                if (args.PropertyName == "T2")
                {
                    var geoTherm = sender as Geotherm;
                    if (geoTherm == null) return;

                    PointAnnotation curAnnot =
                        graphModel.Annotations.FirstOrDefault(x => (string)x.Tag == geoTherm.Name + "T2") as PointAnnotation;
                    if (curAnnot == null) return;

                    var newAnot = new PointAnnotation
                    {
                        X = curAnnot.X,
                        Y = curAnnot.Y,
                        Text = geoTherm.T2.ToString(CultureInfo.CurrentCulture),
                        Fill = OxyColors.Transparent,
                        TextColor = OxyColors.Red,
                        Tag = curAnnot.Tag
                    };

                    graphModel.Annotations.Remove(curAnnot);
                    graphModel.Annotations.Add(newAnot);
                    graphModel.InvalidatePlot(false);
                }
                if (args.PropertyName == "T3")
                {
                    var geoTherm = sender as Geotherm;
                    if (geoTherm == null) return;

                    PointAnnotation curAnnot =
                        graphModel.Annotations.FirstOrDefault(x => (string)x.Tag == geoTherm.Name + "T3") as PointAnnotation;
                    if (curAnnot == null) return;

                    var newAnot = new PointAnnotation
                    {
                        X = curAnnot.X,
                        Y = curAnnot.Y,
                        Text = geoTherm.T3.ToString(CultureInfo.CurrentCulture),
                        Fill = OxyColors.Transparent,
                        TextColor = OxyColors.Red,
                        Tag = curAnnot.Tag
                    };

                    graphModel.Annotations.Remove(curAnnot);
                    graphModel.Annotations.Add(newAnot);
                    graphModel.InvalidatePlot(false);
                }


                if (args.PropertyName == "Apex0")
                {
                    var geoTherm = sender as Geotherm;
                    if (geoTherm == null) return;

                    LineSeries ptSeries = graphModel.Series.FirstOrDefault(x => x.Tag.ToString() == geoTherm.Name) as LineSeries;
                    if (ptSeries == null) return;

                    ptSeries.Points.RemoveAt(0);
                    ptSeries.Points.Insert(0, new DataPoint(geoTherm.Apex0.X, geoTherm.Apex0.Y));

                    ptSeries.Points.RemoveAt(4);
                    ptSeries.Points.Insert(4, new DataPoint(geoTherm.Apex0.X, geoTherm.Apex0.Y));

                    PointAnnotation curAnnot =
                        graphModel.Annotations.FirstOrDefault(x => (string)x.Tag == geoTherm.Name + "T0") as PointAnnotation;
                    if (curAnnot == null) return;

                    var newAnot = new PointAnnotation
                    {
                        X = geoTherm.Apex0.X,
                        Y = geoTherm.Apex0.Y,
                        Text = geoTherm.T0.ToString(CultureInfo.CurrentCulture),
                        Fill = OxyColors.Transparent,
                        TextColor = OxyColors.Red,
                        Tag = curAnnot.Tag
                    };

                    graphModel.Annotations.Remove(curAnnot);
                    graphModel.Annotations.Add(newAnot);


                    graphModel.InvalidatePlot(false);
                }

                if (args.PropertyName == "Apex1")
                {
                    var geoTherm = sender as Geotherm;
                    if (geoTherm == null) return;

                    LineSeries ptSeries = graphModel.Series.FirstOrDefault(x => x.Tag.ToString() == geoTherm.Name) as LineSeries;
                    if (ptSeries == null) return;

                    ptSeries.Points.RemoveAt(1);
                    ptSeries.Points.Insert(1, new DataPoint(geoTherm.Apex1.X, geoTherm.Apex1.Y));

                    PointAnnotation curAnnot =
                        graphModel.Annotations.FirstOrDefault(x => (string)x.Tag == geoTherm.Name + "T1") as PointAnnotation;
                    if (curAnnot == null) return;

                    var newAnot = new PointAnnotation
                    {
                        X = geoTherm.Apex1.X,
                        Y = geoTherm.Apex1.Y,
                        Text = geoTherm.T1.ToString(CultureInfo.CurrentCulture),
                        Fill = OxyColors.Transparent,
                        TextColor = OxyColors.Red,
                        Tag = curAnnot.Tag
                    };

                    graphModel.Annotations.Remove(curAnnot);
                    graphModel.Annotations.Add(newAnot);

                    graphModel.InvalidatePlot(false);
                }

                if (args.PropertyName == "Apex2")
                {
                    var geoTherm = sender as Geotherm;
                    if (geoTherm == null) return;

                    LineSeries ptSeries = graphModel.Series.FirstOrDefault(x => x.Tag.ToString() == geoTherm.Name) as LineSeries;
                    if (ptSeries == null) return;

                    ptSeries.Points.RemoveAt(3);
                    ptSeries.Points.Insert(3, new DataPoint(geoTherm.Apex2.X, geoTherm.Apex2.Y));

                    PointAnnotation curAnnot =
                        graphModel.Annotations.FirstOrDefault(x => (string)x.Tag == geoTherm.Name + "T2") as PointAnnotation;
                    if (curAnnot == null) return;

                    var newAnot = new PointAnnotation
                    {
                        X = geoTherm.Apex2.X,
                        Y = geoTherm.Apex2.Y,
                        Text = geoTherm.T2.ToString(CultureInfo.CurrentCulture),
                        Fill = OxyColors.Transparent,
                        TextColor = OxyColors.Red,
                        Tag = curAnnot.Tag
                    };

                    graphModel.Annotations.Remove(curAnnot);
                    graphModel.Annotations.Add(newAnot);

                    graphModel.InvalidatePlot(false);
                }

                if (args.PropertyName == "Apex3")
                {
                    var geoTherm = sender as Geotherm;
                    if (geoTherm == null) return;

                    LineSeries ptSeries = graphModel.Series.FirstOrDefault(x => x.Tag.ToString() == geoTherm.Name) as LineSeries;
                    if (ptSeries == null) return;

                    ptSeries.Points.RemoveAt(2);
                    ptSeries.Points.Insert(2, new DataPoint(geoTherm.Apex3.X, geoTherm.Apex3.Y));

                    PointAnnotation curAnnot =
                        graphModel.Annotations.FirstOrDefault(x => (string)x.Tag == geoTherm.Name + "T3") as PointAnnotation;
                    if (curAnnot == null) return;

                    var newAnot = new PointAnnotation
                    {
                        X = geoTherm.Apex3.X,
                        Y = geoTherm.Apex3.Y,
                        Text = geoTherm.T3.ToString(CultureInfo.CurrentCulture),
                        Fill = OxyColors.Transparent,
                        TextColor = OxyColors.Red,
                        Tag = curAnnot.Tag
                    };

                    graphModel.Annotations.Remove(curAnnot);
                    graphModel.Annotations.Add(newAnot);

                    graphModel.InvalidatePlot(false);
                }

            };
        }

        public static void AttachChangeEvents(PlotModel graphModel, RockBox rockBox)
        {
            rockBox.PropertyChanged += (sender, args) =>
            {

                if (args.PropertyName == "RockId")
                {
                    AreaSeries arSerie = graphModel.Series.FirstOrDefault(x => x.Tag.ToString() == rockBox.Name + "area") as AreaSeries;
                    if (arSerie == null) return;

                    var rockInd = (rockBox.RockId >= 0) ? rockBox.RockId : rockBox.RockId * -1;
                    var newRc = GraphConfig.Instace.RocksColor.FirstOrDefault(x => x.Index == rockInd);
                    if (newRc == null) return;
                    arSerie.Fill = newRc.Color;

                    LineSeries ptSeries = graphModel.Series.FirstOrDefault(x => x.Tag.ToString() == rockBox.Name) as LineSeries;
                    if (ptSeries == null) return;
                    ptSeries.MarkerFill = newRc.Color;
                }

                if (args.PropertyName == "Apex0")
                {
                    var rBox = sender as RockBox; //todo по-моему это излишне, можно просто ссылаться на аргумент rockBox. Проверить!
                    if (rBox == null) return;

                    LineSeries ptSeries = graphModel.Series.FirstOrDefault(x => x.Tag.ToString() == rBox.Name) as LineSeries;
                    if (ptSeries == null) return;

                    if (!rBox.FreezeLogging)
                    {
                        var x = ptSeries.Points[0].X;
                        var y = ptSeries.Points[0].Y;

                        var hl = new HistoryLog();
                        hl.Undo = () =>
                        {
                            rBox.FreezeLogging = true;
                            rBox.Apex0.X = x;
                            rBox.Apex0.Y = y;
                            rBox.FreezeLogging = false;
                        };
                        History.Add(hl);
                    }

                    ptSeries.Points.RemoveAt(0);
                    ptSeries.Points.Insert(0, new DataPoint(rBox.Apex0.X, rBox.Apex0.Y));

                    ptSeries.Points.RemoveAt(4);
                    ptSeries.Points.Insert(4, new DataPoint(rBox.Apex0.X, rBox.Apex0.Y));

                    AreaSeries arSerie = graphModel.Series.FirstOrDefault(x => x.Tag.ToString() == rBox.Name + "area") as AreaSeries;
                    if (arSerie == null) return;
                    arSerie.Points.RemoveAt(0);
                    arSerie.Points.Insert(0, new DataPoint(rBox.Apex0.X, rBox.Apex0.Y));

                    graphModel.InvalidatePlot(false);

                }
                if (args.PropertyName == "Apex1")
                {
                    var rBox = sender as RockBox;
                    if (rBox == null) return;

                    LineSeries ptSeries = graphModel.Series.FirstOrDefault(x => x.Tag.ToString() == rBox.Name) as LineSeries;
                    if (ptSeries == null) return;

                    if (!rBox.FreezeLogging)
                    {
                        var hl = new HistoryLog();
                        hl.Undo = () =>
                        {
                            rBox.FreezeLogging = true;
                            rBox.Apex1 = new ModPoint(ptSeries.Points[1].X, ptSeries.Points[1].Y);
                            rBox.FreezeLogging = false;
                        };
                        History.Add(hl);
                    }

                    ptSeries.Points.RemoveAt(1);
                    ptSeries.Points.Insert(1, new DataPoint(rBox.Apex1.X, rBox.Apex1.Y));


                    AreaSeries arSerie = graphModel.Series.FirstOrDefault(x => x.Tag.ToString() == rBox.Name + "area") as AreaSeries;
                    if (arSerie == null) return;
                    arSerie.Points2.RemoveAt(0);
                    arSerie.Points2.Insert(0, new DataPoint(rBox.Apex1.X, rBox.Apex1.Y));

                    graphModel.InvalidatePlot(false);

                }
                if (args.PropertyName == "Apex2")
                {
                    var rBox = sender as RockBox;
                    if (rBox == null) return;

                    LineSeries ptSeries = graphModel.Series.FirstOrDefault(x => x.Tag.ToString() == rBox.Name) as LineSeries;
                    if (ptSeries == null) return;

                    if (!rBox.FreezeLogging)
                    {
                        var x = ptSeries.Points[3].X;
                        var y = ptSeries.Points[3].Y;

                        var hl = new HistoryLog();
                        hl.Undo = () =>
                        {
                            rBox.FreezeLogging = true;
                            rBox.Apex2.X = x;
                            rBox.Apex2.Y = y;
                            rBox.FreezeLogging = false;
                        };
                        History.Add(hl);
                    }

                    ptSeries.Points.RemoveAt(3);
                    ptSeries.Points.Insert(3, new DataPoint(rBox.Apex2.X, rBox.Apex2.Y));

                    AreaSeries arSerie = graphModel.Series.FirstOrDefault(x => x.Tag.ToString() == rBox.Name + "area") as AreaSeries;
                    if (arSerie == null) return;
                    arSerie.Points.RemoveAt(1);
                    arSerie.Points.Insert(1, new DataPoint(rBox.Apex2.X, rBox.Apex2.Y));

                    graphModel.InvalidatePlot(false);



                }
                if (args.PropertyName == "Apex3")
                {
                    var rBox = sender as RockBox;
                    if (rBox == null) return;

                    LineSeries ptSeries = graphModel.Series.FirstOrDefault(x => x.Tag.ToString() == rBox.Name) as LineSeries;
                    if (ptSeries == null) return;

                    if (!rBox.FreezeLogging)
                    {
                        var x = ptSeries.Points[2].X;
                        var y = ptSeries.Points[2].Y;

                        var hl = new HistoryLog
                        {
                            Undo = () =>
                            {
                                rBox.FreezeLogging = true;
                                rBox.Apex3.X = x;
                                rBox.Apex3.Y = y;
                                rBox.FreezeLogging = false;
                            }
                        };

                        History.Add(hl);
                    }

                    ptSeries.Points.RemoveAt(2);
                    ptSeries.Points.Insert(2, new DataPoint(rBox.Apex3.X, rBox.Apex3.Y));

                    AreaSeries arSerie = graphModel.Series.FirstOrDefault(x => x.Tag.ToString() == rBox.Name + "area") as AreaSeries;
                    if (arSerie == null) return;
                    arSerie.Points2.RemoveAt(1);
                    arSerie.Points2.Insert(1, new DataPoint(rBox.Apex3.X, rBox.Apex3.Y));

                    graphModel.InvalidatePlot(false);



                }

            };
        }


        #region Private Methods

        private static DataPoint AdjustDataPoint(DataPoint pt, int xAccuracy, int yAccuracy)
        {
            return new DataPoint(AdjustValue(pt.X, xAccuracy), AdjustValue(pt.Y, yAccuracy));
        }

        private static int AdjustValue(double value, int accuracy)
        {
            var rem = value % accuracy;
            if (rem > accuracy / 2d)
            {
                return Convert.ToInt32((value - rem) + accuracy);
            }
            else
            {
                return Convert.ToInt32(value - rem);
            }
        }

        #endregion

    }
}
