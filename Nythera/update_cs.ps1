$content = Get-Content 'MainPage.xaml.cs' -Raw
$start = $content.IndexOf('    private void NavigateToPage(int newIndex)')
$end = $content.IndexOf('    private void LanguageComboBox_SelectionChanged')

$newCode = @"
    private void NavigateToPage(int newIndex)
    {
        if (newIndex == _currentPageIndex) return;

        _currentPageIndex = newIndex;

        UIElement[] pages = { Page1, Page2, Page3 };
        int numPages = pages.Length;

        for (int i = 0; i < numPages; i++)
        {
            int offset = i - newIndex;
            // Wrap the offset so it is always -1, 0, or 1 for 3 pages
            if (offset > 1) offset -= numPages;
            if (offset < -1) offset += numPages;

            double targetScale = offset == 0 ? 1.0 : 0.8;
            double targetOpacity = offset == 0 ? 1.0 : 0.4;
            double targetX = offset * 220;
            int zIndex = offset == 0 ? 2 : 1;

            AnimateCoverFlow(pages[i], targetScale, targetX, targetOpacity, zIndex);
            
            pages[i].IsHitTestVisible = (offset == 0);
        }
    }

    private void AnimateCoverFlow(UIElement element, double targetScale, double targetTranslateX, double targetOpacity, int zIndex)
    {
        Canvas.SetZIndex(element, zIndex);

        var transform = element.RenderTransform as CompositeTransform;
        if (transform == null) return;

        transform.Rotation = 0;

        var storyboard = new Storyboard();
        var duration = new Duration(TimeSpan.FromMilliseconds(500));
        var easing = new ExponentialEase { EasingMode = EasingMode.EaseOut, Exponent = 4.0 };

        var transXAnim = new DoubleAnimation { To = targetTranslateX, Duration = duration, EasingFunction = easing };
        var scaleXAnim = new DoubleAnimation { To = targetScale, Duration = duration, EasingFunction = easing };
        var scaleYAnim = new DoubleAnimation { To = targetScale, Duration = duration, EasingFunction = easing };
        var opacityAnim = new DoubleAnimation { To = targetOpacity, Duration = duration, EasingFunction = easing };

        Storyboard.SetTarget(transXAnim, transform);
        Storyboard.SetTargetProperty(transXAnim, "TranslateX");

        Storyboard.SetTarget(scaleXAnim, transform);
        Storyboard.SetTargetProperty(scaleXAnim, "ScaleX");

        Storyboard.SetTarget(scaleYAnim, transform);
        Storyboard.SetTargetProperty(scaleYAnim, "ScaleY");

        Storyboard.SetTarget(opacityAnim, element);
        Storyboard.SetTargetProperty(opacityAnim, "Opacity");

        storyboard.Children.Add(transXAnim);
        storyboard.Children.Add(scaleXAnim);
        storyboard.Children.Add(scaleYAnim);
        storyboard.Children.Add(opacityAnim);
        
        storyboard.Begin();
    }

"@

$newContent = $content.Substring(0, $start) + $newCode + $content.Substring($end)
Set-Content 'MainPage.xaml.cs' $newContent -NoNewline
