$content = Get-Content 'MainPage.xaml.cs' -Raw

$oldOpacity = "double targetOpacity = offset == 0 ? 1.0 : 0.4;"
$newOpacity = "double targetOpacity = offset == 0 ? 1.0 : 0.4;`r`n            double blurOpacity = offset == 0 ? 0.0 : 0.6;"

$content = $content.Replace($oldOpacity, $newOpacity)

$oldAnim = "    private void AnimateCoverFlow(UIElement element, double targetScale, double targetTranslateX, double targetOpacity, int zIndex)`r`n    {"
$newAnim = "    private void AnimateCoverFlow(UIElement element, double targetScale, double targetTranslateX, double targetOpacity, int zIndex, double blurOpacity = 0.0)`r`n    {"
$content = $content.Replace($oldAnim, $newAnim)

$oldCall = "AnimateCoverFlow(pages[i], targetScale, targetX, targetOpacity, zIndex);"
$newCall = "AnimateCoverFlow(pages[i], targetScale, targetX, targetOpacity, zIndex, blurOpacity);"
$content = $content.Replace($oldCall, $newCall)

$oldAdd = "        storyboard.Children.Add(opacityAnim);"
$newAdd = @"
        var blurOverlay = (Border)element.FindName(((FrameworkElement)element).Name + "BlurOverlay");
        if (blurOverlay != null)
        {
            var blurAnim = new DoubleAnimation { To = blurOpacity, Duration = duration, EasingFunction = easing };
            Storyboard.SetTarget(blurAnim, blurOverlay);
            Storyboard.SetTargetProperty(blurAnim, "Opacity");
            storyboard.Children.Add(blurAnim);
        }
        storyboard.Children.Add(opacityAnim);
"@

$content = $content.Replace($oldAdd, $newAdd)

Set-Content 'MainPage.xaml.cs' $content -NoNewline
