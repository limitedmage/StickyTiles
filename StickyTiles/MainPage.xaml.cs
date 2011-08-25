using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using System.Windows.Media.Imaging;
using System.IO.IsolatedStorage;
using Microsoft.Phone.Shell;
using System.ComponentModel;

namespace StickyTiles {
    public partial class MainPage : PhoneApplicationPage, INotifyPropertyChanged {
        
        #region Properties

        private string _frontText;
        public string FrontText {
            get {
                return _frontText;
            }
            set {
                _frontText = value;
                Changed("FrontText");
            }
        }

        private string _backText;
        public string BackText {
            get {
                return _backText;
            }
            set {
                _backText = value;
                Changed("BackText");
            }
        }

        private Color _frontColor;
        public Color FrontColor {
            get {
                return _frontColor;
            }
            set {
                _frontColor = value;
                Changed("FrontColor");
            }
        }

        private Color _backColor;
        public Color BackColor {
            get {
                return _backColor;
            }
            set {
                _backColor = value;
                Changed("BackColor");
            }
        }

        #endregion

        // Constructor
        public MainPage() {
            InitializeComponent();

            DataContext = this;

            FrontColor = (Color) App.Current.Resources["PhoneAccentColor"];
            BackColor = (Color)App.Current.Resources["PhoneAccentColor"];
        }

        private void PinButton_Click(object sender, RoutedEventArgs e) {
            long id = DateTime.Now.Ticks;

            WriteableBitmap front = new WriteableBitmap(TilePreview, null);
            string frontFilename = "Shared/ShellContent/" + id + "-front.jpg";

            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication()) {
                using (IsolatedStorageFileStream fs = isf.CreateFile(frontFilename)) {
                    front.SaveJpeg(fs, front.PixelWidth, front.PixelHeight, 0, 100);
                }
            }

            StandardTileData tile = new StandardTileData {
                BackgroundImage = new Uri("isostore:/" + frontFilename)
            };

            if (EnableBack.IsChecked.GetValueOrDefault()) {
                WriteableBitmap back = new WriteableBitmap(BackTilePreview, null);
                string backFilename = "Shared/ShellContent/" + id + "-back.jpg";

                using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication()) {
                    using (IsolatedStorageFileStream fs = isf.CreateFile(backFilename)) {
                        back.SaveJpeg(fs, back.PixelWidth, back.PixelHeight, 0, 100);
                    }
                }

                tile.BackBackgroundImage = new Uri("isostore:/" + backFilename);
            }

            ShellTile.Create(new Uri("/MainPage.xaml?tile=" + id, UriKind.Relative), tile);
        }

        private void ToggleFrontColorPicker(object sender, RoutedEventArgs e) {
            if (FrontColorPickerOverlay.Visibility == System.Windows.Visibility.Visible) {
                FrontColorPickerOverlay.Hide();
            } else {
                FrontColorPickerOverlay.Show();
            }
        }

        private void ToggleBackColorPicker(object sender, RoutedEventArgs e) {
            if (BackColorPickerOverlay.Visibility == System.Windows.Visibility.Visible) {
                BackColorPickerOverlay.Hide();
            } else {
                BackColorPickerOverlay.Show();
            }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        public void Changed(string property) {
            if (PropertyChanged != null) {
                PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
        }

        #endregion
    }
}