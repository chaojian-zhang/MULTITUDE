using Airi.TheSystem.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Airi.TheSystem.Conception
{
    /// <summary>
    /// Provides a mechanism to uniquely identify a concept, i.e. providing GUID
    ///     Also provides quick access to any concept using ID
    /// </summary>
    class ConceptID
    {
        private int ID;
    }

    /// <intrpretation>
    ///     A concept is a purely abstract self-organizing structure.
    ///     Concepts are infitestimally divisible.
    ///     There is One root concept above all.
    ///     Concepts are fast to navigate because they are simple, but impossible to end because there is no end rule (that's why people never stop thinking).
    ///     When we say a sentence/pattern, which is a concept, "contains" concepts of other things, e.g. words - we need to know this is not concept per se, but formula for identifying concepts.
    ///     If a concpet is purely abstract and ambiguous then it's not accessible and useless to the language.
    /// </intrpretation>

    /// <summary>
    /// Defines abstract concepts for Airi, working in coordiance with Memory and Patterns
    ///     A concept might be related to a patten, might be describing a process, might be describing a compound object -- but this merely depends on whatever interpretation is related to this concpet
    /// @???
    ///     - A single word, despite its varieties and explicit meanings and attributes, always represents at least one and only concept;
    ///     - If a concepte requires more than one concept to trigger it (i.e. multi-entrance), then its parents are grouped inside another concept, because a concept always have only one direct parent only;
    ///     - Concpets are not a map/graph: they are more like a multi-dimensional tree;
    ///     - Concepts are not directly defined, they are described using catagorized concepts (i.e. concept catagories text);
    ///     - The proper loading order for constructing Airi's memory: dictonary words, concept catagories, patterns, then finally themes (stories and conversations), then maybe extra learning materials.
    /// </summary>
    class Concept
    {
        // Each concept has a GUID to distinguish it from others; A GUID in unique across the whole memory space
        private ConceptID GUID;

        // A Concept might be multi-layer related to a bunch of whole other concepts
        private List<ConceptID> SubConcepts;
        // A concept may have only ONE parent concept, from that we may figure out all its siblings etc.
        private ConceptID ParentConcept;

        // Related words describes meaning of concept, subjet to interpretation of user
        Dictionary<string, Word> RelatedWords;

        // A concept becomes useful when it can define behaviors
        List<Airi.TheSystem.Instruction.Action> RelatedActions;
        List<Airi.TheSystem.Instruction.Accessor> RelatedAccessors;

        // A concept is also conceptually mapped to a well-organized-theme -- a theme that contains well defined responses
        Theme Theme;
    }
}

/* Do you think logically, or your thinking is bounded by available logic constructs/language/grammar in your head? It's curious how people "think" using language.
 * Fundation of Thinking: 
 *  At the very root/beginning of thinking, there is one concept, call it nature or self or spirit or mind.
 *  All perceptions, as described below, are either ignored or contributed(identified) to this root concept.
 *  Short-path reflection, interrupt, non-end-path thinking, language pattern guides people's mind.
 * 
 * Fundamental Conception Behaviors: Given any perception, human's next response is one of the below
 *  (Unconsciously)Ignore
 *  Process
 *      Imagination of related concepts
 *      Conceptualization as identification
 * 
 * Fundamental Language Behaviors: Given any input speech/thought, human's next response is one of the below // Notice this is enough for programming response, thought not yet for understanding
 *  Statement
 *      Informative
 *          Random conception
 *      Emotional
 *          语气词
 *      Logical
 *          First, then, then
 *          If, then
 *          Attribute
 *  Question
 *      Essence
 *      Process
 *      Rationale
 */

/* Assertions:
 * 1. It is impossible to odevelop a machine that just learns without requiring any designer-specific knowledge because in that case the machine is just like a human baby: it can only learn through a lot of practice etc. And still it is impossible to 
 *    achieve mental equilibrium because it cannot determine which are short-path thinking (and emotions) and which are good information. ANd knowledge in his mind isn't well organized - not suitable for practical use.
 */

/* What if a language fully describes possible concepts within?
 *     Given any word, or subpattern, it is possible to identify a parent pattern and follow this until a sentence is completed (inspired by: we generate a complete sentence while we think/speak, not before)
 *     Nouns describe other nouns, as their names indicate, e.g. "location"
 *     Follow this, there are LIMITED AMOUNT OF CONCEPTUAL STRUCTURES for organizing all kinds of information
 *     The natural processing of information (e.g. people interpreting a sentence) doesn't involve looking into specific menaing of words - until we decided to investigate that; In other words, symbols are just interpreted/remembered.processed as symbols
 *         E.g. When we hear "Bob eat an apple for lunch" we don't care what is the difference between an "apple" and an "organge" and there is NO such image of apple coming to our mind - that only happens because immediately after that we decided to look
 *         into the meaing of apple; One could as equally probably looking into the meaning of lunch and think of what he/she will eat and don't worry about apples at all. This provides rationale for storing meaning/def of words as is, 
 *         using a/multiple statements.
 *     The lgoical flow of conversation is defined as ...
 *     
 * On Intrepretation of Human Texts
 *     - Every setence non-uniquely identifies one piece of information, and all possible combinations of language constructs is well defined for any sentence;
 *     - GIven an article on anything (i.e. not necessarily a dialog that Airi The System was originally designed to play with), we can study the meaning of each sentence and structure it; And define multiple "subjects" from one discussion
 *     - All contents of a sentence, unless marked as "Definition", are marked as "WArning Interpreted/Observation" to indicate lack of contextual information e.g. "The most widely used language in world";
 *     - Things like "raining cats and dogs" or "feeling blue" are not rational, they are merely cultrual language conventions.
 *     
 * On Inteilligent Behavior     
 * : Pattern, Process, Logic helps choose words and action.
 */

/* 1. Concepts are described in complete by words/sentences/phrases (called a "simple concept"); There are no layers inside this kind conecpt; This concept is a listing of subconcepts
 * 2. Another kind of concept is called a "process", which contains information about a process, thus it's naturally a <vector> type. A process can contain condition and loop with break. A process might also embed pattern allowing identifying
 * process using complicated methods. A process might also contain compound process describing each step in detail. A process is pure text and human readable because it constitutes of sentences. A process naturally describes "how to" concepts.
 * 3. A definition is a textual description for a concept, e.g. a word.
 * 4. Conversation flow have following aspects:
 *  - Self-Recognized from learning....
 *  - Desgined defined processes - These might be high level process that describes whole interaction cycle
 *  - Pattern recognition - Any sentence falls into one process, more sentences adds definition; The matching will be ambigous; The matched process, if available, enable Airi to choose repsponse accordingly
 *  - Logical information e.g. names, question, discussions
 *      : Defined as specific general purpose process with pattern
 *      : Have a clear physical boundary regarding where to begin and where to end
 *      : Might have working memory constructs for in-depth communication for any identified begining session
 */

/* Interaction: 
 * 1. Guess Word: Obviously this will be a pattern explicitly defined for entrance
 */

/* @On human language interpretation
Fallback: Preset actions for specific sentences(e.g. when people ask what do you mean they seek a way to understand intention/background/motivation/source of information), patterns for actions, dialog storage Knowledge quick match, personal understanding/interpretation or intention and respond accordingly
 */

/* Pending:
 *  Terminology/Phrase extraction.
 */

/* @Conceptualization
1 Dynamic Relation (e.g. Types of cups). There are only two ways to define such: use of "of", or a sequential convention. In essence, iteration of proposition completely defines such constructs (and we can create a mapping for each). That is, either "A of B" or "B A" or "B's A" defines A as an aspect of B, with contents of A pending populated. It's essential to be able to differentiate between Mountainl Types from Mount Fuji, both of which conforms the form. We should recognize both are complete phrases as well, it's just former contains conceptual children while later doesn't. While Mount Fuji Name is another phrase which does contain conceptual children (as "Fuji"). Symbolically, A B C = {} with one or more elements defines a relation while A B C without extra qualification defines an instance. If the set contains only one element then it's a property, otherwise it's an enumeration. Such knowledge is picked during learning process with well defined learning materials. 
2 Concept Access of Relation: All pronoun can be substituted in interpretation/preprocessing process to mark either a description/constraint or accessor for more specific object/concept. E.g. His/my can be substituted as John/[Speaker name]'s, which refers directly to John/[Speaker Name] those objects where we can operate on.
3 Conceptually, the difference between type and instance is that  types define new properties while instance only gives new data. An instance by all means can become a type when it's definition is no longer bounded by its type or new understanding arises. 
4 <New>A concept is simply a node that can contain many other things - but without a hierarchy. It's either the end point of Conception, or other means are used to further access information. There is NO concept of parent and children for a concept in this case. And a concept can be implemented as a vector of phrases. And there is no object encapsulation of a "relation" - it's just a concept of a phrase, as mentioned above.
5 Understanding is defined as instantiation: every word can be explained by others without an ending unless it is identifiable as an object, with properties and/or value. Every word is abstract unless instantiable. The word apple, spectacular, and eat has no meaning until it's instantiated. 
: Every "using" (rather than "mentioning") of an apple/volors instantiates a new one in mind, whether or not it stands for the same apple we know/identify elsewhere depending on a lower level comparison, which in our case in encapsulated inside apple/color class and given. E.g. Every instance of concept "apple" has a property(implicit) as "color" which by itself is a completely new instance, which Color class might store things more efficiently by defining colors using only RGB and merge the same ones together. When comparing two colors it's also defined by Color class as well.
 */
