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

            // Filtering patterns
            // ...

            // <Debug> Iterate through patterns and see what we have gotten
            foreach (PatternInstance pattern in patterns)
            {
                pattern.ShowAnalysis();
            }

            // Do other stuff
            // ...

            return null;
        }

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
