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
        // Constructor
        public MainPage() {
            InitializeComponent();
        }

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

        private void PinButton_Click(object sender, RoutedEventArgs e) {
            
            long id = DateTime.Now.Ticks;

            WriteableBitmap front = new WriteableBitmap(TilePreview, null);
            string frontFilename = "Shared/ShellContent/" + id + "-front.jpg";

            WriteableBitmap back = new WriteableBitmap(BackTilePreview, null);
            string backFilename = "Shared/ShellContent/" + id + "-back.jpg";

            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication()) {
                using (IsolatedStorageFileStream fs = isf.CreateFile(frontFilename)) {
                    front.SaveJpeg(fs, front.PixelWidth, front.PixelHeight, 0, 100);
                }
                
                using (IsolatedStorageFileStream fs = isf.CreateFile(backFilename)) {
                    back.SaveJpeg(fs, back.PixelWidth, back.PixelHeight, 0, 100);
                }
            }

            StandardTileData tile = new StandardTileData {
                BackgroundImage = new Uri("isostore:/" + frontFilename),
                BackBackgroundImage = new Uri("isostore:/" + backFilename)
            };

            ShellTile.Create(new Uri("/MainPage.xaml?tile=" + id, UriKind.Relative), tile);
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