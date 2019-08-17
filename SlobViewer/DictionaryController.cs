using HtmlToFlowDocument.Dom;
using SlobViewer.Common;
using SlobViewer.Slob;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SlobViewer
{
    /// <summary>
    /// ViewModel for the <see cref="DictionaryControl"/>.
    /// </summary>
    /// <seealso cref="System.ComponentModel.INotifyPropertyChanged" />
    public class DictionaryController : System.ComponentModel.INotifyPropertyChanged
    {
        Settings _settings = new Settings();

        HtmlToFlowDocument.Converter _converter = new HtmlToFlowDocument.Converter();

        /// <summary>
        /// The currently loaded dictionaries.
        /// </summary>
        List<ISlobDictionary> _dictionaries = new List<ISlobDictionary>();

        SortKey[] _sortKeys;

        CompareInfo _compareInfo;

        Collections.Text.LongestCommonSubstringsOfStringAndStringArray _bestMatches = new Collections.Text.LongestCommonSubstringsOfStringAndStringArray().Preallocate(255, 255);
        CancellationTokenSource _bestMatchesCancellationTokenSource = new CancellationTokenSource();

        static readonly char[] _wordTrimChars = new char[] { ' ', '\t', '\r', '\n', ',', '.', '!', '?', ';', ':', '-', '+', '\"', '\'', '(', ')', '[', ']', '{', '}', '<', '>', '|', '=' };


        #region Bindable Properties

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        string[] _keys;

        ObservableCollection<string> _keysColl;

        /// <summary>
        /// Gets or sets the keys. This list contains the collected keys of all dictionaries currently loaded. Used for data binding to the key list.
        /// </summary>
        /// <value>
        /// The keys.
        /// </value>
        public ObservableCollection<string> KeyList
        {
            get
            {
                return _keysColl;
            }
            set
            {
                if (!object.ReferenceEquals(_keys, value))
                {
                    _keysColl = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(KeyList)));
                }
            }
        }

        string _selectedKeyInKeyList;

        /// <summary>
        /// Gets or sets the selected search text (used for binding to the search text TextBox).
        /// </summary>
        /// <value>
        /// The selected search text.
        /// </value>
        public string SelectedKeyInKeyList
        {
            get
            {
                return _selectedKeyInKeyList;
            }
            set
            {
                if (!(_selectedKeyInKeyList == value))
                {
                    _selectedKeyInKeyList = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedKeyInKeyList)));
                }
            }
        }

        private List<string> _bestMatchesList;

        /// <summary>
        /// Gets or sets the list of best matches (key words that are most similar to the original key). Used for data binding to the best matches list.
        /// </summary>
        /// <value>
        /// The best matches list.
        /// </value>
        public List<string> BestMatchesList
        {
            get { return _bestMatchesList; }
            set
            {
                if (!object.ReferenceEquals(_bestMatchesList, value))
                {
                    _bestMatchesList = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BestMatchesList)));
                }
            }
        }

        FlowDocument _flowDocument;

        /// <summary>
        /// Gets or sets the flow document (used for data binding to the FlowDocumentViewer).
        /// </summary>
        /// <value>
        /// The flow document.
        /// </value>
        public FlowDocument FlowDocument
        {
            get
            {
                return _flowDocument;
            }
            set
            {
                if (!object.ReferenceEquals(_flowDocument, value))
                {
                    _flowDocument = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FlowDocument)));
                }
            }
        }

        string _searchText;

        /// <summary>
        /// Gets or sets the selected search text (used for binding to the search text TextBox).
        /// </summary>
        /// <value>
        /// The selected search text.
        /// </value>
        public string SearchText
        {
            get
            {
                return _searchText;
            }
            set
            {
                if (!(_searchText == value))
                {
                    _searchText = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SearchText)));
                    ShowContentForKey(_searchText);
                    UpdateBestMatches(_searchText);
                }
            }
        }

        #endregion

        /// <summary>
        /// Gets the currently loaded dictionaries.
        /// </summary>
        /// <value>
        /// The currently loaded dictionaries.
        /// </value>
        public IList<ISlobDictionary> Dictionaries { get { return _dictionaries; } }


        /// <summary>
        /// Loads a Slob dictionary.
        /// </summary>
        /// <param name="fileName">Name of the dictionary file. The dictionary must be in SLOB format.</param>
        /// <param name="collectKeys">If set to <c>true</c>, the keys of all dictionaries will be collected. When loading multiple dictionaries subsequently, you should set this parameter to true only for the last dictionary loaded.</param>
        public void LoadDictionary(string fileName, bool collectKeys = true)
        {
            var slobReader = new SlobReaderWriter(fileName);
            var dictionary = slobReader.Read();
            dictionary.FileName = fileName;

            _dictionaries.Add(dictionary);

            if (collectKeys)
            {
                CollectAndSortKeys();
            }
        }

        /// <summary>
        /// Collects the keys of all loaded dictionaries and sorts them alphabetically. The list of keys is then published in <see cref="KeyList"/>.
        /// </summary>
        public void CollectAndSortKeys()
        {
            var hashSet = new HashSet<string>();

            foreach (var dict in _dictionaries)
            {
                hashSet.UnionWith(dict.GetKeys());
            }

            _keys = hashSet.ToArray();

            _compareInfo = new CultureInfo("en-US", false).CompareInfo;

            _sortKeys = _keys.Select(x => _compareInfo.GetSortKey(x)).ToArray();
            Array.Sort(_sortKeys, _keys, new UnicodeStringSorter());

            KeyList = new ObservableCollection<string>(_keys);
        }

        /// <summary>
        /// Given the search key <paramref name="searchText"/>, the function looks out for other keys that have the most number of chars in common with the orignal key.
        /// The first 100 best matches are then put into the list <see cref="BestMatchesList"/>.
        /// </summary>
        /// <param name="searchText">The search text.</param>
        public void UpdateBestMatches(string searchText)
        {
            _bestMatchesCancellationTokenSource.Cancel();
            _bestMatchesCancellationTokenSource.Dispose();

            _bestMatchesCancellationTokenSource = new CancellationTokenSource();

            var token = _bestMatchesCancellationTokenSource.Token;

            System.Threading.Tasks.Task.Run(
                () =>
                {
                    _bestMatches.Evaluate(searchText, _keys, token);

                    if (!token.IsCancellationRequested)
                    {
                        BestMatchesList = new List<string>(_bestMatches.BestMatches.Select(x => x.Phrase).Take(Math.Min(100, _keys.Length)));
                    }
                }, token
                );
        }

        public int SearchListLock { get; private set; } = 0;

        /// <summary>
        /// Given the key <paramref name="untrimmedSearchText"/>, this function first of all trims the key a the start and end from unwanted characters. Then it enumerates through all currently loaded dictionaries, 
        /// collects the content found for this key, and finally shows the collected content in a FlowDocument.
        /// </summary>
        /// <param name="untrimmedSearchText">The untrimmed key.</param>
        public void ShowContentForUntrimmedKey(string untrimmedSearchText)
        {
            SearchText = untrimmedSearchText.Trim(_wordTrimChars);
        }

        /// <summary>
        /// Given the key <paramref name="originalSearchText"/>, this function enumerates through all currently loaded dictionaries, 
        /// collects the content found for this key, and finally shows the collected content in a FlowDocument.
        /// </summary>
        /// <param name="originalSearchText">The original search text.</param>
        public void ShowContentForKey(string originalSearchText)
        {
            if (string.IsNullOrEmpty(originalSearchText))
                return;

            (ISlobDictionary Dictionary, string Content, string ContentId, string SearchText)? firstResult = null;

            var listDirect = new List<(ISlobDictionary Dictionary, string Content, string ContentId, string SearchText)>();
            var listVerb = new List<(ISlobDictionary Dictionary, string Content, string ContentId, string SearchText)>();

            var originalSearchTexts = char.IsUpper(originalSearchText[0]) ? new[] { originalSearchText, originalSearchText.ToLowerInvariant() } : new[] { originalSearchText };

            foreach (var originalSearchTextVariant in originalSearchTexts)
            {
                foreach (var dictionary in _dictionaries)
                {
                    var searchText1 = originalSearchTextVariant;
                    var (result1, result1Id, found1) = GetResult(ref searchText1, dictionary);

                    if (null == firstResult)
                        firstResult = (dictionary, result1, result1Id, searchText1);

                    if (found1)
                        listDirect.Add((dictionary, result1, result1Id, searchText1));

                    var searchText2 = "to " + originalSearchText;
                    var (result2, result2Id, found2) = GetResult(ref searchText2, dictionary);

                    if (found2)
                        listVerb.Add((dictionary, result2, result2Id, searchText2));
                }

                if (listDirect.Count != 0 || listVerb.Count != 0)
                    break;

            }

            string searchText = listDirect.Count > 0 ? listDirect[0].SearchText : listVerb.Count > 0 ? listVerb[0].SearchText : firstResult?.SearchText;

            try
            {
                ++SearchListLock;
                SelectedKeyInKeyList = searchText;
            }
            finally
            {
                --SearchListLock;
            }


            if (listDirect.Count > 0 && listVerb.Count > 0)
                ShowContent(listDirect.Concat(listVerb).ToArray());
            else if (listDirect.Count > 0)
                ShowContent(listDirect.ToArray());
            else if (listVerb.Count > 0)
                ShowContent(listVerb.ToArray());
            else if (firstResult.HasValue)
                ShowContent(firstResult.Value);
        }


        /// <summary>
        /// Converts plain text to a <see cref="Section"/>.
        /// </summary>
        /// <param name="key">The key. It is neccessary here because text files do not neccessarily contain the key, so that the key is used as title here.</param>
        /// <param name="content">The content in plain text format.</param>
        /// <returns>A <see cref="Section"/> that represents the plain text content.</returns>
        public Section ConvertTextContentToSection(string key, string content)
        {
            var doc = new Section();

            var para = new Paragraph();
            var run = new Run(key);
            run.FontSize = 22;

            para.AppendChild(run);
            doc.AppendChild(para);

            para = new Paragraph();
            run = new Run(content);

            para.AppendChild(run);
            doc.AppendChild(para);

            return doc;
        }

        /// <summary>
        /// Converts TEI XML content to a <see cref="Section"/>.
        /// </summary>
        /// <param name="content">The content to convert. It must be in TEI XML format (see README.md of <see href="https://github.com/freedict/fd-dictionaries"/>).</param>
        /// <returns>A <see cref="Section"/> that represents the TEI XML content.</returns>
        public Section ConvertTeiContentToSection(string content)
        {
            var doc = new Section();


            using (var tr = System.Xml.XmlReader.Create(new StringReader(content)))
            {
                tr.MoveToContent();
                tr.ReadToDescendant("orth");
                var orth = tr.ReadElementContentAsString();

                if (null != orth)
                {
                    var para = new Paragraph();
                    para.AppendChild(new Run(orth) { FontSize = 23 });
                    doc.AppendChild(para);
                }

                if (tr.Name == "pron")
                {
                    var pron = tr.ReadElementContentAsString();
                    var para = new Paragraph();
                    para.AppendChild(new Run(pron) { FontSize = 18 });
                    doc.AppendChild(para);
                }

                tr.ReadToFollowing("sense");
                tr.ReadStartElement("sense");

                var sensePara = new Paragraph();
                doc.AppendChild(sensePara);

                while (tr.Name == "cit")
                {
                    string quote = null;

                    tr.ReadStartElement("cit");
                    if (tr.Name == "quote")
                    {
                        quote = tr.ReadElementContentAsString();
                    }

                    if (null != quote)
                    {
                        if (sensePara.Childs.Count != 0)
                        {
                            sensePara.AppendChild(new Run("; ") { FontSize = 15 });
                        }
                        sensePara.AppendChild(new Run(quote) { FontSize = 15 });
                    }

                    tr.ReadToFollowing("cit");
                }

            }


            return doc;
        }

        /// <summary>
        /// Converts HTML content returned by a dictionary to a <see cref="Section"/>.
        /// </summary>
        /// <param name="dictionary">The dictionary. It is neccessary to retrieve CSS files referenced by the HTML content.</param>
        /// <param name="htmlContent">The HTML to convert.</param>
        /// <returns>A <see cref="Section"/> that represents the HTML content.</returns>
        public Section ConvertHtmlContentToSection(ISlobDictionary dictionary, string htmlContent)
        {
            Func<string, string> cssStyleSheetProvider = (cssFileName) =>
            {
                if (dictionary.TryGetValue(cssFileName, out var entry))
                {
                    return entry.Content;
                }
                else
                {
                    return null;
                }
            };

            try
            {
                var section = (Section)_converter.Convert(htmlContent, false, cssStyleSheetProvider);
                return section;
            }
            catch (Exception ex)
            {

            }


            return null;

        }

        /// <summary>
        /// Tries to find the key <paramref name="searchText"/> in the dictionary.
        /// </summary>
        /// <param name="searchText">The text to search. If this original text was not found, on return, the parameter contains the key for which the content is returned.</param>
        /// <param name="dictionary">The dictionary.</param>
        /// <returns>A tuple consisting of the content, the content identifer, and a boolean. The boolean is true if the original search text was found in the dictionary.
        /// If the original search text was not found, the boolean is false, and <paramref name="searchText"/>contains the search text that was found.</returns>
        private (string Content, string ContentId, bool foundOriginal) GetResult(ref string searchText, ISlobDictionary dictionary)
        {
            var searchKey = _compareInfo.GetSortKey(searchText);
            var index = Array.BinarySearch(_sortKeys, searchKey, new UnicodeStringSorter());


            (string Content, string ContentId) result = (null, null);
            bool found = false;

            if (index >= 0)
            {
                result = dictionary[searchText];
                found = true;
            }
            else
            {
                index = ~index;

                if (index < _keys.Length)
                {
                    searchText = _keys[index];
                    result = dictionary[searchText];
                }
            }

            return (result.Content, result.ContentId, found);
        }

        public FlowDocument ShowContent(params (ISlobDictionary Dictionary, string ContentText, string ContentID, string SearchKey)[] contents)
        {
            var doc = new FlowDocument();

            foreach (var (Dictionary, ContentText, ContentID, SearchKey) in contents)
            {
                if (string.IsNullOrEmpty(ContentID) || string.IsNullOrEmpty(ContentText) || string.IsNullOrEmpty(SearchKey))
                    continue;

                if (ContentID.StartsWith("text/html"))
                {
                    var section = ConvertHtmlContentToSection(Dictionary, ContentText);
                    if (null != section)
                        doc.AppendChild(section);

                }
                else if (ContentID.StartsWith("text/plain"))
                {
                    var section = ConvertTextContentToSection(SearchKey, ContentText);
                    if (null != section)
                        doc.AppendChild(section);

                }
                else if (ContentID.StartsWith("application/tei+xml"))
                {
                    var section = ConvertTeiContentToSection(ContentText);
                    doc.AppendChild(section);
                }
            }

            FlowDocument = doc;
            return doc;
        }




    }
}
