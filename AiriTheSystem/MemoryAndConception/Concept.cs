using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
/// <summary>
/// Comprehension is defined as:
/// 1. Mapping from symbols to conceptual objects (in terms of pooled instances)
/// 2. Analytical structure and related rules of elements (phrases and patterns) inside a sentence
/// (3. Potentially afffecting existing memory of objects, by means of adding new connections)
/// (4. Potentially associating with conception/perception engine to enable recognize of aforementioned concepts in a more fundamental way -- this is THE effect of comprehension)
/// </summary>
namespace Airi.TheSystem.Memory
{
    /// <summary>
    /// Base class for encapsualtion/abstraction of conceptual understanding
    /// Concepts are objects encapsulating grammar concepts and provide ways to do manipulation on them; Independent of ConceptPool
    /// This part of design is used to implement comprehension (defined above), currently not used for other more specific purposes
    /// Below classes are all accessible through Concept, and their instances will be used inside ConceptPool to represent dynamic structure of things
    /// All string variables in below objects should be well-defined phrases in VocabualryManager
    /// </summary>
    abstract class Concept
    {
        private string Symbols { get; set; }
        public abstract string AddSymbol(string symbol);
    }

    /// <summary>
    /// ConceptObjects are NOT noun, they are objects that are SINGULAR by nature.
    /// There are ways to manipulate and compound a concept: +-*/, indicate add, minus, compound, disintegrate, respectively
    /// </summary>
    class ConceptObject : Concept
    {
        private string ObjectSymbols { get; set; }
    }
    // E.g. Remax = BuildingA + BuildingB + BuildingC

    /// <summary>
    /// ConceptActions are NOT verb, they are actions that are PROCEDURAL by nature; There are three ways to comprehend THE PROCESS of an action: abstract and symbolic, mathematically accurate, spatial perceptual automatically processed by human brain
    /// But aspects of an action is not just the process itself, especially when the action is understood abstractly: the possible objects (types) and chained operations (actions) in this case matters a lot how one might frame and use the action in describing things (but of course, without knowledge of physically perceiving the action the action can only be "comprehended" but not identified and used)
    /// Use a strongly formated natural logic to describe actions: if, or (|), and(+), then, then
    /// As we mentioned, it seems it's possible to describe both verbs and adj and some noun that represent verbs in this way
    /// Related facility see Category.Action
    /// </summary>
    class ConceptAction : Concept
    {
        private string ActionSymbols { get; set; }
    }
    // E.g. Danger = Touch/Hear/Smell/Think -> Death/Hurt/Harm/Damage

    /// <summary>
    /// A very specialized and very common type of concept in human language that describes one and THE ONLY ONE relationship between things (including human)
    /// In terms of English it's most often described by: have, 's, of, 物主代词 etc.
    /// Specific examples include: son, money (one of its meaning), health, and more abstractly, type, age, etc.
    /// </summary>
    class ConceptOwnership : Concept
    {
        Dictionary<string, string> Relations { get; set; }
    }

    /// <summary>
    /// Defines concept ownership for specific categories of things, i.e. those things if fall in the category are assumed to have such properties no matter whether specific values are defined for the instances or not
    /// The instances of this class are pre-populated by designer using VocabularySheet and added later during learning process when items in the category are encountered to have more relations specific pertaining to them
    /// </summary>
    class ConceptCategoryTemplate: ConceptOwnership
    {

    }
}
