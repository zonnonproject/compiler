# Language

[Zonnon](http://zonnon.ethz.ch/) is a general-purpose programming language in the Pascal, Modula-2 
and Oberon family with focus on concurrency and ease of composition. Its conceptual model based on modules, 
objects, definitions and implementations. Its computing model based on active objects with 
their interaction defined by syntax controlled dialogs. It also features mathematical data 
types and operations as first-class language citizens.

# Command Line Compiler for .NET platform / Mono

This project is a fork of ETH Zonnon compiler, but excludes Visual Studio integration.

Zonnon compiler targets .NET framework, but also works on Mono. For computations with mathematical 
data types it can also use OpenCL. To run on Mono the compiler needs to be specially compiled with 
"Rotor" flag.

# Source Code

Source code in this repository for Zonnon compiler does not include source code for CCI. Instead 
3 libraries were precompiled for .NET and separately for Mono. If you are interested in the source 
code for these CCI libraries you can fined a very similar code in [Spec# project](http://specsharp.codeplex.com/) on this site. 
Please note that Spec# extends these libraries directly so in the code there you will get also many things 
not directly relevant for Zonnon.

Zonnon command line compiler does not compile with latest public version of [CCI](https://github.com/microsoft/cci) 
so please either use the libraries checked in with the source code or use the source code from Spec#.
