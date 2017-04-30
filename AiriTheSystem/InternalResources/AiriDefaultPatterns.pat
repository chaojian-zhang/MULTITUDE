/************************** Comments **************************/
// This file defines grammar pattern recognition for Airi
// Patterns are used for two purposes:
// - General grammar definition: This is used to decompose sentences into syntactial elements, normally handled to generic conceptual processing framework for generating outputs
// - Domain specific commands/expressions: This is used to define specific expressions for specific types of interactions, normally have specialized actions associated
// Patterns are not used to below functions:
// - Attributes, properties, categories of specific words: the knowledge of these should be defined in dictionary
// - Concepts: the knowledge of these should be defined in conceptual patterns or memories
// Usage: The formatting should be strict and pay attention to spaces; We have provided validity checker for checking syntax problems

/* (Not recommded) Examples: 
Subject: <noun> // Improper definition; Such kind of patterns should either be defined in dictionary file or implied, to avoid conflicting with other more useful patterns
State: <adj> // Improper definition:  Unnecessary as above */

/************************** Formal Definitions **************************/
// Key Grammars
Be: [be|is|was|are|were]	// Notice this is NOT defined in dictionary because dictionaries carry reference of meaning and some simple grammar(like a real dictionary), not language constructs
Spatial prep: [on|above|in|below|behind|inside]
Verb phrase: [do|<verb>] <noun>
Descriptive Component: (<art>) (<*adv>) (<*adj>) <noun> // Noun phrase (√˚¥ –‘∂Ã”Ô)	// In general avoid such embeded patterns for better efficiency and processability

// Language Constructs
is Construct: <noun> "Be" <noun> --> PoolAsBeingConcept
state Construct: <noun> "Be" <adj> --> PoolAsBeingConcept
on Constrct: <noun> "Spatial prep" <noun>	--> PoolAsSpatialConcept
of Constrct: <noun> of <noun>	--> PoolAsOfConcept
Direction Construct: <noun> "Be" [north|south|east|west|near|close] to <noun> --> PoolAsGeoDirectionConcept

// Expressions
Curiosity Quest: What is <noun>

// Phrases
Interruption 1: [Hi|Hey|Sorry]
Interruption 2: Excuse me
Interruption 3: Sorry to bother you
Coutesy Interrupt: ["Interruption 1"|"Interruption 2"|"Interruption 3"]
Time: [Morning|Afternoon|Evening]	// Not differentiating cases for specific words
Greeting: Good "Time"

// Conceptual Interactions (Incomplete attemp, more advanced see concept patterns
Definition: ??? [~be|~mean] "Descriptive Component" --> DynamicBaseKnowledgeReconstruction
Advanced language concept: ['From now on'|'In the future we will'] (use) <noun> [means|indicates] [<noun>|<verb>] --> ConceptReprocessingOrRoutedInterpretation
Ask Experience: Do you "Verb phrase"	--> SearchExperienceForPastOccurence

/************************** Domain Specific System Design  **************************/
Location Query: "Coutesy Interrupt"(,) Where is @LoocationName	--> QueryMap
Weather Query: How is weather --> QueryWeather
Airi Run Command: Airi, play @ApplicationName --> Run application