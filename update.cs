using System.IO;
using System.Text.RegularExpressions;

var content = File.ReadAllText("Nythera/MainPage.xaml");

var match1 = Regex.Match(content, @"(<!-- Header -->.*?)(?=<!-- About Expander -->)", RegexOptions.Singleline);
var page1 = match1.Groups[1].Value.Trim();

var match2 = Regex.Match(content, @"(<!-- App Info Section -->.*?</StackPanel>\s*</Grid>\s*</StackPanel>)", RegexOptions.Singleline);
var page2 = match2.Groups[1].Value.Trim();

var newContent = $@"    <Grid x=""Name=""RootGrid"">
        <Grid HorizontalAlignment=""Stretch"" VerticalAlignment=""Stretch"">
            
            <!-- PAGE 2: About (Starts in background) -->
            <Grid x:Name=""Page2"" Width=""480"" VerticalAlignment=""Center"" HorizontalAlignment=""Center"" 
                  Opacity=""0.3"" Canvas.ZIndex=""0"" RenderTransformOrigin=""0.5,0.5"" IsHitTestVisible=""False"">
                <Grid.RenderTransform>
                    <CompositeTransform TranslateX=""150"" ScaleX=""0.8"" ScaleY=""0.8"" />
                </Grid.RenderTransform>
                <Border Background=""{{ThemeResource CardBackgroundFillColorDefaultBrush}}"" 
                        BorderBrush=""{{ThemeResource CardStrokeColorDefaultBrush}}""
                        BorderThickness=""1"" CornerRadius=""8"" Padding=""32"" Margin=""0,32,0,48"">
                    <ScrollViewer VerticalScrollBarVisibility=""Hidden"">
                        <StackPanel Spacing=""24"">
                            <Grid>
                                <Grid.ColumnDefinitions>
                                    <ColumnDefinition Width=""Auto"" />
                                    <ColumnDefinition Width=""*"" />
                                </Grid.ColumnDefinitions>
                                <FontIcon Glyph=""&#xE946;"" FontSize=""20"" Foreground=""{{ThemeResource SystemAccentColor}}"" VerticalAlignment=""Center"" Margin=""0,0,12,0"" />
                                <TextBlock x:Name=""AboutTitleText"" Text=""Hakkında"" Grid.Column=""1"" VerticalAlignment=""Center"" FontWeight=""SemiBold"" FontSize=""18"" />
                            </Grid>
                            
                            {page2}
                        </StackPanel>
                    </ScrollViewer>
                </Border>
            </Grid>

            <!-- PAGE 1: Settings (Starts active) -->
            <Grid x:Name=""Page1"" Width=""480"" VerticalAlignment=""Center"" HorizontalAlignment=""Center"" 
                  Opacity=""1.0"" Canvas.ZIndex=""1"" RenderTransformOrigin=""0.5,0.5"" IsHitTestVisible=""True"">
                <Grid.RenderTransform>
                    <CompositeTransform TranslateX=""0"" ScaleX=""1.0"" ScaleY=""1.0"" />
                </Grid.RenderTransform>
                <ScrollViewer VerticalScrollBarVisibility=""Hidden"">
                    <StackPanel Spacing=""24"" Padding=""0,32,0,48"">
{page1}
                    </StackPanel>
                </ScrollViewer>
            </Grid>
            
        </Grid>
        
        <PipsPager x:Name=""CarouselPager"" NumberOfPages=""2"" SelectedPageIndex=""0"" 
                   HorizontalAlignment=""Center"" VerticalAlignment=""Bottom"" Margin=""0,0,0,24""
                   SelectedIndexChanged=""CarouselPager_SelectedIndexChanged"" />
    </Grid>
</Page>";

var fullNewContent = Regex.Replace(content, @"<Grid x:Name=""RootGrid"">.*</Page>", newContent, RegexOptions.Singleline);
fullNewContent = fullNewContent.Replace(@"x=""Name=""RootGrid""", @"x:Name=""RootGrid""");

File.WriteAllText("Nythera/MainPage.xaml", fullNewContent);
