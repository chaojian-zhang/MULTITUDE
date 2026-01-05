using Airi.TheSystem.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Airi.TheSystem.Memory
{
    /// <summary>
    /// Defines base memory structure for TheSystem
    ///     Provides textual representation of current deployed memory structure (currently we use JSON and plain text files for this purpose
    ///     Defines interface for learning and storing and accessing memory
    /// </summary>
    internal class BaseMemory
    {
        // Total collection of elements currently in memory
        public Dictionary<string, Word> Words { get; set; } // Words refer to sentences
        public Dictionary<string, Theme> Themes { get; set; }  // Themes contain sentences
        public PatternMemory Patterns { get; set; }

        // Construct from memory file
        public BaseMemory(string MemoryFilePath)
        {
            // Initialize Members
            Words = new Dictionary<string, Word>();
            Themes = new Dictionary<string, Theme>();
            Patterns = new PatternMemory();

            throw new NotImplementedException();
        }
        // Default Constructor
        public BaseMemory()
        {
            // Initialize Members
            Words = new Dictionary<string, Word>();
            Themes = new Dictionary<string, Theme>();
            Patterns = new PatternMemory();
        }

        // Given any input string, recognize its pattern and provide an Action respond
        // During a recognization process many things may happen including fundamental learning and conceptual construction process of Airi's internal memory
        public PatternInstance Recognize(string sentence)
        {
            // Do other stuff
            // ...

            // Recognize pattern
            List<PatternInstance> patterns = Patterns.FindMatchPatterns(sentence);
            // There are three ways to handle non-match with existing patterns
            // 1. Begin with next character and procceed to see if there is any trialing matches
            // 2. Use partial matches from above process
            // 3. Use genenric conceptual methods (pending devising)

            // Filtering patterns
            // ...

            // <Debug> Iterate through patterns and see what we have gotten
            for (int i = 0; i < patterns.Count; i++)
            {
                System.Console.Write(i+1 + ": ");
                patterns[i].ShowAnalysis();
            }

            // Do other stuff
            // ...

            // <On processing of speeches in terms of memory>
            // 1. Airi's memory of speechs are as is, for information can just be remembered as is, with all its details; The effect on concept though, is represented as its effect on new constructions of links and concepts in a more fundamental "base understanding", i.e. the BaseMemory and maybe Vocabulary components
            // 2. "The effectiveness of a piece of information lies in our understanding of it", i.e. the association we have made with it, see Concept.cs, the definition of Comprehension; In otherwords, a piece of information itself is just a string of text without any power, UNLESS, it's associated with our conceptual recognition (from text map to objects, and from objects we recognize texts) and our behaviors (if any, implied by the information itself, not necessarily the sentence itself but the overall descritpion -- I assert, some explicit mentioning of actions must be made for an information to be behavior-wise influential, otherwise it will completely depend on reader's logical capability and creativity to apply the information in his/her own actions), that, is the essence of memory. (Example: that is why a sentence like "Tom ate an apple" really doesn't mean anything despite the fact that it describes an event - the listener's behavior to this sentence is completely random (e.g. they can ask questions or express their feelings) in a sense that not implied by the sentence itself)
            // 3. In a sentence, only several elements are specific, or non are specific at all - in later case we remember it as is and assume nothing about it; in former case it's a clue as to which block in our memory pool we can associate the information with; Otherwise, the sentence is associated with a general concept. Both of which, we call "subjects". The key here is "general concept" means "marine life" is the same as "sea creatures" in so far as the former CONTAINS the later - and that is defined explicitly in our knowledge, e.g. in terms of 

            return null;
        }

#if DEBUG
        internal void RecognizeUnitTest()
        {
            RecognizeUnitTestHelper("is", new string[] { "Be"}); // "Be"
            RecognizeUnitTestHelper("have lunch",new string[] { "Verb phrase"});   // "Verb phrase"
            RecognizeUnitTestHelper("do sport",new string[] { "Verb phrase"});   // "Verb phrase"
            RecognizeUnitTestHelper("in",new string[] { "Spatial prep" });   // "Spatial prep"
            RecognizeUnitTestHelper("deliciously cooked dinner",new string[] { "Descriptive component"});   // "Descriptive component"
            RecognizeUnitTestHelper("apple is fruit",new string[] { "is construct"});   // " is Construct"
            RecognizeUnitTestHelper("carpet is red", new string[] { "is construct", "state construct" });   // "state Construct"
            RecognizeUnitTestHelper("computer on desk",new string[] { "on construct" });   // "on Construct"
            RecognizeUnitTestHelper("friend of me",new string[] { "of construct"});   // "of Construct"
            RecognizeUnitTestHelper("London is west to Toronto",new string[] { "Direction construct"});   // "Direction Construct"
            RecognizeUnitTestHelper("What is happiness",new string[] { "Curiosity quest certain"});   // "Curiosity Quest Certain"
            RecognizeUnitTestHelper("What does computer aided design mean?",new string[] { "", ""});   // "Curiosity Quest Uncertain"
            RecognizeUnitTestHelper("Hi",new string[] { "Descriptive component", "Interruption 1", "Courtesy interrupt" });   // "Interruption 1"
            RecognizeUnitTestHelper("Excuse me",new string[] { "Verb phrase", "Interruption 2", "Courtesy interrupt" });   // "Interruption 2"
            RecognizeUnitTestHelper("Sorry to bother you",new string[] { "Interruption 3", "Courtesy interrupt" });   // "Interruption 3" // "Courtesy Interrupt"
            RecognizeUnitTestHelper("Evening",new string[] { "Descriptive component","Time" });   // "Time"
            RecognizeUnitTestHelper("Good Night",new string[] { "Descriptive component","Greeting" });   // "Greeting"
            RecognizeUnitTestHelper("迷迭香 is mysterious spice",new string[] { "Definition" });   // "Definition"
            RecognizeUnitTestHelper("From now on, Knife means mission", new string[] { "Advanced language concept"});   // "Advanced language concept"
            RecognizeUnitTestHelper("Do you like me?",new string[] { "Ask experience" });   // "Ask Experience"
            RecognizeUnitTestHelper("Excuse me, where is London",new string[] { "Location query" });   // "Location Query"
            RecognizeUnitTestHelper("How is weather",new string[] { "Weather query" });   // "Weather Query"
            RecognizeUnitTestHelper("Airi, play Music",new string[] { "","" });   // "Airi Run Command"
            RecognizeUnitTestHelper("Why he escaped from school is what we do not know yet",new string[] { "从句表达句" });   // "从句表达句"
            RecognizeUnitTestHelper("Send email to charles@totalimagine.com", new string[] { "Send email pattern test" });   // "Send email pattern test"
            // RecognizeUnitTestHelper("",new string[] { "","" });   // ""
        }
#endif

#if DEBUG
        internal bool RecognizeUnitTestHelper(string sentence, string[] expectedPatterns)
        {
            // Recognize pattern
            List<PatternInstance> patterns = Patterns.FindMatchPatterns(sentence);

            if (patterns.Count == 0)
            {
                System.Console.WriteLine("[Error]\"" + sentence + "\": no matching pattern");
                DebugPrintSentenceBasicStatistics(sentence);
                return false;
            }

            // Compare with expectations
            for (int i = 0; i < patterns.Count; i++)
            {
                if (patterns[i].Type.Name != expectedPatterns[i])
                {
                    System.Console.WriteLine("[Error]\"" + sentence + "\": failed at " + patterns[i].Type.Name);
                    DebugPrintSentenceBasicStatistics(sentence);
                    return false;
                }
            }

            // System.Console.WriteLine("[Success]\"" + sentence + "\": " + string.Join(", ", expectedPatterns));
            return true;
        }
#endif

#if DEBUG
        // Print information about each word in the sentence to aid in debug
        private void DebugPrintSentenceBasicStatistics(string sentence)
        {
            // Print information about phrases in the word, mostly whether they exist or their POS
            // throw new NotImplementedException();
        }
#endif

        // Export current memory into human readable form
        static public void ExportMemory()
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Defines Layer 1 memory structure for TheSystem: Words
    ///     Words have clearly seperated boundary in English by the following seperators: space, punctuations symbols, new line
    ///     Words have their usage cases recorded
    ///     Words DO NOT DISTINGUISH cases
    ///     Words DO distinguish tenses and verbal forms - this is currently an implementation shortage, theoractically speaking in current model this shouldn't be the case
    ///     Words are stored as a dictionary for access efficiency
    ///     Words usages are stored as a list for iteration purpose
    /// </summary>
    internal class Word
    {
        public Word(string Name)
        {
            this.Name = Name;
            RelatedSentences = new List<Sentence>();
        }

        // <Concept> Name of the word, case undistinguished; in the future it shall be tense etc. undistinguished as well, no suitable structure proposed for that yet so here we are just using a string
        // <Convention> Stored in lower case for readability (because most characters in an English word is writted in lower case)
        public string Name { get; set; }    // given a name it may not have a corresponding dictionary item
        // public Phrase Phrase { get; set; }  // Corresponding dictionary item, can be null if Name doesn't correspond one; Accessed directly using Dictionaries hash

        // <Concept> Usage Cases, used to identify how and when this word shall be used
        public List<Sentence> RelatedSentences { get; set; }
        public Conception.Concept PerceptionConcept { get; set; }   // Direct perception concept mapping
        public Conception.Concept MemoryConcept { get; set; }   // Symbolic memory
        public List<Conception.Concept> Compounds { get; set; } // Compounds, themes, processes etc.
    }

    /// <summary>
    /// Defines Layer 2 memory structure for TheSystem: Sentences
    ///     Setences have clearly seperated boundary in English by the following seperators: quotation marks, punctuations symbols, new line
    ///     Sentences implicitly have words associated with them and represent usage cases
    ///     Sentences belong to a theme
    ///     Sentences can embed instructions and meta-accessors
    ///     Sentences do somehow correlate to some concepts, but not through themselves, but through their matched patterns or their contained words
    /// </summary>
    internal class Sentence
    {
        // Sentence Constructors
        // @Content
        //  - Expected to be a complete sentence with punctuation, without quotation mark, and contain only one sentence
        public Sentence(string Content, Theme Theme)
        {
            this.Content = Content;
            this.Theme = Theme;
        }

        public string Content { get; set; }
        public Theme Theme { get; set; }

        public string Intercept(Context context)
        {
            throw new NotImplementedException();
        }

        public string Interpret(Context context)
        {
            throw new NotImplementedException();
        }

        // Helper
        public int MatchMarks { get; set; }
    }

    /// <summary>
    /// Defines Layer 3 memory structure for TheSystem: Themes
    ///     Themes are identified by designers or assigned simply from seperate learning materials
    ///     Themes define higher level logical pattern for sentences
    ///     Themes define what we call "explicit memory" in daily language
    ///     A well-organized theme can be used to define a specific set of conventional dialogs or be used to define a context, both of which are designer specified
    /// </summary>
    internal class Theme
    {
        // Construtor with Name as input
        public Theme(string Name)
        {
            this.Name = Name;
            Sentences = new List<Sentence>();
        }

        // A designer specified summary name for the theme/plot/pattern
        public string Name { get; set; }

        // An ordered list of all sentences representing conversation dialogs
        public List<Sentence> Sentences { get; set; }

        // A Theme naturally maps to a concept
        Conception.Concept Concept; // Direct mapping to Concept.Theme
    }
}
