
<h1 align="center">
  <br>
  <img width="200" height="200" src=logo.png></img>
  <br>
  lwsharp
  <br>
</h1>

An interpreter for [LOOP and WHILE](https://en.wikipedia.org/wiki/LOOP_(programming_language)) written in F#.

## Description

Developed for the [SWAFP (Applied functional programming)](https://kursuskatalog.au.dk/en/course/134686/SWAFP-01-Applied-Functional-Programming) course at Aarhus University, this interpreter prioritizes demonstrating course concepts over performance. Both languages are fully implemented and can be used via the REPL or executed through the CLI.

*There are some small bugs in the code, nevertheless the interpreter does its honest work.*
## Concepts

* Monads and Functors
* Testing and recursive functions
* Reactive programming
* Monoids and Model/Type-based programming
* Akka (Actor model)
* Functional architecture
* Error handling and Railway oriented programming
* Persistent data structures
* Applicatives and functions

The concepts are defined by the course and are used as a base for the oral exam. 
For more information about the concepts, please refer to the [course slides](https://github.com/hkirk/swtafp-slides).

## How to use

To use the interpreter, you can either run it in the REPL or execute it via CLI.
* REPL
```
lwsharp repl
// Opens the REPL
> x10 := 10 
Store: map [("x10", 10)]
> LOOP x10 DO x10 := x10 + 1 END
```

* CLI - (fun fact: it uses the actor model to execute the files in parallel)
```
lwsharp parallel file1.lw file2.lw
SUCCESS - Loop.lw
x0 = 6
x1 = 1
x9 = 3

SUCCESS - While.lw
x0 = 0
x1 = 0
x2 = 159200

Done
```

## Support

In case you have any questions, please feel free to reach out to me via email or create an issue in this repository.

Additionally, if you are interested in taking [SWAFP](https://kursuskatalog.au.dk/en/course/134686/SWAFP-01-Applied-Functional-Programming)
feel free to reach out to me for more information about the course and the exam I might remember some parts of it or refer to the
docs folder where I have the slides used at my oral exam.

Also if you have any feedback on the code, please let me know as this is my first F# project.

## Acknowledgments

* Henrik Bitsch Kirk for the course at the Aarhus University
* [Functors, Applicatives and Monads blogpost](https://www.adit.io/posts/2013-04-17-functors,_applicatives,_and_monads_in_pictures.html)
* [General F# Blogposts](https://fsharpforfunandprofit.com)
* [Mark Seemann's blog](https://blog.ploeh.dk/)

