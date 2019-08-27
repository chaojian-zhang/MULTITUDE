/************************** Comments **************************/
// This file defines grammar pattern recognition for Airi
// Patterns are used for two purposes:
// - General grammar definition: This is used to decompose sentences into syntactial elements, normally handled to generic conceptual processing framework for generating outputs
// - Domain specific commands/expressions: This is used to define specific expressions for specific types of interactions, normally have specialized actions associated
// Patterns are not used to below functions:
// - Attributes, properties, categories of specific words: the knowledge of these should be defined in dictionary
// - Concepts: the knowledge of these should be defined in conceptual patterns or memories
// Usage: The formatting should be strict and pay attention to spaces; We have provided validity checker for checking syntax problems
// Definition here can contain lower and upper case for readability, but during processing all internal vocabular, input strings and hanlding routines will deal with lower case only

/* (Not recommded) Examples: 
Subject: <noun> // Improper definition; Such kind of patterns should either be defined in dictionary file or implied, to avoid conflicting with other more useful patterns
State: <adj> // Improper definition:  Unnecessary as above */

/************************** Formal Definitions **************************/
// Key Grammars
Be: [be|is|was|are|were]	// Notice this is NOT defined in dictionary because dictionaries carry reference of meaning and some simple grammar(like a real dictionary), not language constructs
Verb phrase: [do|<verb>] <noun>
Spatial prep: [on|above|in|below|behind|inside]
Descriptive component: (<art>) (<*adv>) (<*adj>) <noun> // Noun phrase (名词性短语)	// In general avoid such embeded patterns for better efficiency and processability
People prep: [he|she|I]
People: [@PeopleName|@PeopleProp|"People prep"]
动词状语1: ('do not') <verb> [from|in|to] <noun>
动词状语2: ('do not') <verb> <adv>
Adverbial (状语): 
// Adverbials serve four functions per wiki https://en.wikipedia.org/wiki/Adverbial, and gladly we can iterate all of them here: 
// 1. Adverbial complement: Serve as constraints on time, location, and other aspects of specific action, e.g. Juhn puts the flowers in the water.
//      Implementation: when no adverbial is supplied, the stated information is true as is in a general sense; otherwise it's truth only so far as to the adverbial
// 2. Adjuncts: Serve as supplementary information describing the action, to us the exact meaning of the sentence depend on such, e.g. John and Sophia helped me with my homework. (Notice this is a specific use of help: help somebody with something)
//      Notice per this page https://en.wikipedia.org/wiki/Adjunct_(grammar) the definition of Adjunct is a bit different but that page was probably inaccurate since it counts conjuct as adjunct as well
//      The other three ones are easier to deal with but Adjunct worth some more remarks: per current knowledge the meaning of Adjunct is defined either by the action(i.e. the verb phrase) itself (e.g. in case of help somebody with sth), or by adjunct itself (e.g. with @ToolName); Perceptually they all imply a process specific to the action
//      We heaby clain that conceptually it's equivalent to think that the action + adjunct itself defines the (otherwise physical and tangible and perceptual) process itself, with the legitity of which cannot easily be verified by Airi (e.g. "help sb" "with homework" "with a fork" isn't too legit compared with "eat food with a fork"), but enough to serve the purpose of comprehension. The only problem with such is Airi might have trouble answer "How" questions, see 《Made up reading and questions - I wanted to travel badly》, which are, unspecific after all by its nature unless relavent information is given.
// 3. Conjucts: Serve as logical connection between two sentences, to us it's completely fine to ignore such words without affect meaning, e.g. John helped; therefore, I was able to do my homework. E.g. Mr Reninson, however, voted against the proposal.
// 4. Disjuctives: Serve as more or less as modal particles (语气词), thus can be dropped without affecting the information being conveyed but can serve as a subtle emotional clue of the speaker, e.g. Surprisingly, he passed all his exams.

// Language Constructs
is construct: <noun> "Be" <noun> --> PoolAsBeingConcept
state construct: <noun> "Be" <adj> --> PoolAsBeingConcept
on construct: <noun> "Spatial prep" <noun>	--> PoolAsSpatialConcept
of construct: <noun> of <noun>	--> PoolAsOfConcept
Direction construct: <noun> "Be" [north|south|east|west|near|close] to <noun> --> PoolAsGeoDirectionConcept
名词性从句: [where|when|why|how|what|that] [<noun>|<pron>] ["动词状语1"|"动词状语2"] ("名词性从句")
从句表达句: "名词性从句" [<verb>|"Be"] "名词性从句"
Compound ownership relational accessor 物主代词 component: "物主代词" [<noun>|"Compound ownership relational accessor s form"]
Compound ownership relational accessor of form: (The) "Descriptive component" of [<noun>| "Compound ownership relational accessor of form"| "Compound ownership relational accessor s form" | "Compound ownership relational accessor 物主代词 component"] --> ResolveRelationAccessOfForm   // Returns the objects/concepts being accessed, e.g. Age of Daughter of John Daughter
// Pending define QUOTE_S and Mr. and " etc. inside a pattern // Compound ownership relational accessor s form: [<noun>| "Compound ownership relational accessor s form"|"Compound ownership relational accessor 物主代词 component"] QUOTE_S "Descriptive component" --> ResolveRelationAccessOfForm   // Returns the objects/concepts being accessed, e.g. Age of John's Daughter
// Things to take care of: . can be a punctuation or an abbreciation; 's can be abbv for is or afte a noun to mean a relation; both ' and " are used as actual quotes in written and spoken English -- we also need to define such elements in a generic way so we can easily incorporate other languages e.g. CHN and JPN, i.e. to allow easier future changes

  // Expressions
  Curiosity quest certain: What is <noun>
Curiosity quest uncertain: What does <any> mean(?) --> QueryEncyclopedia    // <Pending> implementation

// Questions
Informative question: [Who|When|Where|What|How|'How many'|'How much'|'How often'|Why] (?) --> ResolveQuestionByQuestionTranslator   // Essentially the translator tries to re-order the elements and fit(replace) the question part by items in our knowledge
// Courtesy questions doesn't count as this type e.g. Can you please (do sth)...
State-related question: "Be" <noun> [<adj>|<noun>](?) --> ResolveQuestionByConceptPool
// Direct query question: ... // This type is more complicated and pending solution, e.g. Tell me name of current president of America; Classify dogs
// Ref: https://en.wikipedia.org/wiki/Question
Simple true/false questions: [Do|Does|Did|Can|Could|May|Might|Must] <noun> "Verb phrase" --> ResolveQuestionByValidatingConceptPool // Essentially search and see whether we have a match

// Phrases
Interruption 1: [Hi|Hey|Sorry]
Interruption 2: Excuse me
Interruption 3: Sorry to bother you
Courtesy interrupt: ["Interruption 1"|"Interruption 2"|"Interruption 3"]
Time: [Morning|Afternoon|Evening|Night]	// Not differentiating cases for specific words
Greeting: Good "Time"

// Conceptual Interactions (Incomplete attemp, more advanced see concept patterns
Definition: ??? [~be|~mean] "Descriptive component" --> DynamicBaseKnowledgeReconstruction
Advanced language concept: ['From now on'|'In the future we will'](,) (use) <noun> [means|indicates] [<noun>|<verb>] --> ConceptReprocessingOrRoutedInterpretation
Ask experience: Do you "Verb phrase"(?)	--> SearchExperienceForPastOccurence

// Patterns that interact with categories
Expand category: ??? is a ('kind of') [#CategoryNameInOwnershipForm|#Category]  --> ExpandCategoryItems

/************************** Domain Specific System Design  **************************/
Location query: "Courtesy interrupt"(,) Where is @LoocationName	--> QueryMap
Weather query: How is weather --> QueryWeather
Airi run command: Airi, run @CommandName --> RunCommand
Airi run application: Airi, "Verb phrase" --> RunApplication
Send email: (Airi)(,) send <any> to @Contacts --> RunEmail  // E.g. "Airi, send today's arrangements to Charles" This kind of compounds which are not participated by email client itself; The <any> in this pattern is re-interpreted and the retrieved text result from executing other commands
Send email pattern test: send email to #Email   --> SendEmailToAddress

/************************** Experimental: Mathematical Language; Not aimed at efficiency, but to test whether this is a generic framework that satisfied how human learn a new kind of language **************************/
// Need support for: Escape characters; general specific word with all symbols not just characters; Grouped optional element (this seems syntactically unnecessary, for equivalents exist as below, but we might want this)
Variable definition: #ValidVariableSymol = ["Basic Expression"|#Number] --> DecalreVariable
Quantity: [#Number|@VariableName|@ConstantQuantityName] // Constant quantities include pi and e etc.
Mathematical function: @FunctionName\(["Quantity"|"Basic Expression"](,)(["Quantity"|"Basic Expression"])\)    --> EvaluateMathematicalFunction
Basic Expression: (\() ["Quantity"|"Mathematical function"] [+|-|*|/] ["Quantity"|"Mathematical function"|"Basic Expression"] (\)) --> EvaluateOrSimplifyExpression
// Notice this is redundant: Basic Expression: (\() ["Quantity"|"Mathematical function"|"Basic Expression"] [+|-|*|/] ["Quantity"|"Mathematical function"|"Basic Expression"] (\)) --> EvaluateOrSimplifyExpression
// Notice to implement above we need the last choice not return premature (defined as a convention)
// The only problem is the above seem not imply operator prescedence -- but it also seems we can easily handle that inside the handler given this pattern, just always evaluate latter expression's first part first if it's a * or /, of if later expression has brackets then evaluate it as a whole first; Then it's just recursion inside handler
Algebraical Arighmetic Equation:  "Basic Expression" = "Basic Expression"  --> SolveAlgebraicEquation
// Some potential design reference (Pending reading): http://www.partow.net/programming/exprtk/, https://courses.cs.vt.edu/~cs1044/spring01/cstruble/notes/6.complexexpr.pdf, https://exprtk.codeplex.com/