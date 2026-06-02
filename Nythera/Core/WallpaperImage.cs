using System;

namespace Nythera.Core
{
    public class WallpaperImage : System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }

        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        private string _name = string.Empty;
        public string Name 
        { 
            get => _name; 
            set { _name = value; OnPropertyChanged(); }
        }

        private string _imagePath = string.Empty;
        public string ImagePath 
        { 
            get => _imagePath; 
            set { _imagePath = value; OnPropertyChanged(); }
        }

        private Microsoft.UI.Xaml.Media.ImageSource _thumbnail;
        public Microsoft.UI.Xaml.Media.ImageSource Thumbnail 
        { 
            get => _thumbnail; 
            set { _thumbnail = value; OnPropertyChanged(); }
        }

        private bool _isFavorite = false;
        public bool IsFavorite 
        { 
            get => _isFavorite; 
            set 
            { 
                _isFavorite = value; 
                OnPropertyChanged();
                OnPropertyChanged(nameof(FavoriteIcon));
            }
        }

        public string FavoriteIcon => IsFavorite ? "\uEB52" : "\uEB51";

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SelectionIndicatorVisibility));
                }
            }
        }

        public Microsoft.UI.Xaml.Visibility SelectionIndicatorVisibility => IsSelected ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;

        private Microsoft.UI.Xaml.Visibility _playlistSelectionVisibility = Microsoft.UI.Xaml.Visibility.Collapsed;
        public Microsoft.UI.Xaml.Visibility PlaylistSelectionVisibility
        {
            get => _playlistSelectionVisibility;
            set
            {
                if (_playlistSelectionVisibility != value)
                {
                    _playlistSelectionVisibility = value;
                    OnPropertyChanged();
                }
            }
        }

        public Microsoft.UI.Xaml.Visibility DeleteButtonVisibility => IsCustom ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;

        private bool _isApplied;
        public bool IsApplied
        {
            get => _isApplied;
            set
            {
                if (_isApplied != value)
                {
                    _isApplied = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _appliedMonitorsText = "";
        public string AppliedMonitorsText
        {
            get => _appliedMonitorsText;
            set
            {
                if (_appliedMonitorsText != value)
                {
                    _appliedMonitorsText = value;
                    OnPropertyChanged();
                }
            }
        }
        
        private bool _isCustom = false;
        public bool IsCustom 
        { 
            get => _isCustom; 
            set { _isCustom = value; OnPropertyChanged(); }
        }

        private bool _isMarketplace = false;
        public bool IsMarketplace 
        { 
            get => _isMarketplace; 
            set { _isMarketplace = value; OnPropertyChanged(); }
        }

        private string _category = "All";
        public string Category 
        { 
            get => _category; 
            set { _category = value; OnPropertyChanged(); }
        }
        public DateTime AddedDate { get; set; } = DateTime.Now;

        // Image Settings
        public double Blur { get; set; } = 0;
        public double Brightness { get; set; } = 100;
        public double Contrast { get; set; } = 100;

        // Effects
        public bool EnableKenBurns { get; set; } = false;
        public bool EnableParallax { get; set; } = false;
        public bool EnableSnow { get; set; } = false;
        public bool EnableRain { get; set; } = false;

        // Layout
        // Fill, Fit, Stretch, Center, Span
        public string LayoutMode { get; set; } = "Fill";
        
        // Playlist selection
        private bool _isSelectedForPlaylist = false;
        public bool IsSelectedForPlaylist
        {
            get => _isSelectedForPlaylist;
            set
            {
                if (_isSelectedForPlaylist != value)
                {
                    _isSelectedForPlaylist = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(PlaylistCheckVisibility));
                }
            }
        }
        
        public Microsoft.UI.Xaml.Visibility PlaylistCheckVisibility => IsSelectedForPlaylist ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
    }
}
