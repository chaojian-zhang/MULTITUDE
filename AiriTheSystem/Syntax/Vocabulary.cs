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
    /// Extra features:
    /// - Provides interface to access wikipedia and wikidict for further real-time queries (Utilizing our Wiki library, which also provides other functions like getting wiki updates)
    /// Development Notes:
    /// - There is some overlap between this class and Memory.BaseMemory, but that was intentional since we provide different interfaces: this class is fundamental while BaseMemory is specialized; Or we might want to resolve this problem because part of BaseMemory is deprecated
    /// - This class is NO LONGER used for query purpose (e.g. like WordNet) (and thus can be on-disk, and are kept in physical memory instead of disk) and now IS BECOMING an inherent part of Airi's run-time memory
    /// </summary>
    class VocabularyManager
    {
        // ---------------------- Initialize vocabulary database ---------------------- //
        // Default Constructor
        public VocabularyManager()
        {
            // Use one file and every knowledge is there
            LoadDictionary();
        }

        // Load a specific file containing custom format of dictionary information
        private void LoadDictionary()
        {
            // Create a new dictionary
            EnglishPhrases = new Dictionary<string, Dictionary<string, Phrase>>(); // Notice values of this dictionary (the inside dictionaries) not created yet
            // Create a new category
            Categories = new Dictionary<string, Category>();

            // <Debug> Timing
            var watch = System.Diagnostics.Stopwatch.StartNew();
            int nItems = 0;

            // <Improvement> For exact definitions/abstracts we might consider not loading these before hand but develope a mechnism (e.g. record line number, or byte offset into the file) for query per need to save memory
            // Load file using custom format
            StreamReader reader;
            try
            {
                reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(Airi.Properties.Resources.VocabularySheetPath));
                if (reader != null)
                {
                    string fileContent = reader.ReadToEnd();
                    string[] lines = fileContent.Split(new char[] { '\n', '\r' });

                    // Prepare variables for constructing a phrase
                    Phrase phrase = null;
                    bool bNewDef = false;
                    WordAttribute? attribute = null;
                    List<Phrase> forms = null, synonyms = null, opposites = null;
                    string definition = null;
                    string Abstract = null;
                    foreach (string line in lines)
                    {
                        // Skip empty lines
                        string lineContent = line.TrimStart();
                        if (lineContent == String.Empty || lineContent[0] == '#') continue;

                        // Process an item
                        // Extract token
                        int startTokenIndex = lineContent.IndexOf('[');
                        int endTokenIndex = lineContent.IndexOf(']');
                        string token = lineContent.Substring(startTokenIndex + 1, endTokenIndex - startTokenIndex - 1);
                        string value = lineContent.Substring(endTokenIndex + 1);
                        switch (token)
                        {
                            case "Word":
                                // Save previously saved definition contents
                                if (bNewDef == true)
                                {
                                    phrase.AddDefinition(attribute.Value, forms, synonyms, opposites, definition, Abstract);
                                }
                                // Re-initialize variables
                                forms = null;
                                attribute = null;
                                synonyms = null;
                                opposites = null;
                                definition = null;
                                Abstract = null;
                                bNewDef = false;
                                // Add phrase
                                phrase = ContainsPhrase(value, true);
                                if(phrase == null)
                                {
                                    phrase = new Phrase(value);
                                    EnglishPhrases[value[0].ToString()].Add(value, phrase);
                                }

                                // Statistics
                                nItems++;
                                break;
                            case "Definition":
                                // Save previously saved definition contents
                                if (bNewDef == true)
                                {
                                    phrase.AddDefinition(attribute.Value, forms, synonyms, opposites, definition, Abstract);
                                }
                                else bNewDef = !bNewDef;
                                // Extract element
                                int attributeStartIndex = value.IndexOf('<');
                                int attributeEndIndex = value.IndexOf('>');
                                string attributeString = value.Substring(attributeStartIndex + 1, attributeEndIndex - attributeStartIndex - 1);
                                attribute = (WordAttribute)Enum.Parse(typeof(WordAttribute), attributeString);
                                definition = value.Substring(attributeEndIndex + 1);
                                break;
                            case "Form":
                                string[] formsStrings = value.Split(new char[] { ',' });
                                foreach (string formString in formsStrings)
                                {
                                    string formStringTemp = formString.TrimStart();
                                    if(formStringTemp != string.Empty)
                                    {
                                        // Create a new phrase list
                                        if (forms == null) forms = new List<Phrase>();
                                        // Add string to the phrase
                                        Phrase formPhrase = ContainsPhrase(formStringTemp, true);
                                        if (formPhrase == null)
                                        {
                                            formPhrase = new Phrase(formStringTemp);
                                            EnglishPhrases[formStringTemp[0].ToString()].Add(formStringTemp, formPhrase);
                                        }
                                        forms.Add(formPhrase);
                                    }
                                }
                                break;
                            case "Synonym":
                                string[] synsStrings = value.Split(new char[] { ',' });
                                foreach (string synString in synsStrings)
                                {
                                    string synStringTemp = synString.TrimStart();
                                    if (synStringTemp != string.Empty)
                                    {
                                        // Create a new syns list
                                        if (synonyms == null) synonyms = new List<Phrase>();
                                        // Add string to the phrase
                                        Phrase synPhrase = ContainsPhrase(synStringTemp, true);
                                        if (synPhrase == null)
                                        {
                                            synPhrase = new Phrase(synStringTemp);
                                            EnglishPhrases[synStringTemp[0].ToString()].Add(synStringTemp, synPhrase);
                                        }
                                        synonyms.Add(synPhrase);
                                    }
                                }
                                break;
                            case "Opposite":
                                string[] oppsStrings = value.Split(new char[] { ',' });
                                foreach (string oppString in oppsStrings)
                                {
                                    string oppStringTemp = oppString.TrimStart();
                                    if (oppStringTemp != string.Empty)
                                    {
                                        // Create a new syns list
                                        if (opposites == null) opposites = new List<Phrase>();
                                        // Add string to the phrase
                                        Phrase oppPhrase = ContainsPhrase(oppStringTemp, true);
                                        if (oppPhrase == null)
                                        {
                                            oppPhrase = new Phrase(oppStringTemp);
                                            EnglishPhrases[oppStringTemp[0].ToString()].Add(oppStringTemp, oppPhrase);
                                        }
                                        opposites.Add(oppPhrase);
                                    }
                                }
                                break;
                            case "Abstract":
                                Abstract = value;
                                break;
                            default:
                                break;
                        }
                        // Notice forms, synonyms and opposites just add their corresponding phrases without recursively connect self to them since those are defined by later dictionary items and not necessary as well (If A->B it doesn't need to be B->A)
                    }
                    // Save last definition because that is end of file and no other element to cause it being saved
                    phrase.AddDefinition(attribute.Value, forms, synonyms, opposites, definition, Abstract);
                }
            }
            catch
            {
                throw new Exception("Cannot read vocabular sheet file.");
            }

            // <Debug> Timing
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            System.Console.WriteLine("Parsing took: " + elapsedMs + "ms for " + nItems + " items.");
        }

        // ---------------------- Wikipedia Queries ---------------------- //
        // Enabling dynamic web queries using Wiki API for Airi
        public string SearchWikipediaPhrase()
        {
            throw new NotImplementedException();
        }

        // ---------------------- Data Members ---------------------- //
        // Use multi-layer (or we call it "domain") division to storable extend knowledge range - not limited to 26 characters; For other languages e.g. Chinese it's more intelligent not to do it this way
        // Since those languages are character orientend and have a lot of words with different beginning characters
        private Dictionary<string, Dictionary<string, Phrase>> EnglishPhrases { get; set; }
        // For ease of searching in the multi-domain phrase
        // Returns whether the tring is a phrase in the dictionary
        private Phrase ContainsPhrase(string word, bool bCreateDivision = false)
        {
            // Decide Division Domain by first character
            string divisor = word[0].ToString();    // <Debug> Assumed first char always exist
            if (EnglishPhrases.ContainsKey(divisor) == false)
            {
                if (bCreateDivision) EnglishPhrases.Add(divisor, new Dictionary<string, Phrase>());
                return null;
            }
            if(EnglishPhrases[divisor].ContainsKey(word) == false)
            {
                return null;
            }
            return EnglishPhrases[divisor][word];
        }
        // Designer specified
        private Dictionary<string, Category> Categories { get; set; } // An item inside a category must be a valid phrase
        // <Pending> Pending initailzie Cateogries

        // ---------------------- Language Interpretation ---------------------- //
        public bool IsInCategory(string category, Phrase phrase)
        {
            if (Categories.ContainsKey(category) == false)
                // throw new InvalidOperationException("Category doesn't exist.");  // <Debug> Might want to check this while generating patterns, not while matching
                return false;

            if (phrase != null)
                return Categories[category].Items.Contains(phrase);
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
                return !Categories[category].Items.Contains(phrase);
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
        public bool IsPraseVaryingFormOrSynonymUndetermined(string inputPhrase, string targetPhrase, ref int matchLength)
        {
            // Check whether target word exists
            Phrase target = ContainsPhrase(targetPhrase);
            if (target == null) throw new InvalidOperationException("Invalid target word: the word isn't listed in dictionary.");
            // Also we assume the given targetPhrase in this case is an original form (This is defined inside pattern definition file)

            // Check match original form
            if(inputPhrase.ToLower().IndexOf(targetPhrase) == 0)
            {
                matchLength += targetPhrase.Length; // Notice in general inputPhrase.Length doesn't need to equal to targetPhrase.Length, but since it's original form here so it's equal
                return true;
            }

            // Check match with each of the varying forms
            foreach (Phrase form in target.Forms)
            {
                if(inputPhrase.ToLower().IndexOf(form.Key.ToLower()) == 0)
                {
                    matchLength += form.Key.Length;
                    return true;
                }
            }

            // Check match with each of the synnonyms
            foreach (Phrase form in target.Synonyms)
            {
                if (inputPhrase.ToLower().IndexOf(form.Key.ToLower()) == 0)
                {
                    matchLength += form.Key.Length;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Given a string, try to extract phrases that doesn't exist in dictionary from it
        /// </summary>
        /// <param name="sequence"></param>
        /// <returns>return null if the content is perfectly recognizable</returns>
        // Internally it's implemented like GetPhrase() but this time we try to find the longest non-match
        // <Debug> Current implementation seems way too expensive
        public string GetUnknownPhrase(string sequence)
        {
            // The logic is that from any specific location we check if there is a match
            // Check which ones DO NOT match and store the longest one
            string longestNoMatch = null;
            for (int i = 1; i <= sequence.Length; i++)
            {
                string potentialMatch = sequence.Substring(i-1);
                Phrase match = GetPhrase(potentialMatch);
                if (match != null) break;
                longestNoMatch = sequence.Substring(0, i);
            }
            if (longestNoMatch != null) longestNoMatch = longestNoMatch.Trim();

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
            // Not assume words and space delimiters, just count characters, e.g. not assuming space or punctuations

            // Method 1
            // Stage 1: Get all of the phrases with the beginning word as the first word
            // Stage 2: Iterate through remaining words in words array and find a best match
            // Remark: That wasn't efficient considering our current dictionary implementation of phrases storage, and iterating through all takes too much effort

            // Method 2
            // Since there are only limited amount of words input, there are only limited amount of potential matches; Check which ones match and store the longest one
            sequence = sequence.Trim();
            Phrase longestMatch = null;
            for (int i = 1; i <= sequence.Length; i++)
            {
                string potentialMatch = sequence.Substring(0, i);
                Phrase match = ContainsPhrase(potentialMatch);
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
    /// An important method to help organize and define concepts; Used to group phrases' meaning together and also used in defining meanings for certain actions (either certain actions only operate on certain categories of phrases, or meaning vary depending on operators)
    /// </summary>
    class Category
    {
        // Constructor and Modification methods
        Category(string name, Category parent = null, Category[] subCategories = null)
        {
            Name = name;
            throw new NotImplementedException();
        }
        Category(string name, string[] items)
        {
            throw new NotImplementedException();
        }
        Category(string name, Phrase[] items)
        {
            throw new NotImplementedException();
        }
        public void SetParent(Category parent)
        {
            throw new NotImplementedException();
        }
        public void AddItem(Phrase item)
        {
            // Check error
            throw new NotImplementedException();
        }
        public void AddSubCategory(Category sub)
        {
            // Check error
            throw new NotImplementedException();
        }

        // Identifier
        // For clarify when designing such, we use "Category+Name" in the vocabulary file to differentiate a symbol from its abstract concept, but actual concept of the category is better termed "Category" only
        public string Name { get; }    // Here the string DOES NOT contain "Name"
        public Category parentCategory { get; }

        // Member: either one of the below can exist
        public List<Phrase> Items { get; } // Phrase instances that belongs to this category
        public List<Category> SubCategories { get; }

        // Attributes: Linguistic and conceptual operations on the members of the Category; Those attribtues are inherited downward to subcategories
        // All strings referenced below should be well-defined phrases in VocabularyManager
        /* Special Notice */public List<Phrase> Properties { get; set; } // Defines potential properties, i.e. ownership relations that can possibly be assigned to members of this category
        public List<Phrase> BeStates { get; set; }    // Defines potential states that are grammartically adjective, i.e. state relations that can be attribtued to members of this category
        public List<Phrase> DoStates { get; set; }    // Defines potential states that are grammartically adverb, i.e. state relations that can be attribtued to members of this category
        public List<Phrase> Actions { get; set; }   // Defines potential actions, i.e. the behaviors that members of this category can do; Notice this is an ABSTRACT companion for ABSTRACT meaning/definitions of verbs - i.e. an approach to define verbs through its available operations rather than its physical perceptions (related remark see class ConceptAction), and as such requires carefully tailored training material
        // Notice that all the BeStates, DoStates and Actions as defined by category of objects (if exist) inside Properties are all considered that of items in this category and subcategories as well, with a condition as mentioned below
        // To summarize: 1. All above four are inherited by subcategories; 2. BeStates, DoStates and Actions of the categories of objects in Properties are also considered that of items in current category, and inherited by subcategories of current category, IF AND ONLY IF that property is considered a REPRESENTATIVE PROPERTY of the given object instance (thus chairs and desks do not represent a person, but a person's skin and temper do); Representative Properties are strictly designer specified and do not change by information
    }

    /// <summary>
    /// The smallest vocabulary unit; Record only directly useful information; Metadata are appended in other facilities e.g. Memory module
    /// There is no fundamental difference between a phrase and a word since in reality a phrase can be interpreted as a completely new word despite the fact that its componetns might have individual meaning
    /// Its considered the longer phrase whenever components occur in order, e.g. object oriented programming is considered as one phrase, not three phrases (words)
    /// </summary>
    class Phrase
    {
        /// <summary>
        /// Provides definitions for the phrase
        /// </summary>
        public class PhraseDefinition
        {
            // Constructor
            public PhraseDefinition(WordAttribute attribute, List<Phrase> forms, List<Phrase> synonyms, List<Phrase> opposites, string definition, string abs)
            {
                // Initialize members
                Attribute = attribute;
                Forms = forms;
                Synonyms = synonyms;
                Opposites = opposites;
                Definition = definition;
                Abstract = abs;
            }
            // Common members for pattern recognition and basic processing
            public WordAttribute Attribute { get; }
            public List<Phrase> Forms { get; }
            public List<Phrase> Synonyms { get; }
            public List<Phrase> Opposites { get; }
            // Auxiliary members for book-keeping purpose
            string Definition = null;
            string Abstract = null;
            // Advanced members for comprehension and speech generation
            public List<Pattern> Usages { get; }    // Remark see VobularySheet header; Designer specified
            public List<Phrase> Decorations { get; }    // Remark see VobularySheet header; Dynamic growing while learning
        }

        public Phrase(string value, Phrase origin = null)
        {
            // Set key
            Key = value;

            // Initialize Definitions list
            Definitions = new List<PhraseDefinition>();

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

        // Notice difference definitions might share the same WordAttribute, e.g. different meanings/senses of the same noun
        public void AddDefinition(WordAttribute attribute, List<Phrase> forms, List<Phrase> synonyms, List<Phrase> opposites, string definition, string abs)
        {
            Definitions.Add(new PhraseDefinition(attribute, forms, synonyms, opposites, definition, abs));
        }

        // ---------------------- Properties ---------------------- //
        public Phrase OriginalForm = null;  // Indicates whether current phrase is a varying form of orignal, for original this should be set to self
        public string Key { get; } // Phrase face
        public int WordCount { get { return Key.Count(c => c == ' ') + 1; } }
        private List<PhraseDefinition> Definitions;

        // ---------------------- Access Interface ---------------------- //
        public bool IsDefined { get { return Definitions.Count != 0; } } // False indicates an uninitialized but recognized phrase (e.g. during dictionary intiailization phase there are phrase that depend on other phrases which haven't been initialized yet, in this case we add them to vocabulary list first then initialize them later)
        public WordAttribute? Attribute // The characteristic property of the word, can contain more than one value
        {
            get
            {
                if(IsDefined)
                {
                    WordAttribute temp = Definitions[0].Attribute;
                    for (int i = 1; i < Definitions.Count; i++)
                    {
                        temp |= Definitions[i].Attribute;
                    }
                    return temp;
                }
                else
                {
                    return null;
                }
            }
        }
        // Usage: e.g. (Attribute&WordAttribute.Verb == WordAttribute.Verb)

        // The below have value only when OriginalForm == null
        public List<Phrase> Forms // Other forms that represent the same word maybe in different tense
        {
            get
            {
                List<Phrase> temp = new List<Phrase>();
                foreach (PhraseDefinition definition in Definitions)
                {
                    if(definition.Forms != null)
                        temp.AddRange(definition.Forms);
                }
                return temp;
            }
        }
        public List<Phrase> Synonyms    // Words with similar meaning, might not contain their complete variable forms (but can be derefenced further using Phrase)
        {
            get
            {
                List<Phrase> temp = new List<Phrase>();
                foreach (PhraseDefinition definition in Definitions)
                {
                    if (definition.Synonyms != null)
                        temp.AddRange(definition.Synonyms);
                }
                return temp;
            }
        }    
        public List<Phrase> Opposites   // (Not used for now, might prove useful when deducting meanings) Words with opposite meaning, might not contain their complete variable forms
        {
            get
            {
                List<Phrase> temp = new List<Phrase>();
                foreach (PhraseDefinition definition in Definitions)
                {
                    if (definition.Opposites != null)
                        temp.AddRange(definition.Opposites);
                }
                return temp;
            }
        }
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
        /// Don't use 0 for comparison needs that for failed comparison
        //// Nouns
        // Summary:
        //     Can be used as a verb
        verb = 1,
        // Summary:
        //     Can be used as a noun
        noun = 2,
        // Summary:
        //     Can be used as adjective
        adj = 4,
        // Summary:
        //     Can be used as adverb
        adv = 8,
        // Summary:
        //     Can be used as pronoun
        pron = 16,
        // Summary:
        //     Can be used as conjunction
        conj = 32,
        // Summary:
        //     Can be used as determiner (e.g. what, some)
        det = 64,
        // Summary:
        //     Can be used as a predeterminer (e.g. what, all, both)
        pred = 128,
        // Summary:
        //     Can be used as article (e.g. the, a, an)
        art = 256,
        // Summary:
        //     Can be used as proposition (e.g. except, see longman dictonary on "except" for more details)
        prep = 512,

        //// Verbs
        // Summary:
        //     Is used as Modal verb
        modal = 1024,
        // Summary:
        //     Can be used transitive
        transitive = 2048,
        // Summary:
        //     Can be used intransitive
        intransitive = 4096,
        // Summary:
        //     Is Simple Present
        simple = 8192,
        // Summary:
        //     Is Present Progressive
        present = 16384,
        // Summary:
        //     Is present particle form
        // http://grammar.about.com/od/basicsentencegrammar/f/progpartdiff.htm
        gerund = 32768,  
        // Summary:
        //     Is Simple Past
        past = 65536,
        // Summary:
        //     Is Present Perfect
        perfect = 131072,
        // Summary:
        //     Wild match for any; This isn't specified in dicitonary but used only manually
        any = 1073741824 // The 31th bit
    }
}
