using System;
using System.ComponentModel;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using System.IO;

namespace StickyTiles {
    public partial class MainPage : PhoneApplicationPage {
        
        #region Properties
        private IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
        private string id = null;

        public Sticky Sticky { get; set; }

        private ApplicationBar MainAppbar;
        private ApplicationBar OverlayAppbar;
        
        #endregion

        // Constructor
        public MainPage() {
            InitializeComponent();

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

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e) {
            Sticky sticky;
            if (NavigationContext.QueryString.TryGetValue("tile", out id)) {
                if (settings.TryGetValue(id, out sticky)) {
                    Sticky = sticky;
                    DataContext = Sticky;

                    (MainAppbar.Buttons[0] as ApplicationBarIconButton).IconUri = new Uri("/icons/appbar.save.rest.png", UriKind.Relative);

                    return;
                }

                MessageBox.Show(id);
            }

            Sticky = new Sticky {
                FrontColor = (Color)App.Current.Resources["PhoneAccentColor"],
                BackColor = (Color)App.Current.Resources["PhoneAccentColor"],
                FrontTextColor = Colors.White,
                BackTextColor = Colors.White,
                FrontSize = 20,
                BackSize = 20
            };
            DataContext = Sticky;
            
        }

        Uri GetTileUri(string id) {
            return new Uri("/MainPage.xaml?tile=" + id, UriKind.Relative);
        }

        ShellTile GetTile(string id) {
            return ShellTile.ActiveTiles.FirstOrDefault(x => x.NavigationUri == GetTileUri(id));
        }

        private void PinButton_Click(object sender, EventArgs e) {
            if (id == null) {
                id = DateTime.Now.Ticks.ToString();
            }

            // save sticky
            settings[id] = Sticky;

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

            var shelltile = GetTile(id);

            if (shelltile != null) {
                shelltile.Update(tile);
                Focus();
                MessageBox.Show("Tile saved!");
            } else {
                ShellTile.Create(GetTileUri(id), tile);
            }
        }

        private void ShowFrontColorPicker(object sender, RoutedEventArgs e) {
            FrontColorPickerOverlay.Show();
            ApplicationBar = OverlayAppbar;
        }

        private void ShowFrontTextColorPicker(object sender, RoutedEventArgs e) {
            FrontTextColorPickerOverlay.Show();
            ApplicationBar = OverlayAppbar;
        }

        private void ShowBackColorPicker(object sender, RoutedEventArgs e) {
            BackColorPickerOverlay.Show();
            ApplicationBar = OverlayAppbar;
        }

        private void ShowBackTextColorPicker(object sender, RoutedEventArgs e) {
            BackTextColorPickerOverlay.Show();
            ApplicationBar = OverlayAppbar;
        }

        private void ClosePickers(object sender, EventArgs e) {
            FrontColorPickerOverlay.Hide();
            FrontTextColorPickerOverlay.Hide();
            BackColorPickerOverlay.Hide();
            BackTextColorPickerOverlay.Hide();
            ApplicationBar = MainAppbar;
        }

        protected override void OnBackKeyPress(CancelEventArgs e) {
            if (FrontColorPickerOverlay.Visibility == System.Windows.Visibility.Visible ||
                FrontTextColorPickerOverlay.Visibility == System.Windows.Visibility.Visible ||
                BackColorPickerOverlay.Visibility == System.Windows.Visibility.Visible ||
                BackTextColorPickerOverlay.Visibility == System.Windows.Visibility.Visible) {

                ClosePickers(this, e);
                e.Cancel = true;
            }

            base.OnBackKeyPress(e);
        }

        private void About_Click(object sender, EventArgs e) {
            MessageBox.Show("Created by Juliana Peña\nhttp://julianapena.com", "StickyTiles 2.0", MessageBoxButton.OK);
        }

        private void ShowFrontPicPicker(object sender, RoutedEventArgs e) {
            var t = new PhotoChooserTask();
            t.PixelHeight = 173;
            t.PixelWidth = 173;
            t.ShowCamera = true;

            t.Completed += (s, ev) => {
                if (ev.TaskResult == TaskResult.OK) {
                    if (ev.ChosenPhoto != null) {
                        var stream = ev.ChosenPhoto;
                        stream.Position = 0;

                        var ms = new MemoryStream();
                        var buffer = new byte[stream.Length];
                        int read;
                        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0) {
                            ms.Write(buffer, 0, read);
                        }

                        Dispatcher.BeginInvoke(() => {
                            Sticky.FrontPicBytes = ms.ToArray();
                        });
                    }
                }
            };
            
            t.Show();
        }
    }
}