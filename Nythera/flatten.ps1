$content = Get-Content 'MainPage.xaml' -Raw

$content = $content.Replace('Background="{ThemeResource ApplicationPageBackgroundThemeBrush}"', 'Background="{ThemeResource CardBackgroundFillColorDefaultBrush}" BorderBrush="{ThemeResource CardStrokeColorDefaultBrush}" BorderThickness="1"')

# Remove inner backgrounds and borders
$content = $content.Replace('Background="{ThemeResource CardBackgroundFillColorDefaultBrush}" `r`n                                BorderBrush="{ThemeResource CardStrokeColorDefaultBrush}"`r`n                                BorderThickness="1" CornerRadius="8" ', '')

$content = $content.Replace('Background="{ThemeResource CardBackgroundFillColorDefaultBrush}" `r`n                        BorderBrush="{ThemeResource CardStrokeColorDefaultBrush}"`r`n                        BorderThickness="1" CornerRadius="8" Padding="32" Margin="0,32,0,48"', 'Padding="24"')

# Update Page 2 settings card
$content = $content.Replace('Background="{ThemeResource CardBackgroundFillColorDefaultBrush}" `r`n                                BorderBrush="{ThemeResource CardStrokeColorDefaultBrush}"`r`n                                BorderThickness="1" CornerRadius="8" Padding="20"', 'Padding="0"')

# Update padding for stack panels
$content = $content.Replace('Padding="0,32,0,48"', 'Padding="24"')
$content = $content.Replace('Padding="20"', 'Padding="0"')

Set-Content 'MainPage.xaml' $content -NoNewline
