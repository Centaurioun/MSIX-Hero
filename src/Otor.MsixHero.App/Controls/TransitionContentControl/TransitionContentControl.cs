﻿// MSIX Hero
// Copyright (C) 2024 Marcin Otorowski
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// Full notice:
// https://github.com/marcinotorowski/msix-hero/blob/develop/LICENSE.md

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

// This control is gratefully borrowed from http://blog.landdolphin.net/?p=17
// Thanks guys!
namespace Otor.MsixHero.App.Controls.TransitionContentControl
{
    /// <summary>
    /// A ContentControl that animates the transition when its content is changed.
    /// </summary>
    [TemplatePart(Name = "PART_Container", Type = typeof(FrameworkElement))]
    [TemplatePart(Name = "PART_PreviousImageSite", Type = typeof(Image))]
    [TemplatePart(Name = "PART_CurrentContentPresentationSite", Type = typeof(ContentPresenter))]
    [TemplateVisualState(Name = NormalState, GroupName = PresentationGroup)]
    public class TransitioningContentControl : ContentControl
    {
        /// <summary>
        /// <see cref="DependencyProperty"/> for the <see cref="Transition"/> property.
        /// </summary>
        public static readonly DependencyProperty TransitionProperty = DependencyProperty.RegisterAttached(
            nameof(Transition),
            typeof(TransitionType),
            typeof(TransitioningContentControl),
            new PropertyMetadata(TransitionType.Fade));

        /// <summary>
        /// <see cref="DependencyProperty"/> for the <see cref="Transition"/> property.
        /// </summary>
        public static readonly DependencyProperty TransitionDirectionProperty = DependencyProperty.RegisterAttached(
            nameof(Direction),
            typeof(TransitionDirection),
            typeof(TransitioningContentControl),
            new PropertyMetadata(TransitionDirection.Left));

        /// <summary>
        /// <see cref="DependencyProperty"/> for the <see cref="Transition"/> property.
        /// </summary>
        public static readonly DependencyProperty TransitionDurationProperty = DependencyProperty.RegisterAttached(
            nameof(Duration),
            typeof(TimeSpan),
            typeof(TransitioningContentControl),
            new PropertyMetadata(TimeSpan.FromSeconds(0.3)));

        private const string PresentationGroup = "PresentationStates";
        private const string NormalState = "Normal";
        private bool _isTransitioning;
        private Storyboard _startingTransition;
        private Storyboard _completingTransition;
        private Grid _container;
        private Image _previousImageSite;
        private ContentPresenter _currentContentPresentationSite;

        /// <summary>
        /// Initializes a new instance of the <see cref="TransitioningContentControl"/> class.
        /// </summary>
        public TransitioningContentControl()
        {
            DefaultStyleKey = typeof(TransitioningContentControl);
        }

        /// <summary>
        /// Occurs when a transition has completed.
        /// </summary>
        public event RoutedEventHandler TransitionCompleted;

        /// <summary>
        /// Occurs when a transition has started.
        /// </summary>
        public event RoutedEventHandler TransitionStarted;

        /// <summary>
        /// Represents the type of transition that a TransitioningContentControl will perform.
        /// </summary>
        public enum TransitionType
        {
            /// <summary>
            /// A simple fading transition.
            /// </summary>
            Fade,

            /// <summary>
            /// A transition that slides old content out of view, and slides new content back in from the same direction.
            /// </summary>
            Move,

            /// <summary>
            /// A transition that keeps old content in view, and slides new content over.
            /// </summary>
            Slide,

            /// <summary>
            /// A transition that slides old content in view, and slider new content over it a little distance while changing opacity.
            /// </summary>
            Drop,

            /// <summary>
            /// A transition that slides old content out of view, then slides new content back in from the opposite direction.
            /// </summary>
            Bounce
        }

        /// <summary>
        /// Represents the type of transition that a TransitioningContentControl will perform.
        /// </summary>
        public enum TransitionDirection
        {
            /// <summary>
            /// Up direction.
            /// </summary>
            Up,

            /// <summary>
            /// Down direction.
            /// </summary>
            Down,

            /// <summary>
            /// Left direction.
            /// </summary>
            Left,

            /// <summary>
            /// Right direction.
            /// </summary>
            Right
        }

        /// <summary>
        /// Gets or sets the transition.
        /// </summary>
        /// <value>The transition.</value>
        public TransitionType Transition { get => (TransitionType)GetValue(TransitionProperty); set => SetValue(TransitionProperty, value); }

        /// <summary>
        /// Gets or sets the transition direction.
        /// </summary>
        /// <value>The direction.</value>
        public TransitionDirection Direction { get => (TransitionDirection)GetValue(TransitionDirectionProperty); set => SetValue(TransitionDirectionProperty, value); }

        /// <summary>
        /// Gets or sets the transition duration.
        /// </summary>
        /// <value>The duration.</value>
        public TimeSpan Duration { get => (TimeSpan)GetValue(TransitionDurationProperty); set => SetValue(TransitionDurationProperty, value); }

        private Storyboard StartingTransition
        {
            get => _startingTransition;
            set
            {
                _startingTransition = value;
                if (_startingTransition != null)
                {
                    SetTransitionDefaultValues();
                }
            }
        }

        private Storyboard CompletingTransition
        {
            get => _completingTransition;
            set
            {
                // Decouple transition.
                if (_completingTransition != null)
                {
                    _completingTransition.Completed -= OnTransitionCompleted;
                }

                _completingTransition = value;

                if (_completingTransition != null)
                {
                    _completingTransition.Completed += OnTransitionCompleted;
                    SetTransitionDefaultValues();
                }
            }
        }

        /// <inheritdoc/>
        public override void OnApplyTemplate()
        {
            // Wire up all of the various control parts.
            _container = GetTemplateChild("PART_Container") as Grid;
            if (_container == null)
            {
                throw new ArgumentException("PART_Container not found.");
            }

            _currentContentPresentationSite = GetTemplateChild("PART_CurrentContentPresentationSite") as ContentPresenter;
            if (_currentContentPresentationSite == null)
            {
                throw new ArgumentException("PART_CurrentContentPresentationSite not found.");
            }

            _previousImageSite = GetTemplateChild("PART_PreviousImageSite") as Image;

            // Set the current content site to the first piece of content.
            _currentContentPresentationSite.Content = Content;
            VisualStateManager.GoToState(this, NormalState, false);
        }

        /// <summary>
        /// Called when the value of the <see cref="ContentControl.Content"/> property changes.
        /// </summary>
        /// <param name="oldContent">The old value of the <see cref="ContentControl.Content"/> property.</param>
        /// <param name="newContent">The new value of the <see cref="ContentControl.Content"/> property.</param>
        protected override void OnContentChanged(object oldContent, object newContent)
        {
            QueueTransition(newContent);
            base.OnContentChanged(oldContent, newContent);
        }

        private static RenderTargetBitmap GetRenderTargetBitmapFromUiElement(UIElement uiElement)
        {
            if (uiElement.RenderSize.Height == 0)
            {
                return default!;
            }

            DpiScale dpiScale = VisualTreeHelper.GetDpi(uiElement);

            var renderTargetBitmap = new RenderTargetBitmap(
                                                            Convert.ToInt32(uiElement.RenderSize.Width * dpiScale.DpiScaleX),
                                                            Convert.ToInt32(uiElement.RenderSize.Height * dpiScale.DpiScaleY),
                                                            dpiScale.PixelsPerInchX,
                                                            dpiScale.PixelsPerInchY,
                                                            PixelFormats.Pbgra32);

            renderTargetBitmap.Render(uiElement);
            renderTargetBitmap.Freeze();

            return renderTargetBitmap;
        }

        /// <summary>
        /// Aborts the transition.
        /// </summary>
        private void AbortTransition()
        {
            // Go to a normal state and release our hold on the old content.
            VisualStateManager.GoToState(this, NormalState, false);
            _isTransitioning = false;

            if (_previousImageSite != null)
            {
                if (_previousImageSite.Source is RenderTargetBitmap renderTargetBitmap)
                {
                    renderTargetBitmap.Clear();
                }

                // https://github.com/dotnet/wpf/issues/2397
                _previousImageSite.Source = null;
                _previousImageSite.UpdateLayout();
            }
        }

        private void OnTransitionCompleted(object sender, EventArgs e)
        {
            AbortTransition();

            TransitionCompleted?.Invoke(this, new RoutedEventArgs());
        }

        private void RaiseTransitionStarted()
        {
            TransitionStarted?.Invoke(this, new RoutedEventArgs());
        }

        private void QueueTransition(object newContent)
        {
            // Both ContentPresenters must be available, otherwise a transition is useless.
            if (_currentContentPresentationSite == null)
            {
                return;
            }

            if (_isTransitioning || _previousImageSite == null)
            {
                _currentContentPresentationSite.Content = newContent;
                return;
            }

            _previousImageSite.Source = GetRenderTargetBitmapFromUiElement(_currentContentPresentationSite);

            _currentContentPresentationSite.Content = newContent;
            string startingTransitionName;

            if (Transition == TransitionType.Bounce)
            {
                // Wire up the completion transition.
                var transitionInName = $"Transition_{Transition}{Direction}In";
                CompletingTransition = GetTransitionStoryboardByName(transitionInName);

                // Wire up the first transition to start the second transition when it's complete.
                startingTransitionName = $"Transition_{Transition}{Direction}Out";
                Storyboard transitionOut = GetTransitionStoryboardByName(startingTransitionName);

                transitionOut.Completed += (sender, args) => VisualStateManager.GoToState(
                    this,
                    transitionInName,
                    false);

                StartingTransition = transitionOut;
            }
            else
            {
                startingTransitionName = Transition == TransitionType.Fade
                                             ? "Transition_Fade"
                                             : $"Transition_{Transition}{Direction}";

                CompletingTransition = GetTransitionStoryboardByName(startingTransitionName);
            }

            // Start the transition.
            _isTransitioning = true;
            RaiseTransitionStarted();

            Console.Out.WriteLine("Go to state " + startingTransitionName);
            VisualStateManager.GoToState(
                                         this,
                                         startingTransitionName,
                                         false);
        }

        private Storyboard GetTransitionStoryboardByName(string transitionName)
        {
            // Hook up the CurrentTransition.
            var presentationGroup =
                ((IEnumerable<VisualStateGroup>)VisualStateManager.GetVisualStateGroups(_container!))!.FirstOrDefault(o => o.Name == PresentationGroup);
            if (presentationGroup == null)
            {
                throw new ArgumentException("Invalid VisualStateGroup.");
            }

            var transition =
                ((IEnumerable<VisualState>)presentationGroup.States).Where(o => o.Name == transitionName).Select(
                    o => o.Storyboard).FirstOrDefault();
            if (transition == null)
            {
                throw new ArgumentException("Invalid transition");
            }

            return transition;
        }

        /// <summary>
        /// Sets default values for certain transition types.
        /// </summary>
        private void SetTransitionDefaultValues()
        {
            // Do some special handling of particular transitions so that we get nice smooth transitions that utilise the size of the content.
            switch (Transition)
            {
                case TransitionType.Fade:
                    {
                        if (CompletingTransition is null)
                        {
                            return;
                        }

                        var completingDoubleAnimation = (DoubleAnimation)CompletingTransition.Children[0];
                        completingDoubleAnimation.Duration = Duration;
                        var startingDoubleAnimation = (DoubleAnimation)CompletingTransition.Children[1];
                        startingDoubleAnimation.Duration = Duration;

                        break;
                    }

                case TransitionType.Slide:
                    {
                        if (CompletingTransition is null)
                        {
                            return;
                        }

                        var startingDoubleAnimation = (DoubleAnimation)CompletingTransition.Children[0];
                        startingDoubleAnimation.Duration = Duration;

                        startingDoubleAnimation.From = Direction switch
                        {
                            TransitionDirection.Down => -ActualHeight,
                            TransitionDirection.Up => ActualHeight,
                            TransitionDirection.Right => -ActualWidth,
                            TransitionDirection.Left => ActualWidth,
                            _ => throw new ArgumentOutOfRangeException(nameof(TransitionDirection))
                        };

                        break;
                    }

                case TransitionType.Move:
                    {
                        if (CompletingTransition is null)
                        {
                            return;
                        }

                        var completingDoubleAnimation = (DoubleAnimation)CompletingTransition.Children[0];
                        var startingDoubleAnimation = (DoubleAnimation)CompletingTransition.Children[1];
                        startingDoubleAnimation.Duration = Duration;
                        completingDoubleAnimation.Duration = Duration;

                        switch (Direction)
                        {
                            case TransitionDirection.Down:
                                startingDoubleAnimation.To = ActualHeight;
                                completingDoubleAnimation.From = -ActualHeight;

                                break;
                            case TransitionDirection.Up:
                                startingDoubleAnimation.To = -ActualHeight;
                                completingDoubleAnimation.From = ActualHeight;

                                break;
                            case TransitionDirection.Right:
                                startingDoubleAnimation.To = ActualWidth;
                                completingDoubleAnimation.From = -ActualWidth;

                                break;
                            case TransitionDirection.Left:
                                startingDoubleAnimation.To = -ActualWidth;
                                completingDoubleAnimation.From = ActualWidth;

                                break;
                            default: throw new ArgumentOutOfRangeException(nameof(TransitionDirection));
                        }

                        break;
                    }

                case TransitionType.Bounce:
                    {
                        if (CompletingTransition != null)
                        {
                            var completingDoubleAnimation = (DoubleAnimationUsingKeyFrames)CompletingTransition.Children[0];
                            completingDoubleAnimation.KeyFrames[1].Value = ActualHeight;
                        }

                        if (StartingTransition is null)
                        {
                            return;
                        }

                        var startingDoubleAnimation = (DoubleAnimation)StartingTransition.Children[0];

                        startingDoubleAnimation.To = Direction switch
                        {
                            TransitionDirection.Down => ActualHeight,
                            TransitionDirection.Up => -ActualHeight,
                            TransitionDirection.Right => ActualWidth,
                            TransitionDirection.Left => -ActualWidth,
                            _ => throw new ArgumentOutOfRangeException(nameof(TransitionDirection))
                        };

                        break;
                    }

                case TransitionType.Drop: break;
                default: throw new ArgumentOutOfRangeException(nameof(TransitionDirection));
            }
        }
    }
}