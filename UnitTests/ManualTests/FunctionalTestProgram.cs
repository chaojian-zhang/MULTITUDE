using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
///  Well this isn't very "formal" unit test, but just testing of isolated functions -- delete them after test is done
/// </summary>
namespace UnitTests
{
    class FunctionalTestProgram
    {
        static int CountPatternStringElements(string elementsString)
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

        static string[] SplitPatternStringElement(string elementsString)
        {
            // If not contain complicated elements then use simple method
            //if (elementsString.Contains('\'') == false && elementsString.Contains('\"') == false && elementsString.Contains('[') == false) return elementsString.Split(new char[] { ' ' });
            //else
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
                        currentCharIndex += elementLength + 1;  // Skip space
                    }
                }

                return returnElements;
            }
        }

        static int CountSubStringOccurrences(string originalString, string substring)
        {
            return (originalString.Length - originalString.Replace(substring, string.Empty).Length) / substring.Length;
        }


        // Return the overlap part of two strings (number of characters overlap), or -1 if no overlap
        static int GetStringOverlapLowerCase(string string1, string string2)
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

        static void Main(string[] args)
        {
            DoTest("[\"Interruption 1\"|\"Interruption 2\"|\"Interruption 3\"]");
            DoTest(" This is a 'Test String' and this is a nother 'Test String'  ");
            DoTest(" ");
            DoTest("AllGoodNoSpace");
            DoTest("A B");

            Console.WriteLine(GetStringOverlapLowerCase("is sphere", "is").ToString());

            Console.ReadLine();
        }

        static void DoTest(string testString)
        {
            string[] results = SplitPatternStringElement(testString);
            foreach (string result in results)
            {
                Console.WriteLine(result);
            }
        }
    }
}
