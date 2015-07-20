using System;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

namespace PhoneCharmControl
{
    public class Charm : ContentControl
    {
        class BindingParameterHolder : DependencyObject
        {
            private Charm _host;

            public BindingParameterHolder(Charm host)
            {
                _host = host;
            }

            public static readonly DependencyProperty OpacityValueProperty = DependencyProperty.Register(
                "OpacityValue", typeof(double), typeof(BindingParameterHolder), new PropertyMetadata(default(double), OpacityValueChanged));

            public double OpacityValue
            {
                get { return (double)GetValue(OpacityValueProperty); }
                set { SetValue(OpacityValueProperty, value); }
            }

            public static readonly DependencyProperty OffsetValueProperty = DependencyProperty.Register(
                "OffsetValue", typeof(double), typeof(BindingParameterHolder), new PropertyMetadata(default(double), OffsetValueChanged));

            public double OffsetValue
            {
                get { return (double)GetValue(OffsetValueProperty); }
                set { SetValue(OffsetValueProperty, value); }
            }

            private static void OpacityValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            {
                var element = (d as BindingParameterHolder)._host.ParentContentElement;
                if (element == null) return;
                element.Opacity = (double)e.NewValue;
            }

            private static void OffsetValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            {
                var element = (d as BindingParameterHolder)._host.ParentContentElement;
                if (element == null) return;
                var transform = element.RenderTransform as CompositeTransform;
                if (transform == null) return;
                transform.ScaleX = transform.ScaleY = 1.0 + ((double)e.NewValue / 800.0);
            }
        }

        public class DepthConverter : IValueConverter
        {
            private Charm _host;

            public DepthConverter(Charm host)
            {
                _host = host;
            }

            public object Convert(object value, Type targetType, object parameter, string culture)
            {
                var d = (double)value;
                if (_host.EdgePlacement == HorizontalAlignment.Left)
                {
                    d += _host.CharmWidth * 2;
                    if (d < 0) return 0;
                    return -64 * (d / _host.CharmWidth);
                }
                if (d > _host.CharmWidth * 2) return 0;
                return 64 * (d / _host.CharmWidth);
            }

            public object ConvertBack(object value, Type targetType, object parameter, string culture)
            {
                throw new NotImplementedException();
            }
        }

        public class OpacityConverter : IValueConverter
        {
            private Charm _host;

            public OpacityConverter(Charm host)
            {
                _host = host;
            }

            public object Convert(object value, Type targetType, object parameter, string culture)
            {
                var d = (double)value;
                if (_host.EdgePlacement == HorizontalAlignment.Left)
                {
                    d += _host.CharmWidth;
                    return 1.0 - (_host.CharmWidth + d) / _host.CharmWidth * 0.4;

                }
                return 1.0 + d / _host.CharmWidth * 0.4;
            }

            public object ConvertBack(object value, Type targetType, object parameter, string culture)
            {
                throw new NotImplementedException();
            }
        }

        public static readonly DependencyProperty EdgePlacementProperty = DependencyProperty.Register(
            "EdgePlacement", typeof(HorizontalAlignment), typeof(Charm), new PropertyMetadata(default(HorizontalAlignment), EdgePlacementChanged));

        public HorizontalAlignment EdgePlacement
        {
            get { return (HorizontalAlignment)GetValue(EdgePlacementProperty); }
            set { SetValue(EdgePlacementProperty, value); }
        }

        public static readonly DependencyProperty CharmWidthProperty = DependencyProperty.Register(
            "CharmWidth", typeof(double), typeof(Charm), new PropertyMetadata(default(double), CharmWidthChanged));

        public double CharmWidth
        {
            get { return (double)GetValue(CharmWidthProperty); }
            set { SetValue(CharmWidthProperty, value); }
        }

        public static readonly DependencyProperty ParentContentElementProperty = DependencyProperty.Register(
            "ParentContentElement", typeof(UIElement), typeof(Charm), new PropertyMetadata(default(UIElement)));

        public UIElement ParentContentElement
        {
            get { return (UIElement)GetValue(ParentContentElementProperty); }
            set { SetValue(ParentContentElementProperty, value); }
        }

        private static void CharmWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as Charm).CharmWidthChanged(e);
        }

        private void CharmWidthChanged(DependencyPropertyChangedEventArgs e)
        {
            if (_leftMenu != null && (_leftMenu.RenderTransform as TranslateTransform) != null)
                (_leftMenu.RenderTransform as TranslateTransform).X = EdgePlacement == HorizontalAlignment.Left ? -(((double)e.NewValue) * 2) - 1 : 0;
            if (_menuContent != null) _menuContent.Width = ((double)e.NewValue) * 2;
        }

        private static void EdgePlacementChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = d as Charm;
            var presenter = self != null ? self._presenter : null;
            if (presenter != null)
                presenter.HorizontalAlignment = (HorizontalAlignment)e.NewValue == HorizontalAlignment.Left
                    ? HorizontalAlignment.Right
                    : HorizontalAlignment.Left;
        }

        public bool IsOpen { get { return _leftMenu.IsOpen; } }

        private static object _charmUsed;

        private Border _edge;
        private Border _cover;
        private Popup _leftMenu;
        private Border _menuContent;
        private ContentPresenter _presenter;

        private BindingParameterHolder _holder;

        public Charm()
        {
            DefaultStyleKey = typeof(Charm);
            _holder = new BindingParameterHolder(this);
        }

        protected override void OnApplyTemplate()
        {
            _edge = (Border)GetTemplateChild("Edge");
            _cover = (Border)GetTemplateChild("Cover");
            _leftMenu = (Popup)GetTemplateChild("LeftMenu");
            _menuContent = (Border)GetTemplateChild("MenuContent");
            _presenter = (ContentPresenter)GetTemplateChild("Presenter");
            Loaded += ThisLoaded;
            //
            _edge.PointerPressed += EdgeDetectorPressed;
            _edge.PointerMoved += EdgeDetectorPointerMoved;
            _edge.PointerReleased += EdgeDetectorReleased;
            //
            _cover.Tapped += LightDissmissCoverTapped;
            _cover.PointerPressed += EdgeDetectorPressed;
            _cover.PointerMoved += EdgeDetectorPointerMoved;
            _cover.PointerReleased += EdgeDetectorReleased;
            //
            _menuContent.PointerPressed += EdgeDetectorPressed;
            _menuContent.PointerMoved += EdgeDetectorPointerMoved;
            _menuContent.PointerReleased += EdgeDetectorReleased;
            //
            (_leftMenu.RenderTransform as TranslateTransform).X = EdgePlacement == HorizontalAlignment.Left ? -(CharmWidth * 2) - 1 : 0;
            _menuContent.Width = CharmWidth * 2;
            _presenter.HorizontalAlignment = EdgePlacement == HorizontalAlignment.Left
                ? HorizontalAlignment.Right : HorizontalAlignment.Left;
            //
            var opacity = new Binding
            {
                Converter = new OpacityConverter(this),
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Path = new PropertyPath("RenderTransform.(TranslateTransform.X)"),
                Source = _leftMenu
            };
            BindingOperations.SetBinding(_holder, BindingParameterHolder.OpacityValueProperty, opacity);
            //
            var emulatedOffsetZ = new Binding
            {
                Converter = new DepthConverter(this),
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Path = new PropertyPath("RenderTransform.(TranslateTransform.X)"),
                Source = _leftMenu
            };
            BindingOperations.SetBinding(_holder, BindingParameterHolder.OffsetValueProperty, emulatedOffsetZ);
        }

        void ThisLoaded(object sender, RoutedEventArgs e)
        {
            if (ParentContentElement == null)
            {
                ParentContentElement = (UIElement)Parent;
            }
        }

        private bool _leftButtonDown;
        private Point _initPoint;
        private Point _prevPoint;
        private bool _pullRight;
        private double _initLeft;
        private Visibility _prevVisibility;

        private void EdgeDetectorPressed(object sender, PointerRoutedEventArgs e)
        {
            _charmUsed = this;
            var appBar = GetCurrentPage().BottomAppBar;
            if (appBar != null)
            {
                _prevVisibility = appBar.Visibility;
                if (appBar.Visibility != Visibility.Collapsed)
                {
                    SetBottomAppBarVisibility(Visibility.Collapsed);
                }

            }
            _leftButtonDown = true;
            _initPoint = e.GetCurrentPoint(this).Position;
            _prevPoint = _initPoint;
            _initLeft = (_leftMenu.RenderTransform as TranslateTransform).X;
            var systemTray = StatusBar.GetForCurrentView();
            var trayHeight = systemTray.OccludedRect.Height;
            _menuContent.Height = Window.Current.Bounds.Height;
            _menuContent.Padding = new Thickness(0, trayHeight, 0, 0);
            (_leftMenu.RenderTransform as TranslateTransform).Y = -trayHeight;
            _leftMenu.IsOpen = true;
            (sender as UIElement).CapturePointer(e.Pointer);
        }

        private void EdgeDetectorPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!_leftButtonDown) return;
            var pt = e.GetCurrentPoint(this);
            var dx = pt.Position.X - _initPoint.X;
            if (EdgePlacement == HorizontalAlignment.Left)
            {
                if (dx < 0)
                    dx = -(_initLeft - dx);
                else if (dx + _initLeft >= -CharmWidth)
                    dx = CharmWidth + ((dx + _initLeft) + CharmWidth) / 4;
                var x = Math.Min(CharmWidth * 2 + 1, dx);
                (_leftMenu.RenderTransform as TranslateTransform).X = Math.Max(-(CharmWidth * 2) - 1, -(CharmWidth * 2) + x - 1);
            }
            else
            {
                if (dx > 0)
                    dx = (_initLeft + dx);
                else if (dx + _initLeft <= -CharmWidth)
                    dx = -CharmWidth + ((dx + _initLeft) + CharmWidth) / 4;
                var x = Math.Max(-CharmWidth * 2, dx);
                (_leftMenu.RenderTransform as TranslateTransform).X = Math.Min(0, x);
            }
            var n = _prevPoint.X - pt.Position.X;
            if (n == 0) return;
            if (EdgePlacement == HorizontalAlignment.Right) n = -n;
            _pullRight = n < 0;
            _prevPoint = pt.Position;
        }

        private void EdgeDetectorReleased(object sender, PointerRoutedEventArgs e)
        {
            _leftButtonDown = false;
            var pt = e.GetCurrentPoint(this);
            if (_initPoint.Equals(pt.Position))
            {
                SetBottomAppBarVisibility(_prevVisibility);
                _charmUsed = null;
                return;
            }
            StartEdgeAnimation(_pullRight);
            if (!_pullRight)
            {
                _charmUsed = null;
            }
            _cover.Visibility = _pullRight ? Visibility.Visible : Visibility.Collapsed;
        }

        private void LightDissmissCoverTapped(object sender, TappedRoutedEventArgs e)
        {
            CloseMenu();
        }

        private void StartEdgeAnimation(bool open)
        {
            if (!open) SetBottomAppBarVisibility(Visibility.Visible);
            var sb = new Storyboard();
            // left offset
            var to = open ? -CharmWidth : -(CharmWidth * 2) - 1;
            if (EdgePlacement == HorizontalAlignment.Right) to = open ? -CharmWidth : 0;
            var aTranslate = new DoubleAnimation
            {
                EasingFunction = new CircleEase { EasingMode = EasingMode.EaseOut },
                From = (_leftMenu.RenderTransform as TranslateTransform).X,
                To = to,
                Duration = TimeSpan.FromMilliseconds(200),
                EnableDependentAnimation = true
            };
            Storyboard.SetTarget(aTranslate, _leftMenu);
            Storyboard.SetTargetProperty(aTranslate, "(UIElement.RenderTransform).(TranslateTransform.X)");
            sb.Children.Add(aTranslate);
            // opacity
            var aOpacity = new DoubleAnimation
            {
                EasingFunction = new CircleEase { EasingMode = EasingMode.EaseOut },
                From = ParentContentElement.Opacity,
                To = open ? 0.6 : 1,
                Duration = TimeSpan.FromMilliseconds(200),
                EnableDependentAnimation = true
            };
            Storyboard.SetTarget(aOpacity, ParentContentElement);
            Storyboard.SetTargetProperty(aOpacity, "Opacity");
            sb.Children.Add(aOpacity);
            // visibility
            if (!open)
            {
                var aVisibility = new ObjectAnimationUsingKeyFrames();
                aVisibility.KeyFrames.Add(new DiscreteObjectKeyFrame
                {
                    KeyTime = TimeSpan.FromMilliseconds(200),
                    Value = false
                });
                Storyboard.SetTarget(aVisibility, _leftMenu);
                Storyboard.SetTargetProperty(aVisibility, "IsOpen");
                sb.Children.Add(aVisibility);
            }
            // depth offset
            var trans = ParentContentElement.RenderTransform as CompositeTransform;
            if (trans != null)
            {
                var aScaleX = new DoubleAnimation
                {
                    EasingFunction = new CircleEase { EasingMode = EasingMode.EaseOut },
                    From = trans.ScaleX,
                    To = open ? 0.92 : 1.0,
                    Duration = TimeSpan.FromMilliseconds(200),
                    EnableDependentAnimation = true
                };
                Storyboard.SetTarget(aScaleX, ParentContentElement);
                Storyboard.SetTargetProperty(aScaleX, "(UIElement.RenderTransform).(CompositeTransform.ScaleX)");
                sb.Children.Add(aScaleX);
                var aScaleY = new DoubleAnimation
                {
                    EasingFunction = new CircleEase { EasingMode = EasingMode.EaseOut },
                    From = trans.ScaleY,
                    To = open ? 0.92 : 1.0,
                    Duration = TimeSpan.FromMilliseconds(200),
                    EnableDependentAnimation = true
                };
                Storyboard.SetTarget(aScaleY, ParentContentElement);
                Storyboard.SetTargetProperty(aScaleY, "(UIElement.RenderTransform).(CompositeTransform.ScaleY)");
                sb.Children.Add(aScaleY);                
            }
            if (!open)
            {
                sb.Completed += sb_Completed;
            }
            sb.Begin();
        }

        void sb_Completed(object sender, object e)
        {
            (sender as Storyboard).Completed -= sb_Completed;
            Dispatcher.RunIdleAsync(_ =>
            {
                var transform = ParentContentElement.RenderTransform as CompositeTransform;
                if (transform != null) transform.ScaleX = transform.ScaleY = 1.0;
            });
        }

        private void OpenMenu()
        {
            StartEdgeAnimation(true);
            _cover.Visibility = Visibility.Visible;
            _charmUsed = null;
        }

        private void CloseMenu()
        {
            StartEdgeAnimation(false);
            _cover.Visibility = Visibility.Collapsed;
            _charmUsed = null;
        }

        private void SetBottomAppBarVisibility(Visibility visibility)
        {
            var appBar = GetCurrentPage().BottomAppBar;
            if (appBar != null)
            {
                appBar.Visibility = visibility;
            }
        }

        private Page GetCurrentPage()
        {
            var rootFrame = Window.Current.Content as Frame;
            return rootFrame.Content as Page;
        }

        public void Open()
        {
            OpenMenu();
        }

        public void Close()
        {
            CloseMenu();
        }
    }
}
