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
            set { _isFavorite = value; OnPropertyChanged(); }
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
        public bool IsSelectedForPlaylist { get; set; } = false;
    }
}
