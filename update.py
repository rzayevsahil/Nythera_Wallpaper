import re

with open("Nythera/MainPage.xaml", "r", encoding="utf-8") as f:
    content = f.read()

# Extract header to update card
pattern1 = r"(<!-- Header -->.*?<!-- About Expander -->)"
match1 = re.search(pattern1, content, re.DOTALL)
page1_content = match1.group(1).strip()
page1_content = page1_content.replace("<!-- About Expander -->", "").strip()

# Extract developer info and app info
pattern2 = r"(<!-- App Info Section -->.*?</StackPanel>\s*</Grid>\s*</StackPanel>)"
match2 = re.search(pattern2, content, re.DOTALL)
page2_content = match2.group(1).strip()

new_content = f"""    <Grid x:Name="RootGrid">
        <Grid HorizontalAlignment="Stretch" VerticalAlignment="Stretch">
            
            <!-- PAGE 2: About (Starts in background) -->
            <Grid x:Name="Page2" Width="480" VerticalAlignment="Center" HorizontalAlignment="Center" 
                  Opacity="0.3" Canvas.ZIndex="0" RenderTransformOrigin="0.5,0.5" IsHitTestVisible="False">
                <Grid.RenderTransform>
                    <CompositeTransform TranslateX="150" ScaleX="0.8" ScaleY="0.8" />
                </Grid.RenderTransform>
                <Border Background="{{ThemeResource CardBackgroundFillColorDefaultBrush}}" 
                        BorderBrush="{{ThemeResource CardStrokeColorDefaultBrush}}"
                        BorderThickness="1" CornerRadius="8" Padding="32" Margin="0,32,0,48">
                    <ScrollViewer VerticalScrollBarVisibility="Hidden">
                        <StackPanel Spacing="24">
                            <Grid>
                                <Grid.ColumnDefinitions>
                                    <ColumnDefinition Width="Auto" />
                                    <ColumnDefinition Width="*" />
                                </Grid.ColumnDefinitions>
                                <FontIcon Glyph="&#xE946;" FontSize="20" Foreground="{{ThemeResource SystemAccentColor}}" VerticalAlignment="Center" Margin="0,0,12,0" />
                                <TextBlock x:Name="AboutTitleText" Text="Hakkında" Grid.Column="1" VerticalAlignment="Center" FontWeight="SemiBold" FontSize="18" />
                            </Grid>
                            
                            {page2_content}
                        </StackPanel>
                    </ScrollViewer>
                </Border>
            </Grid>

            <!-- PAGE 1: Settings (Starts active) -->
            <Grid x:Name="Page1" Width="480" VerticalAlignment="Center" HorizontalAlignment="Center" 
                  Opacity="1.0" Canvas.ZIndex="1" RenderTransformOrigin="0.5,0.5" IsHitTestVisible="True">
                <Grid.RenderTransform>
                    <CompositeTransform TranslateX="0" ScaleX="1.0" ScaleY="1.0" />
                </Grid.RenderTransform>
                <ScrollViewer VerticalScrollBarVisibility="Hidden">
                    <StackPanel Spacing="24" Padding="0,32,0,48">
                        {page1_content}
                    </StackPanel>
                </ScrollViewer>
            </Grid>
            
        </Grid>
        
        <PipsPager x:Name="CarouselPager" NumberOfPages="2" SelectedPageIndex="0" 
                   HorizontalAlignment="Center" VerticalAlignment="Bottom" Margin="0,0,0,24"
                   SelectedIndexChanged="CarouselPager_SelectedIndexChanged" />
    </Grid>
</Page>"""

# Replace from <Grid x:Name="RootGrid"> to end of file
new_full_content = re.sub(r'<Grid x:Name="RootGrid">.*</Page>', new_content, content, flags=re.DOTALL)

with open("Nythera/MainPage.xaml", "w", encoding="utf-8") as f:
    f.write(new_full_content)
