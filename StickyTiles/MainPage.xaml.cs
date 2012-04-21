using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using Microsoft.Xna.Framework.GamerServices;

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
            if (e.NavigationMode == System.Windows.Navigation.NavigationMode.New) {
                CreateDonationPopup();
            }

            Sticky sticky;
            if (NavigationContext.QueryString.TryGetValue("tile", out id) && settings.TryGetValue(id, out sticky)) {
                // Launching pinned sticky
                Sticky = sticky;
                // Change pin button to save button
                var button = (MainAppbar.Buttons[0] as ApplicationBarIconButton);
                button.IconUri = new Uri("/icons/appbar.save.rest.png", UriKind.Relative);
                button.Text = "save";

                FlurryWP7SDK.Api.LogEvent("Editing tile");
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
                BackgroundImage = new Uri("isostore:/" + frontFilename),
                BackBackgroundImage = new Uri("IDontExist", UriKind.Relative), // UGLY HACK
                BackContent = string.Empty,
                BackTitle = string.Empty
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
                FlurryWP7SDK.Api.LogEvent("Updated existing tile");

                shelltile.Update(tile);
                Focus();
                MessageBox.Show("Your updated tile should be in your start screen.", "Tile updated", MessageBoxButton.OK);
            } else {
                FlurryWP7SDK.Api.LogEvent("Pinned new tile");

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
                        FlurryWP7SDK.Api.LogEvent("Changing front color");

                        Dispatcher.BeginInvoke(() => {
                            Sticky.FrontPicBytes = null;
                            FrontColorPickerOverlay.Show();
                            ApplicationBar = OverlayAppbar;
                        });
                    } else if (returned == 1) {
                        FlurryWP7SDK.Api.LogEvent("Changing front image");

                        ShowPicPicker(bytes => Sticky.FrontPicBytes = bytes);
                    }
                }, 
                null
            );
        }

        private void ShowFrontTextColorPicker(object sender, RoutedEventArgs e) {
            FlurryWP7SDK.Api.LogEvent("Changing front text color");

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
                        FlurryWP7SDK.Api.LogEvent("Changing back color");

                        Dispatcher.BeginInvoke(() => {
                            Sticky.BackPicBytes = null;
                            BackColorPickerOverlay.Show();
                            ApplicationBar = OverlayAppbar;
                        });
                    } else if (returned == 1) {
                        FlurryWP7SDK.Api.LogEvent("Changing back image");

                        ShowPicPicker(bytes => Sticky.BackPicBytes = bytes);
                    }
                },
                null
            );
        }

        private void ShowBackTextColorPicker(object sender, RoutedEventArgs e) {
            FlurryWP7SDK.Api.LogEvent("Changed back text color");

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
            FlurryWP7SDK.Api.LogEvent("Opened about page");

            NavigationService.Navigate(new Uri("/AboutPage.xaml", UriKind.Relative));
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
                var regex = new Regex(@"^\d+$");

                for (int i = 0; i < settings.Count; i++) {
                    var id = settings.ElementAt(i).Key;
                    
                    if (regex.IsMatch(id) && GetTile(id) == null) {
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

        #region Donation reminder

        private void CreateDonationPopup() {
            var launchedTimesSetting = "LaunchedTimes";
            var launchedTimesLimit = 10;

            if (settings.Contains(launchedTimesSetting)) {
                int times = (int)settings[launchedTimesSetting];
                if (times == launchedTimesLimit) {
                    // Show popup
                    FlurryWP7SDK.Api.LogEvent("Showing donation popup");

                    Guide.BeginShowMessageBox(
                        "Support StickyTiles",
                        "StickyTiles is a completely free, ad-free and open source project.\n\n" + 
                            "If you like the app and want to support it, how about " +
                            "buying me a cup of coffee to help me stay up at night coding?\n\n" +
                            "Either way, I won't bug you again :)",
                        new List<string> { "sure!", "no, thanks" },
                        0,
                        MessageBoxIcon.Alert,
                        r => {
                            var returned = Guide.EndShowMessageBox(r);
                            if (returned == 0) {
                                FlurryWP7SDK.Api.LogEvent("Donate clicked");
                                var wb = new WebBrowserTask();
                                wb.Uri = new Uri("http://julianapena.com/donate.html?ref=StickyTiles", UriKind.Absolute);
                                wb.Show();
                            } else {
                                FlurryWP7SDK.Api.LogEvent("Donate dismissed");
                            }
                            settings[launchedTimesSetting] = times + 1;
                        },
                        null
                    );
                } else if (times < launchedTimesLimit) {
                    settings[launchedTimesSetting] = times + 1;
                }
            } else {
                settings[launchedTimesSetting] = 1;
            }
        }

        #endregion
    }
}