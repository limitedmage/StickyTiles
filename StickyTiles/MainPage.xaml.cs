using System;
using System.ComponentModel;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using Microsoft.Xna.Framework.GamerServices;
using System.Collections.Generic;

namespace StickyTiles {
    public partial class MainPage : PhoneApplicationPage {
        
        #region Properties

        private IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
        private string id = null;
        
        public Sticky Sticky { get; set; }

        private ApplicationBar MainAppbar;
        private ApplicationBar OverlayAppbar;
        
        #endregion

        #region Creation and navigation events

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
            if (NavigationContext.QueryString.TryGetValue("tile", out id) && settings.TryGetValue(id, out sticky)) {
                // Launching pinned sticky
                Sticky = sticky;
                // Change pin button to save button
                var button = (MainAppbar.Buttons[0] as ApplicationBarIconButton);
                button.IconUri = new Uri("/icons/appbar.save.rest.png", UriKind.Relative);
                button.Text = "save";
            } else if (State.ContainsKey("sticky")) {
                // Restore from tombstone
                Sticky = State["sticky"] as Sticky;
             } else {
                // New sticky
                Sticky = new Sticky {
                    FrontColor = (Color)App.Current.Resources["PhoneAccentColor"],
                    BackColor = (Color)App.Current.Resources["PhoneAccentColor"],
                    FrontTextColor = Colors.White,
                    BackTextColor = Colors.White,
                    FrontSize = 20,
                    BackSize = 20
                };
            }

            DataContext = Sticky;

            DeleteOldTiles();
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e) {
            // Save sticky for tombstoning
            State["sticky"] = Sticky;
            
            base.OnNavigatedFrom(e);
        }

        #endregion

        #region Button handlers

        private void PinButton_Click(object sender, EventArgs e) {
            if (id == null) {
                id = DateTime.Now.Ticks.ToString();
            }

            // save sticky
            settings[id] = Sticky;

            WriteableBitmap front = new WriteableBitmap(TilePreview, null);
            string frontFilename = GetTileFilename(id);

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
                string backFilename = GetTileFilename(id, true);

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
                MessageBox.Show("Tile updated!");
            } else {
                ShellTile.Create(GetTileUri(id), tile);
            }
        }

        private void ShowFrontColorPicker(object sender, RoutedEventArgs e) {
            Guide.BeginShowMessageBox(
                "Front Tile Background", 
                "Use color or image as tile background?", 
                new List<string> { "Color", "Image" }, 
                0, 
                MessageBoxIcon.Alert, 
                r => {
                    var returned = Guide.EndShowMessageBox(r);
                    if (returned == 0) {
                        Dispatcher.BeginInvoke(() => {
                            Sticky.FrontPicBytes = null;
                            FrontColorPickerOverlay.Show();
                            ApplicationBar = OverlayAppbar;
                        });
                    } else if (returned == 1) {
                        ShowPicPicker(bytes => Sticky.FrontPicBytes = bytes);
                    }
                }, 
                null
            );
        }

        private void ShowFrontTextColorPicker(object sender, RoutedEventArgs e) {
            FrontTextColorPickerOverlay.Show();
            ApplicationBar = OverlayAppbar;
        }

        private void ShowBackColorPicker(object sender, RoutedEventArgs e) {
            Guide.BeginShowMessageBox(
                "Back Tile Background",
                "Use color or image as tile background?",
                new List<string> { "color", "image" },
                0,
                MessageBoxIcon.Alert,
                r => {
                    var returned = Guide.EndShowMessageBox(r);
                    if (returned == 0) {
                        Dispatcher.BeginInvoke(() => {
                            Sticky.BackPicBytes = null;
                            BackColorPickerOverlay.Show();
                            ApplicationBar = OverlayAppbar;
                        });
                    } else if (returned == 1) {
                        ShowPicPicker(bytes => Sticky.BackPicBytes = bytes);
                    }
                },
                null
            );
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

        #endregion

        #region Helpers

        private void ShowPicPicker(Action<byte[]> callback) {
            var t = new PhotoChooserTask();
            t.PixelHeight = 173;
            t.PixelWidth = 173;
            t.ShowCamera = true;

            t.Completed += (s, ev) => {
                if (ev.TaskResult == TaskResult.OK) {
                    if (ev.ChosenPhoto != null) {
                        Dispatcher.BeginInvoke(() => callback(GetJpegBytesFromStream(ev.ChosenPhoto)));
                    }
                }
            };

            t.Show();
        }

        private byte[] GetJpegBytesFromStream(Stream stream) {
            byte[] bytes;

            using (var ms = new MemoryStream()) {
                var bitmap = new BitmapImage();
                bitmap.SetSource(stream);
                var wb = new WriteableBitmap(bitmap);
                wb.SaveJpeg(ms, wb.PixelWidth, wb.PixelHeight, 0, 85);
                bytes = ms.ToArray();
            }

            return bytes;
        }

        Uri GetTileUri(string id) {
            return new Uri("/MainPage.xaml?tile=" + id, UriKind.Relative);
        }

        ShellTile GetTile(string id) {
            return ShellTile.ActiveTiles.FirstOrDefault(x => x.NavigationUri == GetTileUri(id));
        }

        string GetTileFilename(string id, bool back = false) {
            if (!back) {
                return "Shared/ShellContent/" + id + "-front.jpg";
            } else {
                return "Shared/ShellContent/" + id + "-back.jpg";
            }
        }

        private void DeleteOldTiles() {
            var worker = new BackgroundWorker();

            worker.DoWork += (s, e) => {
                for (int i = 0; i < settings.Count; i++) {
                    var id = settings.ElementAt(i).Key;
                    if (GetTile(id) == null) {
                        var frontfile = GetTileFilename(id);
                        var backfile = GetTileFilename(id);

                        using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication()) {
                            isf.DeleteFile(frontfile);
                            isf.DeleteFile(backfile);
                        }

                        settings.Remove(id);
                        i--;
                    }
                }
            };

            worker.RunWorkerAsync();
        }

        #endregion
    }
}