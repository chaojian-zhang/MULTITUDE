using MULTITUDE.Class.Facility.ClueManagement;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace MULTITUDE.Class.DocumentTypes
{
    // Not serialized for playlists are stored in text form
    [Serializable]
    enum PlayMode
    {
        Random,
        Order,
        OrderRepeat,
        Single,
        SingleRepeat
    }

    /// <summary>
    /// Represents a list of sounds, images, and videos to play (only these three types)
    /// A list item can be a general clue in which case player will automatically use the documents under that clue; This will enable contents be automatically recognized as soon as it's clued, no need to edit playlist specially - this is called environment integration
    /// A list cannot contain another list
    /// </summary>
    [Serializable]
    class Playlist : Document, ISerializable, INotifyPropertyChanged
    {
        public Playlist(string path, string metaname)
            :this(path, metaname, System.DateTime.Now.ToString("MMMM dd, yyyy HHmmss")){}

        // A wrapper for underlying base
        public Playlist(string path, string metaname, string date)
            :base(DocumentType.PlayList, path, metaname, date != null? date : MULTITUDE.Class.Facility.SystemHelper.CurrentTimeFileNameFriendly)
        {
            _ImageDisplayDuration = 30;
            // _bSkipOrPauseVideoWhenNotWatching = true;
            _PlayMode = PlayMode.Order;
            _bJustPlay = false;
            _MediaClueStrings = new List<string>();
            _CurrentMediaID = -1;
            _CurrentMediaProgress = 0;
        }

        // Construct from existing image, sound or video documents
        internal Playlist(Document refDoc)
            : base(DocumentType.PlayList, null, null, date : MULTITUDE.Class.Facility.SystemHelper.CurrentTimeFileNameFriendly)
        {
            if (refDoc.Type != DocumentType.Sound && refDoc.Type != DocumentType.ImagePlus && refDoc.Type != DocumentType.Video) throw new InvalidOperationException("Invalid media document type.");

            // Defaults
            _ImageDisplayDuration = 30;
            _PlayMode = PlayMode.Order;
            _bJustPlay = false;
            _MediaClueStrings = new List<string>();
            _CurrentMediaID = -1;
            _CurrentMediaProgress = 0;

            // Setup using ref document
            _MediaClueStrings.AddRange(refDoc.Clues.Select(clue => clue.Name));
            NotifyPropertyChanged("MediaClueStrings");
        }

        #region Data
        // Data (Still public for serialization but sohuld be private for encapsulation)
        public double _ImageDisplayDuration { get; set; } // In seconds
        // public bool _bSkipOrPauseVideoWhenNotWatching { get; set; }   // Automatically skip (if not playing yet and upcoming next) or pause (if currently viewing) videos when the document is covered by some other cavans area or the application loses focus // This property is not used because PlayLists when used on VW never displays videos anyway and when used in MediaPlayer always plays video
        public PlayMode _PlayMode { get; set; }
        public bool _bJustPlay { get; set; } // Search all clues which have playable contents and play them in random order with a style (1. One clue by one clue instead of compeltely random; 2. Don't repeat 3. Musics that is too short i.e. shorter than 1min are not played); I.e. ignore below
        public List<string> _MediaClueStrings { get; set; }    // Clue strings
        // Progress Data
        public int _CurrentMediaID { get; set; }  // GUID of currently playing media
        public double _CurrentMediaProgress { get; set; }   // In seconds
        // CurrentPlayProgress is also recorded regarless of media type (i.e. for both video and music - might be a long novel; The same functionality exist in out media player though that's for all played documents)
        public static readonly string FileSuffix = ".MULTITUDEpl";
        public static readonly string SoundExtensions = ".mp3.flac.ogg.wav.aac.ape";
        public static readonly string VideoExtensions = ".avi.wmv.rmvb.mkv.mp4";
        public static readonly string ImageExtensions = ImagePlus.Extensions;
        #endregion

        #region Clue Data Management Halpers
        
        #endregion

        #region Sessional State
        private bool bMediaLoaded = false;
        private Dictionary<string, List<Document>> PlayDocuments = new Dictionary<string, List<Document>>();    // Not serialized, used during playing; Refreshed each time
        private Dictionary<string, List<Document>>.Enumerator CurrentPlayClue;
        private List<Document>.Enumerator CurrentPlayListMedia;
        private Random rnd = new Random();
        private List<Document> AlreadyPlayedDocuments = new List<Document>();
        private int revisionIndex = -1; // -1 not in revision mode; Otherwise points into PlayedDocuments;
        private static readonly string JustPlayModeCategory = "JustPlayMode";
        #endregion

        #region Interaction Interface
        private void LoadMedia()
        {
            // Load only during first usage so things are settled
            if (bMediaLoaded) return;

            // Clean up
            PlayDocuments.Clear();

            // Depending on setting
            List<DocumentType> searchTypes = new List<DocumentType>() { DocumentType.ImagePlus, DocumentType.Sound, DocumentType.Video };
            if (_bJustPlay)
            {
                PlayDocuments[JustPlayModeCategory] = ClueManager.Manager.GetDocumentsFilterByType(searchTypes);
                CurrentPlayClue = PlayDocuments.GetEnumerator();
                CurrentPlayClue.MoveNext();
                CurrentPlayListMedia = CurrentPlayClue.Current.Value.GetEnumerator();
                CurrentPlayListMedia.MoveNext();
            }
            else
            {
                foreach (string mediaClue in _MediaClueStrings)
                {
                    PlayDocuments[mediaClue] = ClueManager.Manager.GetDocumentsFilterByType(new Clue(mediaClue), searchTypes);
                }

                CurrentPlayClue = PlayDocuments.GetEnumerator();    // Immediately after we get enumerator it's not beginned yet
                if (CurrentPlayClue.MoveNext())
                {
                    if (_CurrentMediaProgress != 0)
                    {
                        Document media = Home.Current.GetDocument(_CurrentMediaID);
                        IEnumerable<KeyValuePair<string, List<Document>>> results = PlayDocuments.Where(item => item.Value.Contains(media));
                        if (results.Count() != 0)
                        {
                            KeyValuePair<string, List<Document>> relatedList = results.First();
                            while (CurrentPlayClue.Current.Key != relatedList.Key) CurrentPlayClue.MoveNext();
                            CurrentPlayListMedia = CurrentPlayClue.Current.Value.GetEnumerator();
                            while (CurrentPlayListMedia.Current != media) CurrentPlayListMedia.MoveNext();
                        }
                        else CurrentPlayListMedia = CurrentPlayClue.Current.Value.GetEnumerator();
                    }
                    else
                    {
                        CurrentPlayListMedia = CurrentPlayClue.Current.Value.GetEnumerator();
                    }
                    CurrentPlayListMedia.MoveNext();
                }
            }

            bMediaLoaded = true;
        }
        // Get the media that we were playing
        public Document GetCurrentMedia()
        {
            if (bMediaLoaded == false) LoadMedia();
            return CurrentPlayListMedia.Current;
        }
        // Get the media one slot ahead of current one
        public Document MoveToPreviousMedia()
        {
            if (bMediaLoaded == false) LoadMedia();

            if (revisionIndex == -1 && AlreadyPlayedDocuments.Count != 0)
            {
                revisionIndex = AlreadyPlayedDocuments.Count - 1;
                // Update data
                CurrentMediaID = AlreadyPlayedDocuments[revisionIndex].GUID;
                return AlreadyPlayedDocuments[revisionIndex];
            }
            else if (revisionIndex > 0)
            {
                revisionIndex--;
                // Update data
                CurrentMediaID = AlreadyPlayedDocuments[revisionIndex].GUID;
                return AlreadyPlayedDocuments[revisionIndex];
            }
            else { revisionIndex = -1;  return null; }
        }
        public Document MoveToNextMedia()
        {
            if (bMediaLoaded == false) LoadMedia();

            // Depending on which list we are currently viewing
            // >> Revision Mode
            if (revisionIndex != -1)
            {
                revisionIndex++;
                if (revisionIndex != AlreadyPlayedDocuments.Count) { CurrentMediaID = AlreadyPlayedDocuments[revisionIndex].GUID; return AlreadyPlayedDocuments[revisionIndex]; }
                else revisionIndex = -1;
            }

            // Make a record
            if (CurrentPlayListMedia.Current != null) AlreadyPlayedDocuments.Add(CurrentPlayListMedia.Current);

            // >> Intelligent Just Play Mode
            if(_bJustPlay)
            {
                int nextSong = rnd.Next(0, PlayDocuments[JustPlayModeCategory].Count);
                CurrentMediaID = -1;
                return PlayDocuments[JustPlayModeCategory][nextSong];
            }
            // >> Normal Mode
            else
            {
                switch (_PlayMode)
                {
                    case PlayMode.Random:
                        // <Developmeng> Not handling not repeating the same song in one session: we just need to keeep a "AlreadyPlayed" sessional state and don't repeat ones in that list
                        int nextCategory = rnd.Next(0, PlayDocuments.Keys.Count);
                        string categoryString = PlayDocuments.Keys.ElementAt(nextCategory);
                        int nextSong = rnd.Next(0, PlayDocuments[categoryString].Count);
                        return PlayDocuments[categoryString][nextSong];
                    case PlayMode.Order:
                        if (CurrentPlayListMedia.MoveNext() != false) break;
                        else if (CurrentPlayClue.MoveNext() != false) { CurrentPlayListMedia = CurrentPlayClue.Current.Value.GetEnumerator(); CurrentPlayListMedia.MoveNext(); break; }
                        else { CurrentPlayClue = PlayDocuments.GetEnumerator(); CurrentPlayClue.MoveNext(); CurrentPlayListMedia = CurrentPlayClue.Current.Value.GetEnumerator(); CurrentPlayListMedia.MoveNext(); return null; }
                    case PlayMode.OrderRepeat:
                        if (CurrentPlayListMedia.MoveNext() != false) break;
                        else if (CurrentPlayClue.MoveNext() != false) { CurrentPlayListMedia = CurrentPlayClue.Current.Value.GetEnumerator(); CurrentPlayListMedia.MoveNext(); break; }
                        else { CurrentPlayClue = PlayDocuments.GetEnumerator(); CurrentPlayClue.MoveNext(); CurrentPlayListMedia = CurrentPlayClue.Current.Value.GetEnumerator(); CurrentPlayListMedia.MoveNext(); break; }
                    case PlayMode.Single:
                        return null;
                    case PlayMode.SingleRepeat:
                        break;
                    default:
                        return null;
                }
            }

            // Update data
            CurrentMediaID = CurrentPlayListMedia.Current.GUID;
            // CurrentMediaProgress = ... // Updated by player

            return CurrentPlayListMedia.Current;
        }
        // Alias for CurrentMediaProgress property
        public double GetRecommededProgress()
        {
            return _CurrentMediaProgress;
        }
        public List<Document> GetAllMediasList()
        {
            if (bMediaLoaded == false) LoadMedia();
            List<Document> allMedias = new List<Document>();
            foreach (KeyValuePair<string, List<Document>> category in PlayDocuments)
            {
                allMedias.AddRange(category.Value);
            }
            return allMedias;
        }
        #endregion

        #region Serialization
        public Playlist(SerializationInfo info, StreamingContext ctxt)
            :base(info, ctxt)
        {
            // Assign some default
            _ImageDisplayDuration = 30;
            // _bSkipOrPauseVideoWhenNotWatching = true;
            _PlayMode = PlayMode.Order;
            _MediaClueStrings = new List<string>();
            _CurrentMediaID = -1;
            _CurrentMediaProgress = 0;

            // Load Properties
            _MediaClueStrings = (List<string>)info.GetValue("Medias", typeof(List<string>));
            _CurrentMediaID = (int)info.GetValue("CurrentMedia", typeof(int));
            _CurrentMediaProgress = (double)info.GetValue("CurrentMediaProgress", typeof(double));

            LoadDocument();
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            // Save Properties
            info.AddValue("Medias", _MediaClueStrings);
            info.AddValue("CurrentMedia", _CurrentMediaID);
            info.AddValue("CurrentMediaProgress", _CurrentMediaProgress);

            // Save Properties to a file
            SaveDocument();
        }

        protected override void SaveDocument()
        {
            if (bDirty)
            {
                string filePath = Path;
                // Save data into a text file
                using (StreamWriter outputFile = new StreamWriter(filePath))
                {
                    // Write basic configuration
                    outputFile.WriteLine("ImageDisplayDuration: " + _ImageDisplayDuration);
                    // outputFile.WriteLine("bSkipOrPauseVideoWhenNotWatching: " + _bSkipOrPauseVideoWhenNotWatching);
                    outputFile.WriteLine("PlayMode: " + _PlayMode);
                    outputFile.WriteLine("JustPlay: " + _bJustPlay);
                    // Write medias' user friendly names (for external vieweing only because we don't even load such
                    outputFile.WriteLine("Medias List (Reference Only): ");
                    foreach (KeyValuePair<string, List<Document>> category in PlayDocuments)
                    {
                        outputFile.WriteLine("[Category: " + category.Key + "]");
                        foreach (Document doc in category.Value)
                        {
                            outputFile.WriteLine(doc.ShortDescription);
                        }
                    }
                    // Write out media's document names
                    // ...
                }
                bDirty = false;
            }
        }

        protected override void LoadDocument()
        {
            // Open file content
            string filePath = Path;
            string[] lines = System.IO.File.ReadAllLines(filePath);

            // Pasrse line content
            foreach (string line in lines)
            {
                // Skip empty
                if (string.IsNullOrEmpty(line)) continue;
                int seperator = line.IndexOf(": ");
                // Load basic configuration
                if (seperator != -1)
                {
                    string token = line.Substring(0, seperator);
                    string option = line.Substring(seperator + 2);
                    switch (token)
                    {
                        case "ImageDisplayDuration": _ImageDisplayDuration = float.Parse(option); break;
                        // case "bSkipOrPauseVideoWhenNotWatching": _bSkipOrPauseVideoWhenNotWatching = bool.Parse(option); break;
                        case "PlayMode": try { _PlayMode = (PlayMode)Enum.Parse(typeof(PlayMode), option); } catch (ArgumentException) { _PlayMode = PlayMode.Order; } break;
                        case "JustPlay": _bJustPlay = bool.Parse(option); break; 
                    }
                }
                // Load media
                // else Medias.Add(line);
            }
        }

        public static Playlist Import(System.IO.FileInfo file)
        {
            if (file.Extension == FileSuffix)
            {
                // Need to read actual content in the document
                throw new NotImplementedException();
            }
            else if (MULTITUDE.Class.Facility.StringHelper.ExtensionContains(SoundExtensions, file.Extension) ||
                MULTITUDE.Class.Facility.StringHelper.ExtensionContains(VideoExtensions, file.Extension))
            {
                Playlist newPlaylist = new Playlist(file.FullName, System.IO.Path.GetFileNameWithoutExtension(file.Name));
                return newPlaylist;
            }
            else
            {
                throw new InvalidOperationException("Invalid data collection file type - unrecognizable file type for web.");
            }
        }

        public override void Export()
        {
            throw new NotImplementedException();
        }
        #endregion Serialization

        #region Data Binding Usage
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }

            // Also change dirty state
            bDirty = true;
        }

        public double ImageDisplayDuration
        {
            get { return this._ImageDisplayDuration; }
            set
            {
                if (value != this._ImageDisplayDuration)
                {
                    this._ImageDisplayDuration = value;
                    NotifyPropertyChanged();
                }
            }
        }
        //public bool bSkipOrPauseVideoWhenNotWatching
        //{
        //    get { return this._bSkipOrPauseVideoWhenNotWatching; }
        //    set
        //    {
        //        if (value != this._bSkipOrPauseVideoWhenNotWatching)
        //        {
        //            this._bSkipOrPauseVideoWhenNotWatching = value;
        //            NotifyPropertyChanged();
        //        }
        //    }
        //}
        public PlayMode PlayMode
        {
            get { return this._PlayMode; }
            set
            {
                if (value != this._PlayMode)
                {
                    this._PlayMode = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public bool bJustPlay
        {
            get { return this._bJustPlay; }
            set
            {
                if (value != this._bJustPlay)
                {
                    this._bJustPlay = value;
                    bMediaLoaded = false;
                    NotifyPropertyChanged();
                }
            }
        }
        public string ImageDisplayDurationText
        {
            get { return this._ImageDisplayDuration.ToString() + "s"; }
            set
            {
                NotifyPropertyChanged();
                if (value.Length > 1 && value.IndexOf("s") == value.Length - 1)
                {
                    double seconds;
                    if (double.TryParse(value.Substring(0, value.Length - 1), out seconds))
                    {
                        _ImageDisplayDuration = seconds;
                    }
                }
                else if (value.Length > 1 && value.IndexOf("h") == value.Length - 1)
                {
                    double hours;
                    if (double.TryParse(value.Substring(0, value.Length - 1), out hours))
                    {
                        _ImageDisplayDuration = hours * 3600;
                    }
                }
                else if (value.Length > 3 && value.IndexOf("min") == value.Length - 3)
                {
                    double minutes;
                    if (double.TryParse(value.Substring(0, value.Length - 3), out minutes))
                    {
                        _ImageDisplayDuration = minutes * 60;
                    }
                }
                else
                {
                    double seconds;
                    if (double.TryParse(value, out seconds))
                    {
                        _ImageDisplayDuration = seconds;
                    }
                    else _ImageDisplayDuration = 30;
                }
            }
        }
        public ObservableCollection<string> MediaClueStrings
        {
            get { return new ObservableCollection<string>(this._MediaClueStrings); }
            set
            {
                // if (value != this._MediaClueStrings)
                {
                    this._MediaClueStrings = new List<string>(value);
                    NotifyPropertyChanged();
                }
            }
        }
        public void AddMediaClueString(string newClueString)
        {
            // Check existence and Add
            if (_MediaClueStrings.Contains(newClueString) == false) _MediaClueStrings.Add(newClueString);
            // Reset
            bMediaLoaded = false;
            // Notify
            NotifyPropertyChanged("MediaClueStrings");
        }
        public void RemoveEmptyMediaClueString()
        {
            // Clear empty
            _MediaClueStrings = _MediaClueStrings.Where(item => string.IsNullOrWhiteSpace(item) == false).ToList();
            // Reset
            bMediaLoaded = false;
            // Notify
            NotifyPropertyChanged("MediaClueStrings");
        }
        public void RemoveMediaClueString(string toRemove)
        {
            // Remove
            _MediaClueStrings.Remove(toRemove);
            // Reset
            bMediaLoaded = false;
            // Notify
            NotifyPropertyChanged("MediaClueStrings");
        }
        public void UpdateMediaClueString(string oldOne, string neweOne)
        {
            if(_MediaClueStrings.Contains(oldOne))
            {
                // Remove then add
                _MediaClueStrings.Remove(oldOne);
                _MediaClueStrings.Add(neweOne);
                // Reset
                bMediaLoaded = false;
                // Notify
                NotifyPropertyChanged("MediaClueStrings");
            }
        }
        public int CurrentMediaID
        {
            get { return this._CurrentMediaID; }
            set
            {
                if (value != this._CurrentMediaID)
                {
                    this._CurrentMediaID = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public double CurrentMediaProgress
        {
            get { return this._CurrentMediaProgress; }
            set
            {
                if (value != this._CurrentMediaProgress)
                {
                    this._CurrentMediaProgress = value;
                    NotifyPropertyChanged();
                }
            }
        }
        #endregion
    }
}
