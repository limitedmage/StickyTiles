using System.Windows.Media;
using System.Runtime.Serialization;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using System.IO;

namespace StickyTiles {
    [DataContract]
    public class Sticky : INotifyPropertyChanged {

        private string _frontText;
        [DataMember]
        public string FrontText {
            get {
                return _frontText;
            }
            set {
                _frontText = value;
                Changed("FrontText");
            }
        }

        private Color _frontColor;
        [DataMember]
        public Color FrontColor {
            get {
                return _frontColor;
            }
            set {
                _frontColor = value;
                Changed("FrontColor");
            }
        }

        private Color _frontTextColor;
        [DataMember]
        public Color FrontTextColor {
            get {
                return _frontTextColor;
            }
            set {
                _frontTextColor = value;
                Changed("FrontTextColor");
            }
        }

        private int _frontSize;
        [DataMember]
        public int FrontSize {
            get {
                return _frontSize;
            }
            set {
                _frontSize = value;
                Changed("FrontSize");
            }
        }

        public BitmapImage FrontPic {
            get {
                if (FrontPicBytes != null) {
                    var bm = new BitmapImage();
                    bm.SetSource(new MemoryStream(FrontPicBytes));
                    return bm;
                } else {
                    return null;
                }
            }
        }

        private byte[] _frontPicBytes;
        [DataMember]
        public byte[] FrontPicBytes {
            get {
                return _frontPicBytes;
            }
            set {
                _frontPicBytes = value;
                Changed("FrontPicBytes");
                Changed("FrontPic");
            }
        }

        private string _backText;
        [DataMember]
        public string BackText {
            get {
                return _backText;
            }
            set {
                _backText = value;
                Changed("BackText");
            }
        }

        private Color _backColor;
        [DataMember]
        public Color BackColor {
            get {
                return _backColor;
            }
            set {
                _backColor = value;
                Changed("BackColor");
            }
        }

        private Color _backTextColor;
        [DataMember]
        public Color BackTextColor {
            get {
                return _backTextColor;
            }
            set {
                _backTextColor = value;
                Changed("BackTextColor");
            }
        }

        private int _backSize;
        [DataMember]
        public int BackSize {
            get {
                return _backSize;
            }
            set {
                _backSize = value;
                Changed("BackSize");
            }
        }

        public BitmapImage BackPic {
            get {
                if (BackPicBytes != null) {
                    var bm = new BitmapImage();
                    bm.SetSource(new MemoryStream(BackPicBytes));
                    return bm;
                } else {
                    return null;
                }
            }
        }

        private byte[] _backPicBytes;
        [DataMember]
        public byte[] BackPicBytes {
            get {
                return _backPicBytes;
            }
            set {
                _backPicBytes = value;
                Changed("BackPicBytes");
                Changed("BackPic");
            }
        }

        private bool _hasBack;
        [DataMember]
        public bool HasBack {
            get {
                return _hasBack;
            }
            set {
                _hasBack = value;
                Changed("HasBack");
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
