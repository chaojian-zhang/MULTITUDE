using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WordNetLibrary;

// Coding schedule: Checkout our researched new libraries first before attemping to merging previous contents using WordNet, e.g. we might use other lirbaries or even other languages (e.g. Python) to do this

// Extra knowledge from a real dictionary and output in a format that Airi understands
// <Development> For practical usage we are considering developing an interface for providing definitions for all encoutnered words manually because that's the only way for MC's objective understanding for Airi
namespace Airi.TheSystem.DictionaryBuilder
{
    /// <summary>
    /// This class generates Airi's DictionarySheet from a variety of sources
    /// - Sources: WN (pending deprecation: non-natural, not complete in terms of pos, syn definitions are ambiguous), 
    ///     English dictionary(OPTED, with reference to Oxford English Dictionary and Longman Dict and MC definitions), 
    ///     and manual editing effort (Custom conceptual dictionary might be needed)
    /// - For practical reasons, we don't include Wikipedia/Wikidict contents blindly, but might consider providing some category of phrases as "Extension" vocabulary
    ///     By all means do not provide abstract in memory; Instead, extract abstracts whenever needed from disk
    /// - WikiDict and Wikipedia knowledge, as Extracted from Dump will be provided as real-time online/offline access
    /// - Status: Currently we are providing content for def, POS, syn from WordNet only
    /// </summary>
    internal class Dictionary
    {
        private static readonly string Header = 
@"# Encoded in UTF8
# Vocabularies containing POS, Synonym relationships, wiki abstract, AND a formal english dictionary definition
# Airi should be able to learn new vocabular from formal english dictionary definition
# This file is an effort of program-generated dump from WordNet(or other python/C# NLP libraries) and manual editing (for cleaning up and adding new items)
# Each word shall be defined once; Each sense of the same word is effectively a new word
# The goal of this vocabular sheet is both as a wiki abstract, a data sheet, and a functional dictionary
# Format: 
#	- Line ending with \r or \n or \r\n
#	- Use # at the beginning of line to in dicate comments for the rest of line; No space shall be allowed before # symbol
#	- New words/phrases at the beginning of line after [Word]
#	- Each sense of the word is defined by a FORMAL ENGLISH DICTIONARY DEFINITION, immediately after [Definition] and <attribute>, it should be a single line of statements consisting of one or more sentences
#   - Different forms of the same word (which indicate small tone change or serve other semantic functions) are all listed after [Form] after [Definition] seperated by commas, dependent of definition or POS
#	- Synonyms follow after [Synonym], immediately (no space) followed by a comma delimited list of other words(leading space allowed for readability); Each synnonym corresponds to and differ by each definition
#	- Abstract of the word (used for technical terms) follow [Abstract], no extra new lines are allowed; This is a longer version of definition
#	- Order of above elements are not strict but recommeded ordering as described here; Each word shall be seperated by a blank line; Each definition of word shall be seperated by a commented ------- for readaibility; Leading spaces of lines are always ignored
#	- Unrecognized [Element] will be ignored with a warning, but not causing any side effect; Use this for documentation purpose is needed
#	- Words listed from A-Z in ascending order
#   - All words should appear lower cased

# Advanced (Non-WN, Designer Specified, Airi Style): Might consider them dynamically expandable so Airi can learn from well-selected materials instead of manually expand
#   - A definition can have [Usage] elements that defines specifically what category of objects and what other connectives to use, e.g. see [Word]behave and [Word]use and [Word]above; each usage is one seperate definition; Patterns are used to define this, with all pattern elements allowed; Notice some of the more general uses are already mentioned in DefaultPatternDefinition (e.g. adv followed by adj, or prep + noun), here the Usage provides more specific cases, in some cases it can be phrases that doesn't strictly follow grammar; Each such usage (conceptually without perception) completely defines the meaning of a verb (thus the simplest way to provide an understanding of all possible sentences is to provide such detailed usages of all verbs)
#   - A noun definition or an adj or a verb definition can have [Decorations] elements (dynamic growing) that defines what kind of adj (for noun), adv (for adj), or adv (for verb) can be used with them; This is pretty much like Properties but for specific phrases; Notice for adj and for adv they do not record specifically what nouns and verbs they can be used for (thuse we are object or action oriented, rather than decoration oriented while defining this)
#   - Notice for verbs, object and adverbs used in fron and after have different meanings and can be of different category, e.g. normally behave well vs behave normally, and Tom eats apple vs apple eats Tom -- but the difference isn't within the verb or grammar, but within the objects/adverbs themselves (i.e. they contain different properties and convey different meaning)

# (Not scheduled to be implemented but arranged for design) Advanced: Short-path thinking and character definition
#   - We can associate emotions (physical impulses) with certain adj. verb. adv.+verb. combination and noun., such that we establish characters; The above implementation is a raionale one and doesn't take these into account, thus even empathy will be implemented in above it's purely psychologically technical (as a form of Airi's understanding and technical behavior) rather than personal (as a form of impulsive actions)

# Example
# [Word]book
#	[Definition]<noun>A set of printed pages that are held together so that you can read them.
#	[Form]books
#	[Synonym]printed pages
#	[Abstract]A book is a set of sheets of paper, parchment, or similar materials that are fastened together to hinge at one side. A single sheet within a book is a leaf, and each side of a leaf is a page. Writing or images can be printed or drawn on a book's pages. An electronic image that is formatted to resemble a book on a computer screen, smartphone or e-reader device is known as an electronic book or e-book.
#	# ----------
#	[Definition]<verb>To make arrangements to stay in a place, eat in a restaurant, go to a theatre etc. at a particular time in the future
#	[Form]booked, booking, books
#	[Synonym]reserve, arrange
# 
# [Word]another word here
# ...";

        static void Main(string[] args)
        {
            // Configurations
            string OutputVocabularyFile = @"H:\Projects\按项目分类 - 执行和创造用\-- Productions\SIS\Libraries\Wikipedia and Wikidict\VocabularyList\ProcessedWordsList.txt";

            // <Debug> Timing
            var watch = System.Diagnostics.Stopwatch.StartNew();

            // Initialize writer
            OutputFile = new StreamWriter(OutputVocabularyFile);
            // Output header
            OutputFile.Write(Header);

            // Part 1: Generate vocabulary dictionary
            ExtractVocabulary();
            // Part 2: Extra vocabulary indexes for practical purposes, provided as extension knowledge
            // ExtractWikiKnowledge();

            // Timing
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            System.Console.WriteLine("Parsing took: " + elapsedMs + "ms for " + nItems + " items.");

            // Close writer
            OutputFile.Close();
        }

        private static void ExtractWikiKnowledge()
        {
            /// Stage 2: Wiki Phrases - Iterate through extra information and populate phrases, mark as noun
            /// Notice enwiki-20170220-pages-articles-multistream-index seem more compact(i.e. less messy) than enwiki-latest-all-titles-in-ns0
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

            throw new NotImplementedException();
        }

        static int nItems = 0;  // <Debug> Counting how many phrases we loaded
        // Use existing resources in isolated files
        private static void ExtractVocabulary()
        {
            // <Debug> Use of absoluate locations pending removed into internal resources or project files
            // <Notice> Make sure all files encoded in UTF8
            string WordNetDictLocation = @"H:\Projects\按项目分类 - 执行和创造用\-- Productions\SIS\Libraries\Dict\Libraries In Use\WordNetDictFiles\";    // Trailing '\' is necessary
            string[] TestWordTypes = new string[] { "adj", "adv", "noun", "verb" };
            string RawWordListFile = @"H:\Projects\按项目分类 - 执行和创造用\-- Productions\SIS\Libraries\Wikipedia and Wikidict\VocabularyList\UnProcessedWordsList.txt";
            // string WikiFiles = @"H:\Projects\按项目分类 - 执行和创造用\-- Productions\SIS\Libraries\Wikipedia and Wikidict\VocabularyList\WikiTitles.txt";
            // string WiktionaryFile = @"H:\Projects\按项目分类 - 执行和创造用\-- Productions\SIS\Libraries\Wikipedia and Wikidict\VocabularyList\WiktionaryTitles.txt";
            //string WiktionaryAbstractFile = "";
            //string WikipediaAbstractFile = "";

            // Load Phrases
            // Iterate through all vocabularies and populate phrases, mark its word type accordingly
            // <Improvement> Notice specifics about verbs are not decided yet
            // <Improvement> Notice specifics about words other than noun, verb, adj and adv are not decided yet (currently all disposed)

            /// Stage One: WN Vocabulary
            /// <Improvement> Currently this stage is takes most of loading speed, use a text/binary file instead
            // Set up library
            WNCommon.path = WordNetDictLocation;

            // For each word we test to see whether they are of proper type
            foreach (string word in File.ReadLines(RawWordListFile))
            {
                nItems++;

                // Extract information
                bool bUseSeperator = false;
                for (int i = 0; i < TestWordTypes.Length; i++)
                {
                    WordType pos = WordNetLibrary.WordType.of(TestWordTypes[i]);
                    SearchSet searchSet = WordNetLibrary.WordNetDatabase.FindPossibleDefinitions(word, pos);
                    if (searchSet.NonEmpty == true)
                    {
                        if (bUseSeperator) OutputSeperator();
                        else
                        {
                            // Begin a new word only if we can find any definition
                            OutputNewWord(word);
                            // Mark using seperator next time
                            bUseSeperator = true;
                        }
                        OutputDefinition(TestWordTypes[i], "Empty");
                    }
                }
            }
        }

        // Output functions
        private static System.IO.StreamWriter OutputFile = null;
        private static void OutputNewWord(string word)
        {
            OutputFile.WriteLine("\n[Word]" + word.ToLower());
        }
        private static void OutputDefinition(string pos, string definition)
        {
            OutputFile.WriteLine("\t[Definition]<" + pos + ">" + definition);
        }
        private static void OutputForms(string[] forms)
        {
            OutputFile.Write("\t[Form]");
            for (int i = 0; i < forms.Length - 1; i++)
            {
                OutputFile.Write(forms[i] + ", ");
            }
            OutputFile.WriteLine(forms[forms.Length - 1]);
        }
        private static void OutputSyn(string[] words)
        {
            OutputFile.Write("\t[Synonym]");
            for (int i = 0; i < words.Length - 1; i++)
            {
                OutputFile.Write(words[i] + ", ");
            }
            OutputFile.WriteLine(words[words.Length - 1]);
        }
        private static void OutputAbstract(string content)
        {
            OutputFile.WriteLine("\t[Abstract]" + content);
        }
        private static void OutputSeperator()
        {
            OutputFile.WriteLine("\t# ----------");
        }
    }
}
