using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Airi.TheSystem.Syntax
{
    /// <Improvement>Development Note on Preparing Syntax Library
    /// - WikiDict contain phrases as items, which causes we treat complete phrases as a noun, we need to figure out a way around that

    /// <summary>
    /// Loads all phrases (language neutral) from a dictionary file and provides other useful information regarding specific vocabulary:
    /// - Including common simple words
    /// - Including terminologies and technical words
    /// - Including some common-sense names/notations
    /// - Including commonly-used expressions
    /// - Sources: English dictionary, WIkiDict, Wikipedia, and manually edited effort
    /// Extra features:
    /// - Provides interface to access wikipedia and wikidict for further real-time queries (Utilizing our Wiki library, which also provides other functions like getting wiki updates)
    /// Development Notes:
    /// - There is some overlap between this class and Memory.BaseMemory, but that was intentional since we provide different interfaces: this class is fundamental while BaseMemory is specialized; Or we might want to resolve this problem because part of BaseMemory is deprecated
    /// - This class is NO LONGER used for query purpose (and thus can be on-disk, and are kept in physical memory instead of disk) and now IS BECOMING an inherent part of Airi's run-time memory
    /// </summary>
    class VocabularyManager
    {
        // ---------------------- Initialize vocabulary database ---------------------- //
        // Configurations
        // static readonly private string DefaultDictionaryFile = "Airi.Dict";

        // Default Constructor
        public VocabularyManager()
        {
            // Use one file and every knowledge is there
            LoadDictionary();

            // <Debug>
            // InitializeVocabularyAndKnowledgeBase();
        }

        // <Debug>
        // Testing on existing resources in isolated files
        private void InitializeVocabularyAndKnowledgeBase()
        {
            // <Debug> Use of absoluate locations for debugging purpose (and thus deliberately not portable)
            // <Notice> Make sure all files encoded in UTF8
            string WordNetDictLocation = @"C:\Users\szinu\Desktop\Dict\Libraries In Use\WordNetDictFiles\";
            string[] TestWordTypes = new string[] { "adj","adv","noun","verb" };
            WordAttribute[] TestWordAttributes = new WordAttribute[] { WordAttribute.adj, WordAttribute.adv, WordAttribute.noun, WordAttribute.verb };
            string RawVocabularyFile = @"C:\Users\szinu\Desktop\Wikipedia and Wikidict\VocabularyList\UnProcessedWordsList.txt";
            string WikiFiles = @"C:\Users\szinu\Desktop\Wikipedia and Wikidict\VocabularyList\WikiTitles.txt";
            string WiktionaryFile = @"C:\Users\szinu\Desktop\Wikipedia and Wikidict\VocabularyList\WiktionaryTitles.txt";
            //string WiktionaryAbstractFile = "Airi.Dict";
            //string WikipediaAbstractFile = "Airi.Dict";
            // <Development> Also consider further utlization of wiki by enabling dynamic web queries using Wiki API for Airi
            // For that purpose a VocabularyManager can be kept alive as long as needed and when more information regarding a specific item is needed it will query Wikipedia

            // Initialize Phrases
            EnglishPhrases = new Dictionary<string, Dictionary<string, Phrase>>();

            // Load Phrases
            // Iterate through all vocabularies and populate phrases, mark its word type accordingly
            // <Improvement> Notice specifics about verbs are not decided yet
            // <Improvement> Notice specifics about words other than noun, verb, adj and adv are not decided yet (currently all disposed)

            /// Stage One: WN Vocabulary
            /// <Improvement> Currently this stage is takes most of loading spee, use a text/binary file instead
            // Set up library
            WNCommon.path = WordNetDictLocation;

            // <Debug> Timing
            var watch = System.Diagnostics.Stopwatch.StartNew();
            int nItems = 0;

            // For each word we test to see whether they are of proper type
            foreach (string line in File.ReadLines(RawVocabularyFile))
            {
                WordAttribute? temp = null;
                nItems++;

                for (int i = 0; i < TestWordTypes.Length; i++)
                {
                    WordType pos = WordNetLibrary.WordType.of(TestWordTypes[i]);
                    SearchSet searchSet = WordNetLibrary.WordNetDatabase.FindPossibleDefinitions(line, pos);
                    if (searchSet.NonEmpty == true)
                    {
                        if (temp == null) temp = TestWordAttributes[i];
                        else temp = temp | TestWordAttributes[i];
                    }
                }

                if(temp != null)
                {
                    // Decide Division Domain by first character
                    string divisor = line[0].ToString();    // <Debug> Assumed first char always exist
                    if(EnglishPhrases.ContainsKey(divisor))
                        EnglishPhrases[divisor].Add(line, new Phrase(line, (WordAttribute)temp));
                    else
                        EnglishPhrases.Add(divisor, new Dictionary<string, Phrase>());
                }
            }

            /// Stage 2: Wiki Phrases - Iterate through extra information and populate phrases, mark as noun
            //// Wikipedia
            //foreach (string line in File.ReadLines(WikiFiles))
            //{
            //    nItems++; // Statistics

            //    string divisor = line[0].ToString();
            //    if (EnglishPhrases.ContainsKey(divisor) == false)
            //        EnglishPhrases.Add(divisor, new Dictionary<string, Phrase>());
            //    else
            //    {
            //        // Also make sure the phrase not exist
            //        if (EnglishPhrases[divisor].ContainsKey(line) == false)
            //        {
            //            EnglishPhrases[divisor].Add(line, new Phrase(line, WordAttribute.noun));
            //        }
            //    }
            //}
            //// Wikidict
            //foreach (string line in File.ReadLines(WiktionaryFile))
            //{
            //    nItems++; // Statistics

            //    string divisor = line[0].ToString();
            //    if (EnglishPhrases.ContainsKey(divisor) == false)
            //        EnglishPhrases.Add(divisor, new Dictionary<string, Phrase>());
            //    else
            //    {
            //        // Also make sure the phrase not already exist
            //        if (EnglishPhrases[divisor].ContainsKey(line) == false)
            //        {
            //            EnglishPhrases[divisor].Add(line, new Phrase(line, WordAttribute.noun));
            //        }
            //    }
            //}

            /// Stage 3: Wiki Phrases along with their meaning
            /// Might consider not loading these before hand but develope a mechnism for query per need to save memory
            /// ...

            // Timing
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            System.Console.WriteLine("Parsing took: " + elapsedMs + "ms for " + nItems + " items.");
        }

        // Load a specific file containing custom format of dictionary information
        public LoadDictionary()
        {
            // Create a new dictionary
            EnglishPhrases = new Dictionary<string, Dictionary<string, Phrase>>(); // Notice values of this dictionary (the inside dictionaries) not created yet

            // <Debug> Timing
            var watch = System.Diagnostics.Stopwatch.StartNew();
            int nItems = 0;

            // -------------------------------- Reference ---------------------------------- //
            // For each word we test to see whether they are of proper type
            foreach (string line in File.ReadLines(RawVocabularyFile))
            {
                WordAttribute? temp = null;
                nItems++;

                for (int i = 0; i < TestWordTypes.Length; i++)
                {
                    WordType pos = WordNetLibrary.WordType.of(TestWordTypes[i]);
                    SearchSet searchSet = WordNetLibrary.WordNetDatabase.FindPossibleDefinitions(line, pos);
                    if (searchSet.NonEmpty == true)
                    {
                        if (temp == null) temp = TestWordAttributes[i];
                        else temp = temp | TestWordAttributes[i];
                    }
                }

                if (temp != null)
                {
                    // Decide Division Domain by first character
                    string divisor = line[0].ToString();    // <Debug> Assumed first char always exist
                    if (EnglishPhrases.ContainsKey(divisor))
                        EnglishPhrases[divisor].Add(line, new Phrase(line, (WordAttribute)temp));
                    else
                        EnglishPhrases.Add(divisor, new Dictionary<string, Phrase>());
                }
            }

            /// Stage 2: Wiki Phrases - Iterate through extra information and populate phrases, mark as noun
            //// Wikipedia
            //foreach (string line in File.ReadLines(WikiFiles))
            //{
            //    nItems++; // Statistics

            //    string divisor = line[0].ToString();
            //    if (EnglishPhrases.ContainsKey(divisor) == false)
            //        EnglishPhrases.Add(divisor, new Dictionary<string, Phrase>());
            //    else
            //    {
            //        // Also make sure the phrase not exist
            //        if (EnglishPhrases[divisor].ContainsKey(line) == false)
            //        {
            //            EnglishPhrases[divisor].Add(line, new Phrase(line, WordAttribute.noun));
            //        }
            //    }
            //}
            //// Wikidict
            //foreach (string line in File.ReadLines(WiktionaryFile))
            //{
            //    nItems++; // Statistics

            //    string divisor = line[0].ToString();
            //    if (EnglishPhrases.ContainsKey(divisor) == false)
            //        EnglishPhrases.Add(divisor, new Dictionary<string, Phrase>());
            //    else
            //    {
            //        // Also make sure the phrase not already exist
            //        if (EnglishPhrases[divisor].ContainsKey(line) == false)
            //        {
            //            EnglishPhrases[divisor].Add(line, new Phrase(line, WordAttribute.noun));
            //        }
            //    }
            //}

            /// Stage 3: Wiki Phrases along with their meaning
            /// Might consider not loading these before hand but develope a mechnism for query per need to save memory
            /// ...
            // -------------------------------- Reference ---------------------------------- //

            // Load file using custom format
            StreamReader reader;
            try
            {
                reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(Airi.Properties.Resources.VocabularySheetPath));
                if (reader != null)
                {
                    string fileContent = reader.ReadToEnd();
                    string[] lines = fileContent.Split(new char[] { '\n', '\r' });

                    foreach (string line in lines)
                    {
                        if (line == String.Empty || line[0] == '#') continue;
                        // Process an item
                        // ...
                    }
                }
            }
            catch
            {
                throw new Exception();
            }

            // Timing
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            System.Console.WriteLine("Parsing took: " + elapsedMs + "ms for " + nItems + " items.");
        }

        // ---------------------- Data Members ---------------------- //
        // Use multi-layer (or we call it "domain") division to storable extend knowledge range - not limited to 26 characters; For other languages e.g. Chinese it's more intelligent not to do it this way
        // Since those languages are character orientend and have a lot of words with different beginning characters
        private Dictionary<string, Dictionary<string, Phrase>> EnglishPhrases { get; set; }
        // For ease of searching in the multi-domain phrase
        // Returns whether the tring is a phrase in the dictionary
        private Phrase ContainsPhrase(string word)
        {
            string divisor = word[0].ToString();
            if (EnglishPhrases.ContainsKey(divisor) == false)
            {
                return null;
            }
            if(EnglishPhrases[divisor].ContainsKey(word) == false)
            {
                return null;
            }
            return EnglishPhrases[divisor][word];
        }
        // Designer specified
        private Dictionary<string, List<Phrase>> Categories { get; set; } // An item inside a category must be a valid phrase
        // <Pending> Pending initailzie Cateogries

        // ---------------------- Language Interpretation ---------------------- //
        public bool IsInCategory(string category, Phrase phrase)
        {
            if (Categories.ContainsKey(category) == false)
                throw new InvalidOperationException("Category doesn't exist.");

            if (phrase != null)
                return Categories[category].Contains(phrase);
            else
                return false;
        }

        public bool IsNotInCategory(string category, Phrase phrase)
        {
            if (Categories.ContainsKey(category) == false)
                throw new InvalidOperationException("Category doesn't exist.");

            if (phrase != null)
                return true;    // If the phrase doesn't even exist then it must not be in any existing categories
            else
                return !Categories[category].Contains(phrase);
        }

        // Given a word (and make sure it is only a word, no space is contained), and return whether the word is a synonym or varying form to target
        // @Return false if not
        public bool IsWordVaryingFormOrSynonym(string word, string targetWord)
        {
            // Check and ensure it's a word
            if (word.Contains(' ')) throw new ArgumentException("Invalid word input: expect a valid word containing no spaces.");
            if (targetWord.Contains(' ')) throw new ArgumentException("Invalid target word input: expect a valid word containing no spaces.");

            // Check whether target word exists
            Phrase target = ContainsPhrase(targetWord);
            if (target == null) throw new InvalidOperationException("Invalid target word: the word isn't listed in dictionary.");
            // Also we assume the given target word in this case is an original form

            // Check whether source word exists, if not then it's definitely not gonna be a synnoym
            if (ContainsPhrase(word) == null) return false;

            // <Improvement> Check whether or not this is the original word otherwise its synnonyms are none
            // Return true if source is found to be an varying form or synnonym of source
            return target.Synonyms.Where(p => p.Key == word).ToList().Count > 0
                || target.Forms.Where(p => p.Key == word).ToList().Count > 0;   // <Performance> Might be able to return immediately instead of going through all
        }

        /// <summary>
        /// Given a string with unbounded length, return whether the beginning part of it is a synonym or varying form of the target phrase
        /// </summary>
        /// <param name="inputPhrase">Can be an unbounded string, notice actual matched part doesn't need to have any length similarity to target (e.g. in case of langauge translation or secrese passphrases</param>
        /// <param name="targetPhrase">The target phrase to be matched</param>
        /// /// <param name="matchLength">Number of characters in the match of the input string</param>
        /// <returns>Return false if not</returns>
        // Notice ToLower() is used to support matching English phrases
        public bool IsPraseVaryingFormOrSynonymUndetermined(string inputPhrase, string targetPhrase, out int matchLength)
        {
            // Check whether target word exists
            Phrase target = ContainsPhrase(targetPhrase);
            if (target == null) throw new InvalidOperationException("Invalid target word: the word isn't listed in dictionary.");
            // Also we assume the given targetPhrase in this case is an original form (This is defined inside pattern definition file)

            // Check match original form
            if(inputPhrase.ToLower().IndexOf(targetPhrase) == 0)
            {
                matchLength = targetPhrase.Length; // Notice in general inputPhrase.Length doesn't need to equal to targetPhrase.Length, but since it's original form here so it's equal
                return true;
            }

            // Check match with each of the varying forms
            foreach (Phrase form in target.Forms)
            {
                if(inputPhrase.ToLower().IndexOf(form.Key.ToLower()) == 0)
                {
                    matchLength = form.Key.Length;
                    return true;
                }
            }

            // Check match with each of the synnonyms
            foreach (Phrase form in target.Synonyms)
            {
                if (inputPhrase.ToLower().IndexOf(form.Key.ToLower()) == 0)
                {
                    matchLength = form.Key.Length;
                    return true;
                }
            }

            matchLength = 0;
            return false;
        }

        /// <summary>
        /// Given a string, try to extract phrases that doesn't exist in dictionary from it
        /// </summary>
        /// <param name="sequence"></param>
        /// <returns>return null if the content is perfectly recognizable</returns>
        // Internally it's implemented like GetPhrase() but this time we try to find the longest non-match
        public string GetUnknownPhrase(string sequence)
        {
            // Get individual words
            // <Improvement> Don't assume words and space delimiters, just count characters
            string[] words = sequence.Trim().Split(new char[] { ' ' });

            // Stage 1: Since there are only limited amount of words input, there are only limited amount of potential matches
            string[] potentialMatches = new string[words.Length];
            potentialMatches[0] = words[0];
            for (int i = 1; i < words.Length; i++)
            {
                potentialMatches[i] = potentialMatches[i - 1] + ' ' + words[i];
            }
            // Stage 2: Check which ones DO NOT match and store the longest one
            string longestNoMatch = null;
            foreach (string candidate in potentialMatches)
            {
                Phrase match = ContainsPhrase(candidate);
                if (match == null) longestNoMatch = candidate;
                else break;
            }

            // Return the not-yet-existing-in-dictionary phrase
            return longestNoMatch;
        }

        // Given a word, a phrase, or other long sequence of characters, return the phrase that it can possibly match
        // The phrase itself needs to be an exact match in appearnce (i.e. "sunny days..." doesn't match "sunny day")
        // @Input requires input doesn't have auxiliary beginning and trailing spaces
        // @words can be longer than matched (return) result, but not shorter
        // An input will match a phrase in following cases:
        //  - it is an exact match (be it a word or a phrase)
        //  - When tolerance enabled it will allow fuzzy matching (i.e. allow certain mistyping and synnonym etc. meaning guess) (not implemented yet)
        //  - Substring of the words matches a phrase
        // @Return can be null if nout found
        // @Usage Case: Given any sentence or part of a sentence we wish to see whether its beginning words matches any phrase
        // @Usage Case: Given any Chinese sentence we seperate it into different valid words (<Improvement> This feature is not impelemnted yet) 
        public Phrase GetPhrase(string sequence, bool bFuzzy = false)
        {
            // Get individual words
            // <Improvement> Don't assume words and space delimiters, just count characters
            string[] words = sequence.Trim().Split(new char[] { ' ' });

            // Method 1
            // Stage 1: Get all of the phrases with the beginning word as the first word
            // Stage 2: Iterate through remaining words in words array and find a best match
            // Remark: That wasn't efficient considering our current dictionary implementation of phrases storage, and iterating through all takes too much effort

            // Method 2
            // Stage 1: Since there are only limited amount of words input, there are only limited amount of potential matches
            string[] potentialMatches = new string[words.Length];
            potentialMatches[0] = words[0];
            for (int i = 1; i < words.Length; i++)
            {
                potentialMatches[i] = potentialMatches[i-1] + ' ' + words[i];
            }
            // Stage 2: Check which ones match and store the longest one
            Phrase longestMatch = null;
            foreach (string candidate in potentialMatches)
            {
                Phrase match = ContainsPhrase(candidate);
                if (match != null) longestMatch = match;
            }

            // Return the phrase
            return longestMatch;
        }

        // Given a word, a phrase, or other long sequence of characters, and return whether it is a synonym to target -- this function isn't implemented because it's not used; Also because when used 
        //  it will require some rather involved caller logic since the length cannot be predetermined. This migth be useful for Chinese recognition though but not our current schedule.
        // @Return false if not
        //public bool IsPhraseVaryingFormOrSynonym(string phrase, string targetPhrase)
        //{

        //}

        // ---------------------- Converter and File Manipulation functions ---------------------- //
        static public void DictonaryConverter(string inputFilePath, string outputFilePath)
        {
            throw new NotImplementedException();
        }

        // ---------------------- Advanced Dictionary Usage ---------------------- //
        // Send request to Wiki and download information page and do some processing to display more information to user
        // Might return a formated object for further processing
        public /*SomeObjectType instead of void*/ void QueryPhraseDetails()
        {
            throw new NotImplementedException();
        }

        // Allow Airi to fetch Wiki for news, wiki's features etc. other things so she can study them herself or forward such information to us e.g. for greetings.
        // This function is put here because currently VocabularyManager (will) has access to WikiDictionary but in the future might be encapsulated and defined as an Activator
        //  for more flexible usages
        public string GetSomethingNew()
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// The smallest vocabulary unit; Record only directly useful information; Metadata are appended in other facilities e.g. Memory module
    /// There is no fundamental difference between a phrase and a word since in reality a phrase can be interpreted as a completely new word despite the fact that its componetns might have individual meaning
    /// Its considered the longer phrase whenever components occur in order, e.g. object oriented programming is considered as one phrase, not three phrases (words)
    /// </summary>
    class Phrase
    {
        public Phrase(string value, WordAttribute wordType, Phrase origin = null)
        {
            // Set key
            Key = value;

            // Set Attribtues
            Attribute = wordType;

            // Set origin
            if(origin == null)
            {
                OriginalForm = this;
            }
            else
            {
                OriginalForm = origin;
            }
        }

        // ---------------------- Properties ---------------------- //
        public Phrase OriginalForm = null;  // Indicates whether current phrase is a varying form of orignal, for original this should be set to self
        public string Key { get; set; } // Phrase face
        public int WordCount { get { return Key.Count(c => c == ' ') + 1; } }
        public WordAttribute Attribute { get; set; } // The characteristic property of the word, can contain more than one value
        // Usage: e.g. (Attribute&WordAttribute.Verb == WordAttribute.Verb)

        // The below have value only when OriginalForm == null
        public List<Phrase> Forms { get; set; }    // Other forms that represent the same word maybe in different tense
        public List<Phrase> Synonyms { get; set; }    // (Not used for now, might prove useful when deducting meanings) Words with similar meaning, might not contain their complete variable forms
        public List<Phrase> Opposites { get; set; }    // (Not used for now, might prove useful when deducting meanings) Words with opposite meaning, might not contain their complete variable forms
    }

    // Usage: 
    /// <summary>
    /// Defines Syntactical attributes for a word, can be logiclaly combined
    /// Notice WordAttribute applies both to words and phrases; During matching process, if a pattern gives an Attribute Match element then we need to check again phrases
    /// Related grammar concepts see Longman Grammar Sections; Notice pos (and grammars in general) only serve as a guide, not absolute reference
    /// </summary>
    // To parse: https://msdn.microsoft.com/en-us/library/system.enum.getname(v=vs.110).aspx, https://msdn.microsoft.com/en-us/library/16c1xs4z(v=vs.110).aspx, https://msdn.microsoft.com/en-us/library/essfb559(v=vs.110).aspx
    // Flags: http://stackoverflow.com/questions/8447/what-does-the-flags-enum-attribute-mean-in-c
    // TryParse: https://msdn.microsoft.com/en-us/library/dd783499(v=vs.110).aspx
    [Flags]
    internal enum WordAttribute
    {
        //// Nouns
        // Summary:
        //     Can be used as a verb
        verb = 0,
        // Summary:
        //     Can be used as a noun
        noun = 1,
        // Summary:
        //     Can be used as adjective
        adj = 2,
        // Summary:
        //     Can be used as adverb
        adv = 4,
        // Summary:
        //     Can be used as pronoun
        pron = 8,
        // Summary:
        //     Can be used as conjunction
        conj = 16,
        // Summary:
        //     Can be used as determiner (e.g. what, some)
        det = 32,
        // Summary:
        //     Can be used as a predeterminer (e.g. what, all, both)
        pred = 64,
        // Summary:
        //     Can be used as article (e.g. the, a, an)
        art = 128,
        // Summary:
        //     Can be used as proposition (e.g. except, see longman dictonary on "except" for more details)
        prep = 256,

        //// Verbs
        // Summary:
        //     Is used as Modal verb
        modal = 512,
        // Summary:
        //     Can be used transitive
        transitive = 1024,
        // Summary:
        //     Can be used intransitive
        intransitive = 2048,
        // Summary:
        //     Is Simple Present
        simple = 4096,
        // Summary:
        //     Is Present Progressive
        present = 8192,
        // Summary:
        //     Is present particle form
        // http://grammar.about.com/od/basicsentencegrammar/f/progpartdiff.htm
        gerund = 16384,  
        // Summary:
        //     Is Simple Past
        past = 32768,
        // Summary:
        //     Is Present Perfect
        perfect = 65536,
        // Summary:
        //     Wild match for any; This isn't specified in dicitonary but used only manually
        any = 1073741824 // The 31th bit
    }
}
