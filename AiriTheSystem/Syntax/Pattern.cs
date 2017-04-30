using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Airi.TheSystem.Instruction;
using System.Net.Mail;

namespace Airi.TheSystem.Syntax
{
    /// <specification>
    /// Topic: SPatternExp Format Specification
    /// Design Goal: Human readable, flxibility, efficiency, coverage
    /// @General Format
    //      - <GeneralFormat> PatternName(Required): ('SpecificWord') (~VarietyWord) (<*WordAttribute+MoreAttributeConstraints>) (Punctuation) ("SubPattern") ([Choice]) #Tag @Category --> PatternDefaultAction
    //                        Not distinguish character cases; Elements seperated by space
    ///     - <Name> Pattern Name: For reference purpose only, not learnt as part of memory; This descriptive string can be anything as long as doesn't contain ":" because that will be used to seperate it from actual elements; 
    ///                 All white specas are allowable
    ///                 [Pending] Use curly braces {} immediately before pattern name to indicate the pattern is valid (satisfied only) under certain context, thus causing its response actions only fired under a context
    ///     - <Element> For all below, use () to indicate optional
    ///     - <Element> SpecificWord: Enter the word as is, e.g. "speak"
    ///                 SpecificWord: Use '' for word phrases, do not contain extra white spaces after begining ' and before ending '
    ///     - <Element> VarietyWord: Enter any one of the varieties, beginning with a "~" mark, e.g. "spoken", "speak", "speaking", "speaks" all refer to the same word(s); 
    ///                 Also consider synnonyms (We do not support opposite meanings, e.g. by using ! because patterns define what will happen not what doesn't happen)
    ///     - <Element> WordAttribute: Enter enumeration name from "WordAttribute" and "VerbAttribute", enclosed in a pair of "<>"
    ///                 Use a "+" sign if contains multiple constriants, e.g. "Verb+Past", for verbs, if not specified VerbAttribute then it means always match as long as is a verb
    ///                 [Pending] Use <*> to indicate repeatition, when repetation is used only one attribute is allowed since it makes no sense for a repeatable attribute element to be of different types (for English)
    ///                 [Pending] Use <any> to match any phrase
    ///     - <Element> SubPattern: Enter "Pattern Name" for a previously defined pattern, enclosed in a pair of ""
    ///                 Can have white spaces
    ///                 [Pending] Can refer to self (to cause recursive definitions) (an ending condition must be provided in the pattern by specifying this subpattern optional)
    ///     - <Element> [Pending] Use @ for catagorized phrases (e.g. LocationName, PeopleName), use !@ for exclusive
    ///                 These categorization information are provided by designer along with dictionary for specific usages e.g. GIS system or game NPC or other common object identification by name
    ///     - <Element> [Pending] Use # to indicate alias/Tag for specific concepts, objects or object type (definition see below)
    ///                 These have specialized uses and are not used in common practices
    ///                 Specialized usages: (Rarely used) Identifying key objects in specific application domain; (Common usage) Identifying specialized types of string e.g. email address or other custom formats which we didn't participate in at design time but added later
    ///                 Use regular expression for matching and storage purpose (e.g. email address), or specialized functions for extract information for later use (e.g. accessors). E.g. #Email or #Accessor
    ///     - <Element> Use SPEECH (all upper case) to indicate a quotation mark encoded speech, such a speech is stored and compared as is and represent memorized quotes. E.g. in experience sheet one might say this: My father used to say, "a quote...".
    ///     - <Element> Choice: Use a pair of "[]" to enclose any one of PREVIOUSLY mentioned elements, seperated by | (we used | instead of comma because it's nicer), 
    ///                 Cannot include a secondary Choice element; Designers must be careful that choice options themselves do not overlap because during the match we return the first matched selection;
    ///                 Choice options are always optional but at least one must be selected
    ///     - <Elemenet> Punctuations can be defined in pattern definitions, we support :,.?!-
    ///     - <Element> [Pending] Use ??? for unknown phrases (by default action it will be collected as noun, or not handled)
    ///                 Notice ??? isnt' used as <any> so if the given string is indeed a phrase then we won't commit a match
    ///     - <Response> Embed common responses(i.e.actions), through adding "-->"
    ///                 Actions should ideally match related function/class name, and shouldn't contain white spaces
    ///                 [Pending] Use (Label) immediately after CreateContext or AddContext or RemoveContext action to indicate a context, e.g. --> CreateContext(GEZI)
    ///     - <Annotaion> Use // for comments till end of line; /**/ For comment blocks
    ///     - <Improvement> [Pending] Make it capable of identifying Concepts: types of actions, and types of (noun) objects, e.g. distinguish between "get sth" vs "get (to) someplace"
    ///     - <Improvement> [Pending] Expose Python templates for user implemented custom actions (Devise a better way to integrate this feature to practical use)
    ///     - <DevelopmentNote> Avoid user temptation by not allowing users to input setnences that contain pattern definitions -- well it doesn't matter; But we might still want to strip out unused user input characters (i.e. for current implementation allow only 26 english characters and basic punctuations)
    ///                 Do this in preprocessing so we don't make our pattern processing code too long.
    ///     - <DevelopmentNote> Context is a label, a flag, or a bunch of conditions: either at one time we are in only one context, or multiple context might be present; Context can be used to program pattern recognition in data driven way
    ///                 Potential of such pending further elaboration
    /// @Usage Rules
    ///     - All referenced subpatterns must have alread been defined
    ///     - A fundamentally valid pattern is one that doesn't contain subpatterns
    /// </specification> 

    /// <interpretation>
    ///     Patterns can be used to encode grammars
    /// </interpretation>

    /// <summary>
    /// Defines a way to recognize syntactical patterns inside a well-defined-sentence (i.e. a sentence that comforns to natural language formats)
    /// A pattern is used to match specific pattern elements in order, without necessarily requiring close following each other (i.e. elements can be seperated apart by auxiliary words for ambiguity)
    ///     - A pattern is matched as long as all pattern elements are present in that order, no matter how many words apart they are
    ///     - Multiple patterns can be given at the same time only the most close match will be returned
    ///     - Patterns are data driven, defined using pattern formats (pending specification; We might just use SPatternExp, instead of requiring extra object oriented ways to specify a pattern)
    ///     - When matching a pattern only "containing the pattern" is considered: patterns are not stricly bounded on edges
    /// @Explanation
    ///     - Syntactical Pattern is purely syntactical, it looks like a regex, it can interpret not only words themselves but also access their syntactical properties when matching, what's more it is possible to embed patterns inside patterns 
    /// @Implementation
    ///     - Recursively the end point is pure words, a SPatternExpression can contain below elements, separated by space, each one can be defined optional
    /// @Interface: In Memory.PatternMemory
    ///     - For any given sentence, we can find out which patterns it contains
    ///     - For any given sentence, we can check it against some pattern to see match or not
    /// </summary>
    internal class Pattern
    {
        public Pattern(string name, string action = null, PatternLocale locale = PatternLocale.English)
        {
            // Initialize members
            Name = name;
            Elements = new List<PatternElement>();
            Parent = null;
            Variations = new List<Pattern>();
            Concept = null;
            PatternAction = action;
            Locale = locale;
        }

        public List<PatternElement> Elements { get; }
        public string Name { get; set; }    // A pattern has a name

        // Book Keeping
        public Pattern Parent { get; set; } // Provides one extra layer of structure for current pattern, not necessarily used for every pattern
        public List<Pattern> Variations { get; set; }    // Provides on extra layer of interconnecting different patterns, not necessarily used for every pattern

        // A pattern naturally maps to a concept, the concept itself doesn't need to be aware of such pattern
        public Conception.Concept Concept { get; set; }
        // A pattern may optionally define a proper action (i.e. Airi's resond to this pattern) for specific (designer designed) resonses
        public string PatternAction { get; set; }

        public PatternLocale Locale { get; }

        // Interaction Interface: Return whether or not a given sentence matches given pattern: If a match is found a PatternInstance will be returned other wise null
        // Notice that patterns provide EXACT matches, this gives designers flexibility in defining general or specific matches per need/context; For subpatterns the length cannot be decided though so an option if given
        // @sentence: input sentence doesn't need to be well formed: auxiliary blanks, English or chinese, punctuations or not -- those are handled within pattern and pattern elements themselves
        // <Improvement Note> The impelmentation of this function should be language neutral and doesn't assume meaningful symbols are seperated by spaces
        public PatternInstance Match(string content, VocabularyManager vocabulary, bool bExact = false)
        {
            // Basic Formaing procesing
            content = content.Trim();

            // Match against elements
            PatternInstance instance = new PatternInstance(this);
            int currentLocation = 0;
            foreach (PatternElement element in Elements)
            {
                // Input string boundary check
                if (content.Length <= currentLocation && element.bOptional == false)
                    return null;

                // Skip spaces
                while (content[currentLocation] == ' ' && content.Length <= currentLocation) currentLocation++;

                // Continue checking
                int consumed = 0;
                PatternElementInstance eInstance = element.MatchElement(content.Substring(currentLocation), vocabulary, out consumed);  // Notice consumed only represent actual element size, not including spaces (In English)
                if (eInstance != null)
                {
                    instance.ComponentElements.Add(eInstance);
                    currentLocation += consumed;
                }
                else if(element.bOptional != true) return null;
            }
            return instance;
        }
    }

    internal enum PatternLocale
    {
        English,    // Contains spaces between individual phrases
        Chinese,    // Continuous spacing
        Japanese    // Similar to Chinese, though pending more investigation
    }

    internal enum PatternElementType
    {
        SpecificWord,   // A word as exact match, not case sensitive
        VarietyWord,    // A word that can be in different forms as long as still indicate the same word
        WordAttribute,  // Matches as long as attribute match
        SubPattern,     // Compound patterns
        Choice,         // A choice of any ONE of other kinds of elements (due to practical reasons we won't be need a choice of multiple elements)
        Tag,            // #Tag for identification of specific concept or object or object (string/information) type
        CategoryInclude,        // Name for indicating a group of phrases
        CategoryExclude,        // Name for indicating NOT a group of phrases
        Punctuation,        // Symbols for punctuations
        UnknownPhrase           // Place holder for unknown potential phrases, currently we treat this only as a noun, for more complicated unknown (e.g. a subpattern) is not necessary
    }

    /// <summary>
    /// Defines possible elements in a pattern, elements can be complex
    /// </summary>
    internal class PatternElement
    {
        // Type Specific Constructors
        public PatternElement(PatternElementType type, string key, bool optional = false)
        {
            Type = type;
            Key = key;
            bOptional = optional;
        }
        public PatternElement(Pattern pattern, bool optional = false)
        {
            Type = PatternElementType.SubPattern;
            SubPattern = pattern;
            bOptional = optional;
        }
        public PatternElement(List<PatternElement> choices, bool optional = false)
        {
            Type = PatternElementType.Choice;
            Choices = choices;
            bOptional = optional;
        }

        // Type Indicators
        public PatternElementType Type { get; set; }
        public bool bOptional { get; set; }

        // Only one of below need to filled in, depending on ElementType, provided in constructor
        public string Key { get; set; } // The key can be describing either: word, attribute (unparsed), form
        public Pattern SubPattern { get; set; }
        public List<PatternElement> Choices { get; set; }

        // Match Functions
        /// <summary>
        /// Match input sentence with current element and output relavent parts, by nature of design elements only match as long as it can recognize and doesn't require input sentence to be exact length
        /// bOptional should be checked by caller
        /// </summary>
        /// <param name="content">string to be matched from begining</param>
        /// <param name="consumed">actual number of characters consumed during the match</param>
        /// <returns>An element instance if match found, otherwise null; Caller might also want to remove trailing space</returns>
        /// <Debug> By design MatchELement doesn't consider English spacing, so caller must be cautious about that since consumed doesn't count ending spaces</Debug>
        public PatternElementInstance MatchElement(string content, VocabularyManager vocabulary, out int consumed)
        {
            switch (Type)
            {
                case PatternElementType.SpecificWord:
                    if(content.ToLower().IndexOf(Key.ToLower()) == 0)
                    {
                        consumed = Key.Length;
                        return new PatternElementInstance(Type, Key);
                    }
                    break;
                case PatternElementType.VarietyWord:
                    if(vocabulary.IsPraseVaryingFormOrSynonymUndetermined(content, Key, out consumed) == true)
                    {
                        return new PatternElementInstance(Type, content.Substring(0, consumed));
                    }
                    break;
                case PatternElementType.WordAttribute:
                    // Get attribtues to match; Attributes are guaranted to be valid at load time
                    bool bInfinite = (Key.ElementAt(0) == '*');
                    string[] attributes = content.Split(new char[] { '+', '*'});
                    WordAttribute attribute = 0;
                    foreach (string a in attributes)
                    {
                        if (string.IsNullOrWhiteSpace(a)) continue;
                        attribute |= (WordAttribute)Enum.Parse(typeof(WordAttribute), a);
                    }
                    consumed = 0;
                    // The input must be recognziable so it's gonna be a phrase of some kind
                    Phrase phrase = vocabulary.GetPhrase(content); // <Improvement> Could we be matching the shortest attribute? // <Warning> GetPhrase() trimmed, so phrase.Length might not equal actual consumed characters
                    while(phrase != null)
                    {
                        // Try match against attributes
                        if ((phrase.Attribute & attribute) == attribute || attribute == WordAttribute.any)
                        {
                            consumed += phrase.Key.Length;
                            phrase = vocabulary.GetPhrase(content.Substring(consumed));
                        }
                        if (!bInfinite) break;
                    }
                    if(consumed != 0)
                    {
                        return new PatternElementInstance(Type, content.Substring(0, consumed));    // Return that many elements as one single phrase (which by itself may not exist in the library)
                        // <Development> This can be utilzied by action handlers for learning new expressions e.g. "big shinny red juicy" apple
                    }
                    break;
                case PatternElementType.SubPattern:
                    PatternInstance subPatternInstance = SubPattern.Match(content, vocabulary);
                    if (subPatternInstance != null)
                    {
                        consumed = subPatternInstance.WordCount;
                        return new PatternElementInstance(Type, subPatternInstance);
                    }
                    break;
                case PatternElementType.Choice:
                    // Emitting a successful choice at the first matching
                    PatternElementInstance ChoiceInstance = null;
                    foreach (PatternElement choiceElement in Choices)
                    {
                        ChoiceInstance = choiceElement.MatchElement(content, vocabulary, out consumed);
                        if(ChoiceInstance != null)
                        {
                            return new PatternElementInstance(Type, ChoiceInstance.ElementValue);
                        }
                    }
                    // Valid if we have at least one and only one choice
                    break;
                case PatternElementType.Tag:
                    string tagValue = MatchTag(Key, content, vocabulary);
                    if(tagValue != null)
                    {
                        consumed = tagValue.Length;
                        return new PatternElementInstance(Type, tagValue);
                    }
                    break;
                case PatternElementType.CategoryInclude:
                    { 
                        string match = MatchCategory(Key, true, content, vocabulary);
                        if(match != null)
                        {
                            consumed = match.Length;
                            return new PatternElementInstance(Type, match);
                        }
                    }
                    break;
                case PatternElementType.CategoryExclude:
                    {
                        string match = MatchCategory(Key, false, content, vocabulary);
                        if (match != null)
                        {
                            consumed = match.Length;
                            return new PatternElementInstance(Type, match);
                        }
                    }
                    break;
                case PatternElementType.Punctuation:
                    if(content.IndexOf(Key) == 0)
                    {
                        consumed = Key.Length;
                        return new PatternElementInstance(Type, Key);
                    }
                    break;
                case PatternElementType.UnknownPhrase:
                    // Try extract unknown from known
                    string unknownString = vocabulary.GetUnknownPhrase(content);
                    if(unknownString != null)   // Commit only if we find no match
                    {
                        consumed = unknownString.Length;
                        return new PatternElementInstance(Type, unknownString);
                    }
                    break;
                default:
                    break;
            }
            consumed = 0;
            return null;
        }

        /// <summary>
        /// Given input string, we try to match beginning parts of the stirng with specified tag type
        /// Input content will be trimmed so caller need to be cautious if depends on number of charcters being consumed since return value won't contain it
        /// </summary>
        /// <param name="tagType">A string identifier of tag type</param>
        /// <param name="content">Non-bounded ending string</param>
        /// <param name="vocabulary"></param>
        /// <returns></returns>
        private string MatchTag(string tagType, string content, VocabularyManager vocabulary)
        {
            switch (tagType)
            {
                case "email":
                    // Extract non-phrase part
                    string unknownString = vocabulary.GetUnknownPhrase(content);
                    if(unknownString != null)
                    {
                        try { MailAddress m = new MailAddress(unknownString); return unknownString; }
                        catch (FormatException) { break; }
                    }
                break;
                default:
                    break;
            }
            return null;
        }

        /// <summary>
        /// Check whether beginning parts of input string matches a cateogry, return the matched part
        /// </summary>
        /// <param name="category">category to match</param>
        /// <param name="bInclusive"></param>
        /// <param name="content"></param>
        /// <param name="vocabulary"></param>
        /// <returns>null if not found</returns>
        private string MatchCategory(string category, bool bInclusive, string content, VocabularyManager vocabulary)
        {
            // Might want to use a GetPhrase() first
            Phrase phrase = vocabulary.GetPhrase(content);
            if (phrase == null) return null;

            // Then check whether or not the phrase is in the category
            if(bInclusive)
            {
                if (vocabulary.IsInCategory(category, phrase))
                    return phrase.Key;
                else return null;
            }
            else
            {
                if (vocabulary.IsNotInCategory(category, phrase))
                    return phrase.Key;
                else return null;
            }
        }
    }

    /// <summary>
    /// Describes a specific instance of some pattern, generated during pattern matching processed, used for extracting specific syntactical elements
    /// </summary>
    internal class PatternInstance
    {
        public Pattern Type { get; }
        public List<PatternElementInstance> ComponentElements { get; set; }

        public PatternInstance(Pattern type)
        {
            ComponentElements = new List<PatternElementInstance>();
            Type = type;
        }

        // Used by subpatterns to emit their content (by retrieving elements value)
        public string Value { get {
                string returnValue = "";
                foreach (PatternElementInstance element in ComponentElements)
                {
                    if (returnValue != "") returnValue += " ";
                    returnValue += element.ElementValue;
                }
                return returnValue;
            }
        }

        // <Debug> Pending validity check considering our new additions to pattern definitions
        public int WordCount { get { return Value.Count(x => x == ' ') + 1; } }   // http://stackoverflow.com/questions/541954/how-would-you-count-occurrences-of-a-string-within-a-string

        // Returns the specific element as identified as some component name, e.g. subject/object
        // This only works if current pattern contains named subpatterns
        public string GetElementPattern(string Component)
        {
            return ComponentElements.First(instance => instance.ElementType == PatternElementType.SubPattern).ElementValue;
        }

        // Debug usage: Display all information about current pattern instance
        // <Dedbug> Incomplete implementation
        public void ShowAnalysis()
        {
            System.Console.WriteLine("Specifics of pattern [" + Type .Name + "]");

            // Provide high level view of each element
            System.Console.Write("Signature:");
            foreach (PatternElement element in Type.Elements)
            {
                System.Console.Write(" ");
                if (element.bOptional) System.Console.Write("(");
                switch (element.Type)
                {
                    case PatternElementType.SpecificWord:
                        System.Console.Write(element.Key.Contains(' ') ? ('\'' + element.Key + '\'') : element.Key);
                        break;
                    case PatternElementType.VarietyWord:
                        System.Console.Write('~' + element.Key);
                        break;
                    case PatternElementType.WordAttribute:
                        System.Console.Write('<' + element.Key + '>');
                        break;
                    case PatternElementType.SubPattern:
                        System.Console.Write('\"' + element.SubPattern.Name + '\"');
                        break;
                    case PatternElementType.Choice:
                        System.Console.Write('[');
                        bool bStart = true;
                        foreach (PatternElement choiceElement in element.Choices)
                        {
                            if (!bStart) { System.Console.Write('|'); }  // Don't draw at start
                            bStart = false;

                            switch (choiceElement.Type)
                            {
                                case PatternElementType.SpecificWord:
                                    System.Console.Write(element.Key.Contains(' ') ? ('\'' + element.Key + '\'') : element.Key);
                                    break;
                                case PatternElementType.VarietyWord:
                                    System.Console.Write('~' + element.Key);
                                    break;
                                case PatternElementType.WordAttribute:
                                    System.Console.Write('<' + element.Key + '>');
                                    break;
                                case PatternElementType.SubPattern:
                                    System.Console.Write(choiceElement.SubPattern.Name);
                                    break;
                                case PatternElementType.Choice:
                                    throw new InvalidOperationException("The pattern contains choices inside choices.");
                                case PatternElementType.Tag:
                                    throw new InvalidOperationException("The pattern contains choices inside choices.");
                                case PatternElementType.CategoryInclude:
                                    System.Console.Write('@' + element.Key);
                                    break;
                                case PatternElementType.CategoryExclude:
                                    System.Console.Write("!@" + element.Key);
                                    break;
                                case PatternElementType.Punctuation:
                                    throw new InvalidOperationException("The pattern contains choices inside choices.");
                                case PatternElementType.UnknownPhrase:
                                    throw new InvalidOperationException("The pattern contains choices inside choices.");
                                default:
                                    break;
                            }
                        }
                        System.Console.Write(']');
                        break;
                    case PatternElementType.Tag:
                        System.Console.Write('#' + element.Key);
                        break;
                    case PatternElementType.CategoryInclude:
                        System.Console.Write('@' + element.Key);
                        break;
                    case PatternElementType.CategoryExclude:
                        System.Console.Write("!@" + element.Key);
                        break;
                    case PatternElementType.Punctuation:
                        System.Console.Write(element.Key);
                        break;
                    case PatternElementType.UnknownPhrase:
                        System.Console.Write("???");    // What would element.Key be in this case?
                        break;
                    default:
                        break;
                }
                if (element.bOptional) System.Console.Write(")");
            }

            // Display specifci value of each element
            System.Console.Write("\nParameters: ");
            System.Console.WriteLine(Value);
        }
    }

    /// <summary>
    /// Describes a specific instance of some pattern element, generated during pattern matching processed, used for extracting specific syntactical elements
    /// </summary>
    internal class PatternElementInstance
    {
        public PatternElementInstance(PatternElementType type, string value)
        {
            ElementType = type;
            InstanceValue = value;
        }

        public PatternElementInstance(PatternElementType type, PatternInstance pattern)
        {
            // Assert ElementType == PatternElementType.SubPattern
            ElementType = type;
            SubPattern = pattern;   
        }

        string InstanceValue { get; }   // Display value as input by user; Valid only for Key types
        PatternInstance SubPattern;
        public PatternElementType ElementType { get; }

        public string ElementValue
        {
            get
            {
                switch (ElementType)
                {
                    case PatternElementType.SpecificWord:
                        return InstanceValue;
                    case PatternElementType.VarietyWord:
                        return InstanceValue;
                    case PatternElementType.WordAttribute:
                        return InstanceValue;
                    case PatternElementType.SubPattern:
                        return SubPattern.Value;
                    case PatternElementType.Choice:
                        return InstanceValue;
                    case PatternElementType.Tag:
                        return InstanceValue;
                    case PatternElementType.CategoryInclude:
                        return InstanceValue;
                    case PatternElementType.CategoryExclude:
                        return InstanceValue;
                    case PatternElementType.Punctuation:
                        return InstanceValue;
                    case PatternElementType.UnknownPhrase:
                        return InstanceValue;
                    default:
                        return null;
                }
            }
        }
    }
}
