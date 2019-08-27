/// <initiation>
/// The central algorithm and original inspiration for the system is SIS - NoteTaking System - PC Magic Browser's QuickMatch® function.
///     Highly customized and intelligent fuzzy logic (not in any sense current modern AI approach) is the core to this implementation.
/// </initiation>

/// <naming_convention>
/// 1. Class member names using upper-case
/// 2. Local Variables use lower-class
/// 3. Functions begin with verb
/// 4. For programming purpose <> using xml style lower case letters with _ as connector, for personal discussion we use <> with upper cased first letter and normal spaces (without a closing tag
/// </naming_convention>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Airi.TheSystem.Memory;
using Airi.TheSystem.Instruction;
using System.IO;
using System.Text.RegularExpressions;
using Airi.TheSystem.Syntax;
using System.Threading;

/* This namespace is reponsible for handling learning and generating memory for Airi-The System Core;
 * This namespace is also the interaction interface for querying and communication with Airi-The System.
 * 
 * Currently the system only deals with English for its clear boundary and easy processing; Can be easily converted to handle Chinese words and even English phrases
 *  but current no efficinet fuzzy matching algorithms exist yet (except O(n) iteration pattern match.
 * */
namespace Airi.TheSystem
{
    /// <summary>
    /// TheSystem class provides interaction interface from outside world with Airi.TheSystem
    /// </summary>
    public class TheSystem
    {
        private BaseMemory AiriMemory;  // Global point to main memory
        private static readonly string MemoryFileName = "Airi.memory";    // File to store cached memory

        // Do initialization and load from previous memory if needed
        public TheSystem(bool bRestore = false)
        {
            // Initialize Memory
            if (bRestore == true)
            {
                // Check and load memory file
                string memoryFileLocation = AppDomain.CurrentDomain.BaseDirectory + MemoryFileName;
                if(File.Exists(memoryFileLocation))
                {
                    // Generate memory from file
                    AiriMemory = new BaseMemory(memoryFileLocation);
                }
                else
                {
                    // Otherwise raise an exception
                    throw new MemoryFileNotExistException("Memory File Not Found.");
                }
            }
            else
            {
                // Other wise we don't need to do any special initialization
                AiriMemory = new BaseMemory();
            }

        }

        // Use only internal memory to analysis and respond to given speech input
        // Expecting well formatd input
        // <Debug> This function isn't complete yet and currently only deplyed for debug purpose
        // <Development> In the fucture this fuction will be integrated as part of original Speak() function and change its access level from public to private
        public List<string> SpeakNative(string input, string instigator, string themeContext = null)
        {
            // Do preprocessing on inputs to suite better to our conceptual model (e.g. Pronoun interpretation)
            // ... 

            // Do first order accessor/interrupter handling events
            // ... 

            // (Option) Emotional State Filtering: short path thinking (Do it here or below)
            // ... 

            // Do memory and conceptual construction etc. operations
            // ...

            // Recognize pattern and do pattern specific response
            PatternInstance instance = AiriMemory.Recognize(input);
            //Instruction.Action action = instance.Type.PatternAction;
            //List<string> response = action.Execute(instance);
            //return response;

            // Comprehension
            // ... NExt

            // Organize generated output, substitution proper pronouns
            // ... 

            // Coutersy decorations
            // ...

            // (Option) Emotional State Filtering (Do it here or above)
            // ... 

            return null;
        }

        // [Conditional("DEBUG")]
#if DEBUG
        public void SpeakNativeUnitTest()
        {
            AiriMemory.RecognizeUnitTest();
        }
#endif

        // Given a well formated input (currently we have no special format specification except that we require the sentence itself isn't enclosed in double quotes
        //  otherwise it might indicate a quote, not something speaker says
        // - Returns a list of strings representing sentences to reply in turn
        // - At least one reply is guaranteed to be generated
        // - Due to the nature of this function it might take considerable amount of time to generate a reply so call in a seperate thread
        // - When well indexed, we already know which sentences(and their related themes) words in an input sentences matches to. 
        // - Sentences do not repeat and given any sentence it's occurrence inside a theme is its index in theme's list.
        // @Instigator: who we are currently talking to, should ideally be constant across a session
        // @themeContext: Use this to specify a specific context where conversation should follow, i.e. define a subject/preset of possible communication schemes
        // public async Task<List<string>> Speak(string input, string instigator)
        public List<string> Speak(string input, string instigator, string themeContext = null)
        {
            // Stage 1: Break input into meaningful words
            List<string> words = new List<string>();    // This is for ease of organization, we might speed things up a bit by not generating a new list
            string[] tempWords = input.Split(new char[] { ' ', ',', '.', '?', '!' });
            foreach (string word in tempWords)
            {
                if (word != "")  // In case of comma seperated
                {
                    words.Add(word);
                }
            }

            // Stage 2: Accumulating points of similarity: For each word, find all its related sentences; Then for each sentence evaluate how involved it is with other words
            List<Sentence> RelatedSentences = new List<Sentence>();
            foreach (string word in words)
            {
                // If the word exst, then collect all its related sentences into the bigger list; If not exist, that is perfectly normal
                // <Improvement> In the future we might want to pay special attention to those new vocabulary that we don't know yet in our memory 
                if(AiriMemory.Words.ContainsKey(word))
                {
                    foreach (Sentence s in AiriMemory.Words[word].RelatedSentences)
                    {
                        RelatedSentences.Add(s);
                        s.MatchMarks = 0;
                    }
                }
            }

            // Stage 3: Do a similarity marking
            // <Improvement> Can be more intelligent
            foreach (Sentence sentence in RelatedSentences)
            {
                foreach (string word in words)
                {
                    if (sentence.Content.Contains(word))
                    {
                        sentence.MatchMarks++;
                    }
                }
            }

            // Stage 4: Order and find a best match 
            // <Improvement> Can be more intelligent
            Sentence bestMatch = (RelatedSentences.OrderByDescending(x => x.MatchMarks).ToList())[0];   // <Debug> This raises an exception
            // http://stackoverflow.com/questions/3801748/select-method-in-listt-collection
            List<Sentence> allMatches = RelatedSentences.Where(s => s.MatchMarks == bestMatch.MatchMarks).ToList();
            Random rnd = new Random();
            bestMatch = allMatches[rnd.Next(0, allMatches.Count)];
            // http://stackoverflow.com/questions/2706500/how-do-i-generate-a-random-int-number-in-c

            // Stage 5: Fetch a proper reply
            // Current implementation we are just using next available line in theme
            int index = bestMatch.Theme.Sentences.IndexOf(bestMatch);   // Notice the difference between IndexOf and FindIndex: http://stackoverflow.com/questions/6481823/find-index-of-an-int-in-a-list, https://msdn.microsoft.com/en-us/library/x1xzf2ca(v=vs.100).aspx
            string reply;
            if(bestMatch.Theme.Sentences[index+1] != null)
            {
                reply = bestMatch.Theme.Sentences[index + 1].Content;   // <Debug> Pending cleaning, some sentences are not well defined
            }
            else
            {
                reply = "Well that makes me think";
            }

            // Stage 6: Potentially generate a smooth, fluid multi-conversation
            List<string> speeches = new List<string>();
            speeches.Add(reply);

            // Stage 7: Also learn from the input for next time communication
            if(AiriMemory.Themes.ContainsKey(instigator))
            {
                // Learn, appending to existing theme
                LearnSentence(input, AiriMemory.Themes[instigator]);
            }
            else
            {
                // Create a theme and then learn
                AiriMemory.Themes[instigator] = new Theme(instigator);
                LearnSentence(input, AiriMemory.Themes[instigator]);
            }

            return speeches;
        }

        // Learn from a given text dialog: We accept any sort of written work, as is, without requiring modification from designer
        // A single file represents a theme in this case
        // Call this function multiple times to learn different files
        public void Learn(string filePath)
        {
            if(File.Exists(filePath))
            {
                string theme = Path.GetFileNameWithoutExtension(filePath);
                string content = File.ReadAllText(filePath);
                LearnTheme(content, theme);
            }
        }

        // Provides learning from a designer specified specific theme
        // @dialogContent 
        //  - Should be a loaded complete file, 
        //  - Doesn't need to be modified in anyway just make sure it is natural and follows normal usage
        //  - Require UTF8 encoding
        // @Summary: Ojbects manipulates in this process
        //  - AiriMemory: Its words and themes
        //  - New Themes: Its sentences
        //  - New Words: Its relation to sentences
        private void LearnTheme(string dialogContent, string themeName)
        {
            // Stage 1: Do some preprocessing to discard information that we don't want
            // Seperate all contents into lines
            string[] lines = dialogContent.Split(new char[] { '\n', '\r', '{', '}', '[', ']'});
            // Generate a new theme
            Theme newTheme = new Theme(themeName);
            AiriMemory.Themes[themeName] = newTheme;

            // Stage 2: Extract useful lines
            foreach (string line in lines)
            {
                // Dispose useless lines: 
                //  contain nothing, 
                //  contain not commonly used special symbols, contain CHN(probably some online resource link; actually this is fine, but we dispose all that contains more than latin and punctuation)
                //      i.e. Anything not a letter, number, (all purpose)puncuation, space
                //  contain "www", 
                //  contain no (recognizable)punctuations (e.g. "Chapter One")
                //      Recognized puctuations: .!,?"“” (Notice '-' isn't considered a punctuation)
                if (line == "" || Regex.Matches(line, @"[^\p{L}\p{N}\p{P}\p{Z}]").Count > 0 || line.Contains("www") || Regex.Matches(line, @"[.!?,""“”]").Count == 0
                    || Regex.Matches(line, @"[:]").Count > 0)    // Another would be to check char.IsLetter
                    continue;

                /* Regex reference:
                 * https://msdn.microsoft.com/en-us/library/20bw873z(v=vs.110).aspx#CategoryOrBlock
                 * https://msdn.microsoft.com/en-us/library/20bw873z(v=vs.110).aspx#SupportedUnicodeGeneralCategories
                 * 
                 *  Pending study:
                 *  http://stackoverflow.com/questions/1565847/declaring-a-looooong-single-line-string-in-c-sharp
                 */

                // Break a multi-sentence line into sentences; Pay attention to abbreviations(e.g. Mr. -- currently not handled) because they are not punctuations
                string[] sentences = line.Split(new char[] { '.', '!', '?', '"', '“', '”'});

                // Stage 3: Process sentences
                foreach (string sentence in sentences)
                {
                    LearnSentence(sentence, newTheme);
                }
            }
        }

        // Learn from a new distinct sentence and record it in a theme
        private void LearnSentence(string sentence, Theme theme)
        {
            // Add those setences to theme
            if (sentence != "")
            {
                Sentence newSentence = new Sentence(sentence, theme);
                theme.Sentences.Add(newSentence);

                // Catagorize and organize information
                // <Improvement> We might want to better collect from quoted contesnt, i.e. real dialoges in text
                // Break sentences into words
                string[] words = sentence.Split(new char[] { ' ', ',' });
                foreach (string word in words)
                {
                    if (word != "")  // In case of comma seperated
                    {
                        // Generate memory and connections
                        if (AiriMemory.Words.ContainsKey(word))
                        {
                            AiriMemory.Words[word].RelatedSentences.Add(newSentence);
                        }
                        else
                        {
                            Word newWord = new Word(word);
                            AiriMemory.Words[word] = newWord;
                            newWord.RelatedSentences.Add(newSentence);
                        }
                    }
                }
            }
        }

        // Read and comprehend a story
        public void ReadStory(string storyContent)
        {
            throw new NotImplementedException();
        }

        // Answer questions about the story
        public string AskStory(string v)
        {
            throw new NotImplementedException();
        }
    }
}
