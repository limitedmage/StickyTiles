using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

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

        private ApplicationBar MainAppbar;
        private ApplicationBar OverlayAppbar;

        #endregion

        // Constructor
        public MainPage() {
            InitializeComponent();

            DataContext = this;

            FrontColor = (Color) App.Current.Resources["PhoneAccentColor"];
            BackColor = (Color)App.Current.Resources["PhoneAccentColor"];

            CreateAppbars();
            ApplicationBar = MainAppbar;
        }

        private void CreateAppbars() {
            MainAppbar = new ApplicationBar();

            ApplicationBarIconButton pin = new ApplicationBarIconButton(new Uri("/icons/appbar.pin.rest.png", UriKind.Relative));
            pin.Text = "pin";
            pin.Click += PinButton_Click;
            MainAppbar.Buttons.Add(pin);

            ApplicationBarMenuItem about = new ApplicationBarMenuItem("about");
            about.Click += About_Click;
            MainAppbar.MenuItems.Add(about);

            OverlayAppbar = new ApplicationBar();
            ApplicationBarIconButton ok = new ApplicationBarIconButton(new Uri("/icons/appbar.check.rest.png", UriKind.Relative));
            ok.Text = "ok";
            ok.Click += ClosePickers;
            OverlayAppbar.Buttons.Add(ok);
        }

        private void PinButton_Click(object sender, EventArgs e) {
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

        private void ShowFrontColorPicker(object sender, RoutedEventArgs e) {
            FrontColorPickerOverlay.Show();
            ApplicationBar = OverlayAppbar;
        }

        private void ShowBackColorPicker(object sender, RoutedEventArgs e) {
            BackColorPickerOverlay.Show();
            ApplicationBar = OverlayAppbar;
        }

        private void ClosePickers(object sender, EventArgs e) {
            FrontColorPickerOverlay.Hide();
            BackColorPickerOverlay.Hide();
            ApplicationBar = MainAppbar;
        }

        private void EnableBack_Checked(object sender, RoutedEventArgs e) {
            bool enable = EnableBack.IsChecked.GetValueOrDefault();
            BackTextBox.IsEnabled = enable;
            BackColorButton.IsEnabled = enable;
        }

        protected override void OnBackKeyPress(CancelEventArgs e) {
            if (FrontColorPickerOverlay.Visibility == System.Windows.Visibility.Visible) {
                FrontColorPickerOverlay.Hide();
                ApplicationBar = MainAppbar;
                e.Cancel = true;
            }
            
            if (BackColorPickerOverlay.Visibility == System.Windows.Visibility.Visible) {
                BackColorPickerOverlay.Hide();
                ApplicationBar = MainAppbar;
                e.Cancel = true;
            }

            base.OnBackKeyPress(e);
        }

        private void About_Click(object sender, EventArgs e) {
            MessageBox.Show("Created by Juliana Peña\nhttp://julianapena.com", "StickyTiles 1.0", MessageBoxButton.OK);
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