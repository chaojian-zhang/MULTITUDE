using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Airi.TheSystem.Syntax;
using Airi.TheSystem.Instruction;
using System.Reflection;

namespace Airi.TheSystem.Memory
{
    /// <summary>
    /// Provides interface for accessing patterns
    /// </summary>
    /// <interpretation>
    /// A pattern is called a pattern because
    ///     - It has identifiable features
    ///     - It repeats
    /// </interpretation>
    internal class PatternMemory
    {
        public PatternMemory()
        {
            Vocabulary = new VocabularyManager();
            RecognizedPatterns = new Dictionary<string, Pattern>();
            LoadPatterns();
        }

        // Currently loaded patterns
        Dictionary<string, Pattern> RecognizedPatterns { get; set; }
        VocabularyManager Vocabulary;

        // For loading patterns from internal SPatternExp files
        private void LoadPatterns()
        {
            StreamReader reader;
            try
            {
                reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(Airi.Properties.Resources.PatternDefinitionPath));
                if(reader != null)
                {
                    string fileContent = reader.ReadToEnd();
                    string[] lines = fileContent.Split(new char[] { '\n', '\r' });

                    foreach (string line in lines)
                    {
                        if (line == String.Empty) continue;
                        // Add a pattern to memory
                        Pattern pattern = GeneratePatternFromExpression(line);
                        if (pattern != null)
                            RecognizedPatterns.Add(pattern.Name, pattern);
                    }
                }
            }
            catch
            {
                throw new Exception();
            }
        }

        // For validating grammar of external SPatternExp files
        private bool ValidatePatternDefinitions(string filePath)
        {
            // Check file existence
            if (File.Exists(filePath))
            {
                string fileContent = File.ReadAllText(filePath);
                string[] lines = fileContent.Split(new char[] { '\n', '\r' });

                uint lineNumber = 0;
                foreach (string line in lines)
                {
                    if (line == String.Empty) continue;
                    try
                    {
                        // Check whether indicate a valid pattern by try to generate a pattern from it
                        GeneratePatternFromExpression(line);
                    }
                    catch (InvalidPatternStringException e)
                    {
                        System.Console.WriteLine(string.Format("Invalid syntaxt at line {0}: {1}"), lineNumber, e.Message);
                        return false;
                    }
                    lineNumber++;
                }
                return true;
            }
            return false;
        }

        // Helper for continously reading definition file lines
        private bool bCommentBlock = false;
        /// <summary>
        /// Generate a pattern object from pattern definition string
        /// This function can also be used to validate input expressions
        /// </summary>
        /// <param name="patternString">A line input from definition file</param>
        /// <returns>Returns a valid pattern if found, null if empty, or throw InvalidPatternStringException when input pattern string isn't recognized SPatternExp format</returns>
        // Implementated using a state machine
        private Pattern GeneratePatternFromExpression(string patternString)
        {
            // Preprocessing
            patternString = patternString.Trim();   // Remove starting and trailing white spaces

            // Punctuation handling: insert spaces before punctuations for later handling
            // :,.?!-
            patternString.Replace("(:)", " (:) ");
            patternString.Replace("(,)", " (,) ");
            patternString.Replace("(.)", " (.) ");  // Request for avoid conllision with abbreviations (though not necessary for pattern definitions)
            patternString.Replace("(?)", " (?) ");
            patternString.Replace("(!)", " (!) ");
            patternString.Replace("(-)", " (-) ");
            patternString.Replace(": ", " : ");
            patternString.Replace(", ", " , ");
            patternString.Replace(". ", " . ");  // Request for avoid conllision with abbreviations (though not necessary for pattern definitions)
            patternString.Replace("? ", " ? "); // Yes in a pattern string definition there will be a space after it, which is not neceesarily the case in ordinary languages
            patternString.Replace("! ", " ! "); // Yes in a pattern string definition there will be a space after it, which is not neceesarily the case in ordinary languages
            // patternString.Replace(" - ", " - ");

            // Comments handling
            // "//" Style comments: just strip it
            if (patternString.Contains("//")) patternString = patternString.Substring(0, patternString.IndexOf("//"));
            // "/**/" Style comments: just strip them as well; If they do not form a pair in current line then handle specially
            int startComment = patternString.IndexOf("/*");
            int endComment = patternString.IndexOf("*/");
            while (startComment != -1 || endComment != -1)
            {
                if (startComment != -1) // Enter a coment block
                {
                    bCommentBlock = true;
                }
                if (endComment != -1)   // Exit it
                {
                    bCommentBlock = false;
                }
                if (startComment != -1 && endComment != -1) // Embeded comment block in a line
                {
                    patternString = patternString.Substring(0, startComment) + patternString.Substring(endComment + 2);
                }

                // Check for multiple comment blocks in a single line
                startComment = patternString.IndexOf("/*");
                endComment = patternString.IndexOf("*/");
            }
            if (bCommentBlock)   // If we are still in comment block then continue
                return null;

            // Ignore white spaces
            if (String.IsNullOrWhiteSpace(patternString) == true) return null;

            // Check pattern validity: pattern must contain a name, must have at least one element after the name, All elements must be properly enclosed
            if (patternString.Contains(":") == false)
            {
                throw new InvalidPatternStringException("Pattern doesn't have a name and it's recommended (required) to have one");
            }
            if (patternString.Substring(patternString.LastIndexOf(':') + 1).Split(new char[] { ' ' }).Count() < 1) // <Improvement> This is not yet a robust check
            {
                throw new InvalidPatternStringException("Pattern contain not enough elements after pattern name");
            }
            if (CountSubStringOccurrences(patternString, "\"") % 2 != 0)
            {
                throw new InvalidPatternStringException("Pattern name not well bound by \"\"");
            }
            if (CountSubStringOccurrences(patternString, "'") % 2 != 0)
            {
                throw new InvalidPatternStringException("Pattern word phrase elemetns not well bound by '");
            }
            // Check for [] and <>
            if (patternString.Contains("[") && (CountSubStringOccurrences(patternString, "[") != CountSubStringOccurrences(patternString, "]")))
            {
                throw new InvalidPatternStringException("Pattern choise elements not well bound by []");
            }
            if (patternString.Contains("<") && (CountSubStringOccurrences(patternString, "<") != (CountSubStringOccurrences(patternString, ">") - CountSubStringOccurrences(patternString, "-->"))))
            {
                throw new InvalidPatternStringException("Pattern word attribute elements not well bound by <>");
            }
            else
            {
                // Get pattern name
                string patternName = patternString.Substring(0, patternString.IndexOf(':'));

                // Extract action for later processing
                string actionName = null;
                int actionIndex = patternString.IndexOf("-->");
                if (actionIndex != -1)
                {
                    actionName = patternString.Substring(actionIndex + 3).Replace(" ", String.Empty);
                    patternString = patternString.Substring(0, actionIndex);
                }
                Pattern newPattern = new Pattern(patternName, actionName);

                // Extract Elements, seperate by white space, or in case of word phrase and sub patterns, by '' or "" or []
                patternString = patternString.Substring(patternString.LastIndexOf(':') + 1).Replace('\t', ' ');
                string[] elements = SplitPatternStringElement(patternString);

                // Iteratively generate all elements from expression
                foreach (string notation in elements)
                {
                    if (notation == "") continue;    // Ignore blanks

                    // Check optional
                    bool bOptional = false;
                    string element = notation;
                    if (notation[0] == '(' && notation[notation.Length - 1] == ')')
                    {
                        bOptional = true;
                        element = notation.Substring(1, notation.Length-2);
                    }

                    // VarietyWord
                    if (element[0] == '~')
                    {
                        if (element.Length == 1) throw new InvalidPatternStringException("Expression incomplete for variety word");
                        else newPattern.Elements.Add(new PatternElement(PatternElementType.VarietyWord, element.Substring(1), bOptional));
                    }
                    // WordAttribute
                    else if (element[0] == '<')
                    {
                        newPattern.Elements.Add(new PatternElement(PatternElementType.WordAttribute, element.Substring(1, element.LastIndexOf('>') - 1), bOptional));
                    }
                    // SubPattern
                    else if (element[0] == '"')
                    {
                        string subPatternName = element.Substring(1, element.LastIndexOf('"') - 1);
                        if (RecognizedPatterns.ContainsKey(subPatternName) && subPatternName != patternName)    // Make sure not already exist and subpattern doesn't refer to pattern itself <Improvement> We wll support recursion later as long as there is an ending condition
                        {
                            newPattern.Elements.Add(new PatternElement(RecognizedPatterns[subPatternName], bOptional));
                        }
                        else
                        {
                            throw new InvalidPatternStringException("Expression contain undefined subpattern");
                        }
                    }
                    // Choice
                    else if (element[0] == '[')
                    {
                        // Extract choices
                        string[] choices = element.Substring(1, element.LastIndexOf(']') - 1).Split(new char[] { '|' });
                        List<PatternElement> choiceCandidates = new List<PatternElement>();

                        // Iteratively go throw all choices
                        foreach (string choice in choices)
                        {
                            if (choice == "") continue;    // Ignore blanks

                            // VarietyWord
                            if (choice[0] == '~')
                            {
                                if (choice.Length == 1) throw new InvalidPatternStringException("Expression choice incomplete for variety word");
                                choiceCandidates.Add(new PatternElement(PatternElementType.VarietyWord, choice.Substring(1)));
                            }
                            // WordAttribute
                            if (choice[0] == '<')
                            {
                                choiceCandidates.Add(new PatternElement(PatternElementType.WordAttribute, choice.Substring(1, choice.LastIndexOf('>') - 1)));
                            }
                            // SubPattern
                            if (choice[0] == '"')
                            {
                                string subPatternName = choice.Substring(1, choice.LastIndexOf('"') - 1);
                                if (RecognizedPatterns.ContainsKey(subPatternName) && subPatternName != patternName)    // Make sure not already exist and subpattern doesn't refer to pattern itself
                                {
                                    choiceCandidates.Add(new PatternElement(RecognizedPatterns[subPatternName]));
                                }
                                else
                                {
                                    throw new InvalidPatternStringException("Expression choice contain undefined subpattern");
                                }
                            }
                            // Specific Word Phrase
                            else if (element[0] == '\'')
                            {
                                newPattern.Elements.Add(new PatternElement(PatternElementType.SpecificWord, choice.Substring(1, choice.LastIndexOf('\'') - 1)));
                            }
                            // Specific Word
                            else
                            {
                                choiceCandidates.Add(new PatternElement(PatternElementType.SpecificWord, choice));
                            }
                        }

                        newPattern.Elements.Add(new PatternElement(choiceCandidates, bOptional));
                    }
                    // Specific Word Phrase
                    else if (element[0] == '\'')
                    {
                        newPattern.Elements.Add(new PatternElement(PatternElementType.SpecificWord, element.Substring(1, element.LastIndexOf('\'') - 1), bOptional));
                    }
                    // Tagged identification for concpet or object
                    else if (element[0] == '#')
                    {
                        newPattern.Elements.Add(new PatternElement(PatternElementType.Tag, element.Substring(1), bOptional));
                    }
                    // Organized category for names
                    else if (element.Substring(0,2) == "!@")
                    {
                        newPattern.Elements.Add(new PatternElement(PatternElementType.CategoryExclude, element.Substring(2), bOptional));
                    }
                    // Organized category for names
                    else if (element[0] == '@')
                    {
                        newPattern.Elements.Add(new PatternElement(PatternElementType.CategoryInclude, element.Substring(1), bOptional));
                    }
                    // Unknown phrases/expressions
                    else if (element == "???")
                    {
                        newPattern.Elements.Add(new PatternElement(PatternElementType.UnknownPhrase, element, bOptional));
                    }
                    // Specific Word
                    else
                    {
                        newPattern.Elements.Add(new PatternElement(PatternElementType.SpecificWord, element, bOptional));
                    }
                }

                // Handle actions
                // ... Might want to embed into Pattern class

                return newPattern;
            }
        }

        private string[] SplitPatternStringElement(string elementsString)
        {
            // If not contain complicated elements then use simple method
            if (elementsString.Contains('\'') == false && elementsString.Contains('\"') == false && elementsString.Contains('[') == false) return elementsString.Split(new char[] { ' ' });
            else
            {
                // First figure out how many elements are in it
                int nElements = CountPatternStringElements(elementsString); // Notice elementsString is gurateed to have even number of "'" or "\"\""upon entering this function
                string[] returnElements = new string[nElements];

                // For each element extract it
                int currentCharIndex = 0;
                bool bPhrase = false;
                bool bPattern = false;
                bool bChoice = false;
                for (int i = 0; i < nElements; i++)
                {
                    // Ending condition
                    if (currentCharIndex == elementsString.Length)   // E.g. hanlding " " or " some string  "
                    {
                        returnElements[i] = string.Empty;
                        continue;
                    }

                    int elementLength = 0;
                    // Word Phrase
                    if (elementsString[currentCharIndex] == '\'' && !bChoice)
                    {
                        bPhrase = true;
                        elementLength = 1;
                    }
                    // Pattern
                    if (elementsString[currentCharIndex] == '\"' && !bChoice)
                    {
                        bPattern = true;
                        elementLength = 1;
                    }
                    // Choice
                    if (elementsString[currentCharIndex] == '[')
                    {
                        bChoice = true;
                        elementLength = 1;
                    }

                    if (bPhrase && !bChoice)
                    {
                        while (elementsString[currentCharIndex + elementLength] != '\'') elementLength++;
                        bPhrase = false;
                        elementLength++;    // Include the ending '\''
                    }
                    else if (bPattern && !bChoice)
                    {
                        while (elementsString[currentCharIndex + elementLength] != '\"') elementLength++;
                        bPattern = false;
                        elementLength++;    // Include the ending '\"'
                    }
                    else if (bChoice)
                    {
                        while (elementsString[currentCharIndex + elementLength] != ']') elementLength++;
                        bChoice = false;
                        elementLength++;    // Include the ending ']'
                    }
                    else
                    {
                        while ((currentCharIndex + elementLength) != elementsString.Length && elementsString[currentCharIndex + elementLength] != ' ') elementLength++;
                    }

                    // Handle empty
                    if (elementLength == 0)
                    {
                        returnElements[i] = string.Empty;
                        currentCharIndex += 1;
                    }
                    else
                    {
                        returnElements[i] = elementsString.Substring(currentCharIndex, elementLength);
                        currentCharIndex += elementLength + (((currentCharIndex + elementLength) == elementsString.Length ) ? 1:0);  // Skip space
                    }
                }

                return returnElements;
            }
        }

        // <Debug> Currently cannot handle "A B" where we can identify only one element
        private int CountPatternStringElements(string elementsString)
        {
            int nElements = 0;
            bool bSpace = false;
            bool bPhrase = false;
            bool bPattern = false;
            bool bChoice = false;

            // Go through each character and discover scopes
            foreach (char c in elementsString)
            {
                if (c == ' ' && bPhrase == false && bPattern == false && bChoice == false)
                {
                    if (bSpace == false) { bSpace = true; nElements++; }
                }
                else if (c == '\'' && bChoice == false)
                {
                    if (bPhrase == false) bPhrase = true;
                    else { bPhrase = false; nElements++; }
                }
                else if (c == '\"' && bChoice == false)
                {
                    if (bPattern == false) bPattern = true;
                    else { bPattern = false; nElements++; }
                }
                else if (c == '[')
                {
                    if (bChoice == false) bChoice = true;
                    else throw new Exception("Unbalaced choice elements.");
                }
                else if (c == ']' && bChoice == true)
                {
                    bChoice = false; nElements++;
                }
                else
                {
                    bSpace = false;
                }
            }
            if (bSpace) nElements++;  // Handle ending condition where we didn't have a chance to add one more
            // Handle "A B"
            if (elementsString.Last() != '\'' && elementsString.Last() != '\"' && elementsString.Last() != ']' && elementsString.Last() != ' ') nElements++;

            return nElements;
        }

        // Algorithm: http://stackoverflow.com/questions/541954/how-would-you-count-occurrences-of-a-string-within-a-string
        // Regarding Pass By Value: http://stackoverflow.com/questions/10792603/how-are-strings-passed-in-net, https://msdn.microsoft.com/en-us/library/system.string.copy(v=vs.110).aspx
        // Count substring's numbero f occurrences
        private int CountSubStringOccurrences(string originalString, string substring)
        {
            return (originalString.Length - originalString.Replace(substring, string.Empty).Length) / substring.Length;
        }

        // For Unloading currently learnt patterns into a file
        public void UnloadPatterns(string filePath)
        {
            throw new NotImplementedException();
        }

        // Interaction Interface: Return whether or not a given sentence matches given pattern: If a match is found a PatternInstance will be returned other wise null
        // Notice that patterns provide EXACT matches, this gives designers flexibility in defining general or specific matches per need/context; For subpatterns the length cannot be decided though so an option if given
        // <Debug> Pending thorough logic check
        // @sentence: input sentence must be well formed: no auxiliary blanks, no punctuation (only words are allowed for now).
        public PatternInstance IsMatchPattern(string sentence, Pattern pattern, bool bExactMatch = true)
        {
            // Prepare return value
            PatternInstance instance = new PatternInstance();
            instance.Type = pattern;

            // Split sentence into words
            string[] words = sentence.Split(new char[] { ' ' });    // <Debug><Improvement> Current we are not dealing with sentences that might contain commas (and we should deal with it here), 
            // or multiple sentences types in one piece (and this one should be handled and seperated by higher level input engine because a pattern is designed to handle one setence only (with commas)).

            int currentWord = 0;    // ID of the word we are matching against
            // Stage 1: Iterate through pattern elements: if all elements match then it is likely a good match
            foreach (PatternElement element in pattern.Elements)
            {
                // Make sure we still have more words to match against, otherwise input string is too short for this pattern
                if (words.Length <= currentWord) return null;

                // Predefine so later it won't cause local variable name conflict
                Phrase phrase;
                bool result;
                PatternInstance temp;
                WordAttribute attribute;
                string tempString;
                int overlap;
                // Match element type and vlaue
                switch (element.Type)
                {
                    case PatternElementType.SpecificWord:
                        // Prepare two strings
                        tempString = words[currentWord];
                        for (int i = currentWord + 1; i < words.Length; i++) tempString = tempString + ' ' + words[i];
                        overlap = GetStringOverlapLowerCase(tempString, element.Key);

                        // If the following words don't match and it's not optional then this pattern doesn't match
                        if (overlap == -1 && element.bOptional == false) return null;
                        else
                        {
                            // Generate Pattern Element Instance and Proceed to next word
                            instance.ComponentElements.Add(new PatternElementInstance(PatternElementType.SpecificWord, tempString.Substring(0, overlap)));
                            currentWord += GetNumOfWords(element.Key);
                        }
                        break;
                    case PatternElementType.VarietyWord:
                        result = Vocabulary.IsWordVaryingFormOrSynonym(words[currentWord], element.Key);
                        if (result == false && element.bOptional == false) return null;
                        else
                        {
                            // Generate Pattern Element Instance and Proceed to next word
                            instance.ComponentElements.Add(new PatternElementInstance(PatternElementType.VarietyWord, words[currentWord]));
                            currentWord++;
                        }
                        break;
                    case PatternElementType.WordAttribute:
                        // <Debug> This is currently incomplete implementation, i.e. + for multiple constriants (e.g. for verbs)
                        attribute = (WordAttribute)Enum.Parse(typeof(WordAttribute), element.Key);
                        // foundWord = Vocabulary.ProbeWord(words[currentWord], attribute); // Notice for WordAttribute we are not matching against word, but phrase
                        tempString = words[currentWord];
                        for (int i = currentWord + 1; i < words.Length; i++)
                        {
                            tempString = tempString + ' ' + words[i];
                        }
                        phrase = Vocabulary.GetPhrase(tempString, attribute);
                        if (phrase == null && element.bOptional == false) return null;
                        else
                        {
                            // Generate Pattern Element Instance and Proceed to next word
                            instance.ComponentElements.Add(new PatternElementInstance(PatternElementType.WordAttribute, phrase.Key));
                            currentWord += phrase.WordCount;
                        }
                        break;
                    case PatternElementType.SubPattern:
                        tempString = words[currentWord];
                        for (int i = currentWord + 1; i < words.Length; i++)
                        {
                            tempString = tempString + ' ' + words[i];
                        }
                        temp = IsMatchPattern(tempString, element.SubPattern, false);   // Do not do an exact match in this case
                        if (temp == null && element.bOptional == false) return null;
                        else
                        {
                            // Generate Pattern Element Instance and Proceed to next word
                            instance.ComponentElements.Add(new PatternElementInstance(PatternElementType.SubPattern, temp));
                            currentWord += temp.WordCount;
                        }
                        break;
                    case PatternElementType.Choice:
                        // Return the first matched selection
                        bool bLoopBreak = false;
                        foreach (PatternElement choiceElement in element.Choices)
                        {
                            // break loop, not just the switch
                            if (bLoopBreak) break;

                            // Match CHOICE element type and vlaue
                            // Notice that CHOICE elements are always optional, but at least one must be selected
                            switch (choiceElement.Type)
                            {
                                case PatternElementType.SpecificWord:
                                    // Prepare two strings
                                    tempString = words[currentWord];
                                    for (int i = currentWord + 1; i < words.Length; i++) tempString = tempString + ' ' + words[i];
                                    overlap = GetStringOverlapLowerCase(tempString, choiceElement.Key);

                                    // If next word doesn't match then continue; If match then no more loop
                                    if (overlap == -1) continue;
                                    else
                                    {
                                        // Generate Pattern Element Instance and Proceed to next word
                                        instance.ComponentElements.Add(new PatternElementInstance(PatternElementType.SpecificWord, tempString.Substring(0, overlap)));
                                        currentWord += GetNumOfWords(choiceElement.Key);
                                        bLoopBreak = true;
                                    }
                                    break;
                                case PatternElementType.VarietyWord:
                                    // If word doesn't match then continue; If match then no more loop
                                    result = Vocabulary.IsWordVaryingFormOrSynonym(words[currentWord], choiceElement.Key);
                                    if (result == false) continue;
                                    else
                                    {
                                        // Generate Pattern Element Instance and Proceed to next word
                                        instance.ComponentElements.Add(new PatternElementInstance(PatternElementType.VarietyWord, words[currentWord]));
                                        currentWord++;
                                        bLoopBreak = true;
                                    }
                                    break;
                                case PatternElementType.WordAttribute:
                                    attribute = (WordAttribute)Enum.Parse(typeof(WordAttribute), choiceElement.Key);
                                    // foundWord = Vocabulary.ProbeWord(words[currentWord], attribute); // Notice for WordAttribute we are not matching against word, but phrase
                                    tempString = words[currentWord];
                                    for (int i = currentWord + 1; i < words.Length; i++)
                                    {
                                        tempString = tempString + ' ' + words[i];
                                    }
                                    phrase = Vocabulary.GetPhrase(tempString, attribute);
                                    // If trailing string doesn't match then continue; If match then no more loop
                                    if (phrase == null && choiceElement.bOptional == false) continue;
                                    else
                                    {
                                        // Generate Pattern Element Instance and Proceed to next word
                                        instance.ComponentElements.Add(new PatternElementInstance(PatternElementType.WordAttribute, phrase.Key));
                                        currentWord += phrase.WordCount;
                                        bLoopBreak = true;
                                    }
                                    break;
                                case PatternElementType.SubPattern:
                                    tempString = words[currentWord];
                                    for (int i = currentWord + 1; i < words.Length; i++)
                                    {
                                        tempString = tempString + ' ' + words[i];
                                    }
                                    // If trailing string doesn't match then continue; If match then no more loop
                                    temp = IsMatchPattern(tempString, choiceElement.SubPattern);
                                    if (temp == null) continue;
                                    else
                                    {
                                        // Generate Pattern Element Instance and Proceed to next word
                                        instance.ComponentElements.Add(new PatternElementInstance(PatternElementType.SubPattern, temp));
                                        currentWord += temp.WordCount;
                                        bLoopBreak = true;
                                    }
                                    break;
                                default:
                                    break;
                            }
                        }
                        // If no match was found then this pattern doesn't match, otherwise continue
                        if (bLoopBreak == false) return null;
                        break;
                    default:
                        break;
                }
            }

            // Stage 2: If we are doing an exact match then make sure there is no auxiliary words
            if (bExactMatch == true && words.Length != currentWord) return null;

            return instance;
        }

        // Return number of words in a phrase, seperated by "", and assume no redundant white spaces
        private int GetNumOfWords(string key)
        {
            if (key.Contains(' ') == false) return 1;
            return CountSubStringOccurrences(key, " ") + 1;
        }

        // Return the overlap part of two strings (number of characters overlap), or -1 if no overlap
        public int GetStringOverlapLowerCase(string string1, string string2)
        {
            string1 = string1.ToLower();
            string2 = string2.ToLower();

            int overlap;
            int shortStringLength;
            if (string1.Length > string2.Length)
            {
                overlap = string1.IndexOf(string2);
                shortStringLength = string2.Length;
            }
            else
            {
                overlap = string2.IndexOf(string1);
                shortStringLength = string2.Length;
            }

            // If no overlap
            if (overlap == -1)
                shortStringLength = -1; 

            return shortStringLength;
        }

        // Interaction Interface: Find which patterns a given sentence matches (Ideally any sentence matches only one pattern because we require EXACT match but designers can make mistakes so we return all matches)
        public List<PatternInstance> FindMatchPatterns(string sentence)
        {
            List<PatternInstance> matchedPatterns = new List<PatternInstance>();

            foreach (KeyValuePair<string, Pattern> entry in RecognizedPatterns)
            {
                PatternInstance result = IsMatchPattern(sentence, entry.Value);
                if (result != null) matchedPatterns.Add(result);
            }

            return matchedPatterns;
        }
    }

    /// <summary>
    /// Patterns contain invalid definitions
    /// </summary>
    [Serializable]
    internal class InvalidPatternStringException : Exception
    {
        public InvalidPatternStringException() { }
        public InvalidPatternStringException(string message) : base(message) { }
        public InvalidPatternStringException(string message, Exception inner) : base(message, inner) { }
        protected InvalidPatternStringException(
          global::System.Runtime.Serialization.SerializationInfo info,
          global::System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
